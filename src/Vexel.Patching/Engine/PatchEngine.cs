using Vexel.Patching.Definitions;
using Vexel.Patching.Patterns;
using Vexel.Patching.Sessions;

namespace Vexel.Patching.Engine;

public static class PatchEngine
{
    public static PatchDetectionResult Detect(PatchDefinition definition, ReadOnlySpan<byte> image)
    {
        ArgumentNullException.ThrowIfNull(definition);

        BytePattern pattern;
        try
        {
            pattern = BytePattern.Parse(definition.SearchPattern);
        }
        catch (FormatException exception)
        {
            return new PatchDetectionResult(PatchValidationStatus.InvalidDefinition, null, exception.Message);
        }

        var matches = PatternScanner.FindAll(image, pattern);
        if (matches.Count == 0)
        {
            return new PatchDetectionResult(PatchValidationStatus.SignatureNotFound, null, "The signature was not found in the selected image.");
        }

        if (matches.Count != definition.ExpectedMatchCount)
        {
            return new PatchDetectionResult(PatchValidationStatus.AmbiguousSignature, null, $"Expected {definition.ExpectedMatchCount} signature match but found {matches.Count}.");
        }

        var targetOffset = checked(matches[0] + definition.PatchOffset);
        if (targetOffset < 0 || targetOffset > image.Length - definition.ExpectedOriginalBytes.Length)
        {
            return new PatchDetectionResult(PatchValidationStatus.InvalidDefinition, null, "The patch target lies outside the selected image.");
        }

        if (!image.Slice(targetOffset, definition.ExpectedOriginalBytes.Length).SequenceEqual(definition.ExpectedOriginalBytes))
        {
            return new PatchDetectionResult(PatchValidationStatus.OriginalBytesMismatch, targetOffset, "The target bytes do not match the verified original bytes.");
        }

        return new PatchDetectionResult(PatchValidationStatus.Available, targetOffset, "Signature and original bytes verified.");
    }

    public static async Task<PatchOperationResult> ApplyAsync(
        PatchDefinition definition,
        PatchDetectionResult detection,
        IPatchMemory memory,
        int processId,
        DateTimeOffset processStartedAt,
        long moduleBaseAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(memory);

        if (!detection.CanApply)
        {
            return new PatchOperationResult(PatchOperationStatus.ValidationFailed, detection.Detail);
        }

        var address = checked(moduleBaseAddress + detection.TargetOffset!.Value);
        var actualOriginal = await memory.ReadAsync(address, definition.ExpectedOriginalBytes.Length, cancellationToken);
        if (!actualOriginal.AsSpan().SequenceEqual(definition.ExpectedOriginalBytes))
        {
            return new PatchOperationResult(PatchOperationStatus.OriginalBytesChanged, "The target bytes changed after detection; no write was made.");
        }

        await memory.WriteAsync(address, definition.ReplacementBytes, cancellationToken);
        var verified = await memory.ReadAsync(address, definition.ReplacementBytes.Length, cancellationToken);
        if (!verified.AsSpan().SequenceEqual(definition.ReplacementBytes))
        {
            await memory.WriteAsync(address, actualOriginal, cancellationToken);
            return new PatchOperationResult(PatchOperationStatus.WriteVerificationFailed, "The replacement bytes could not be verified. A restoration was attempted.");
        }

        var session = new PatchSession(
            processId,
            processStartedAt,
            moduleBaseAddress,
            definition.Id,
            address,
            actualOriginal,
            definition.ReplacementBytes,
            DateTimeOffset.UtcNow);
        return new PatchOperationResult(PatchOperationStatus.Applied, "Patch applied and verified.", session);
    }

    public static async Task<PatchOperationResult> RestoreAsync(
        PatchSession session,
        IPatchMemory memory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(memory);

        var current = await memory.ReadAsync(session.PatchAddress, session.PatchedBytes.Length, cancellationToken);
        if (!current.AsSpan().SequenceEqual(session.PatchedBytes))
        {
            return new PatchOperationResult(PatchOperationStatus.PatchBytesChanged, "The patch bytes changed; restoration was refused.");
        }

        await memory.WriteAsync(session.PatchAddress, session.OriginalBytes, cancellationToken);
        var verified = await memory.ReadAsync(session.PatchAddress, session.OriginalBytes.Length, cancellationToken);
        return verified.AsSpan().SequenceEqual(session.OriginalBytes)
            ? new PatchOperationResult(PatchOperationStatus.Restored, "Original bytes restored and verified.")
            : new PatchOperationResult(PatchOperationStatus.RestoreVerificationFailed, "Original bytes could not be verified after restoration.");
    }
}
