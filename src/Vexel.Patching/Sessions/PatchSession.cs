namespace Vexel.Patching.Sessions;

public sealed class PatchSession
{
    public PatchSession(
        int processId,
        DateTimeOffset processStartedAt,
        long moduleBaseAddress,
        string patchId,
        long patchAddress,
        byte[] originalBytes,
        byte[] patchedBytes,
        DateTimeOffset appliedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patchId);
        ArgumentNullException.ThrowIfNull(originalBytes);
        ArgumentNullException.ThrowIfNull(patchedBytes);

        ProcessId = processId;
        ProcessStartedAt = processStartedAt;
        ModuleBaseAddress = moduleBaseAddress;
        PatchId = patchId;
        PatchAddress = patchAddress;
        OriginalBytes = originalBytes.ToArray();
        PatchedBytes = patchedBytes.ToArray();
        AppliedAt = appliedAt;
    }

    public int ProcessId { get; }

    public DateTimeOffset ProcessStartedAt { get; }

    public long ModuleBaseAddress { get; }

    public string PatchId { get; }

    public long PatchAddress { get; }

    public byte[] OriginalBytes { get; }

    public byte[] PatchedBytes { get; }

    public DateTimeOffset AppliedAt { get; }
}
