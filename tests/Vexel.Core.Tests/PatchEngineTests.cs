using Vexel.Patching.Definitions;
using Vexel.Patching.Engine;

namespace Vexel.Core.Tests;

public sealed class PatchEngineTests
{
    [Fact]
    public async Task ApplyAsyncAndRestoreAsyncRoundTripVerifiedBytes()
    {
        var definition = CreateDefinition();
        var image = new byte[] { 0xCC, 0x48, 0x8B, 0x07, 0x90, 0xCC };
        var detection = PatchEngine.Detect(definition, image);
        var memory = new SyntheticPatchMemory(image, moduleBaseAddress: 0x1000);

        var applied = await PatchEngine.ApplyAsync(
            definition,
            detection,
            memory,
            processId: 1234,
            DateTimeOffset.UnixEpoch,
            moduleBaseAddress: 0x1000);

        Assert.Equal(PatchOperationStatus.Applied, applied.Status);
        Assert.NotNull(applied.Session);
        Assert.Equal(new byte[] { 0x90, 0x90 }, memory.Bytes[3..5]);

        var restored = await PatchEngine.RestoreAsync(applied.Session!, memory);

        Assert.Equal(PatchOperationStatus.Restored, restored.Status);
        Assert.Equal(new byte[] { 0x07, 0x90 }, memory.Bytes[3..5]);
    }

    [Fact]
    public void DetectRefusesAmbiguousSignature()
    {
        var image = new byte[] { 0x48, 0x8B, 0x07, 0x90, 0x48, 0x8B, 0x07, 0x90 };

        var result = PatchEngine.Detect(CreateDefinition(), image);

        Assert.Equal(PatchValidationStatus.AmbiguousSignature, result.Status);
    }

    [Fact]
    public void DetectRefusesUnexpectedOriginalBytes()
    {
        var image = new byte[] { 0x48, 0x8B, 0x08, 0x90 };

        var result = PatchEngine.Detect(CreateDefinition(), image);

        Assert.Equal(PatchValidationStatus.OriginalBytesMismatch, result.Status);
    }

    [Fact]
    public async Task RestoreAsyncRefusesWhenPatchBytesChanged()
    {
        var definition = CreateDefinition();
        var image = new byte[] { 0x48, 0x8B, 0x07, 0x90 };
        var memory = new SyntheticPatchMemory(image, moduleBaseAddress: 0x1000);
        var applied = await PatchEngine.ApplyAsync(
            definition,
            PatchEngine.Detect(definition, image),
            memory,
            1234,
            DateTimeOffset.UnixEpoch,
            0x1000);
        await memory.WriteAsync(0x1002, new byte[] { 0xCC, 0xCC });

        var restored = await PatchEngine.RestoreAsync(applied.Session!, memory);

        Assert.Equal(PatchOperationStatus.PatchBytesChanged, restored.Status);
    }

    private static PatchDefinition CreateDefinition() => new(
        "test-patch",
        "Test patch",
        "48 8B ?? 90",
        expectedMatchCount: 1,
        patchOffset: 2,
        expectedOriginalBytes: [0x07, 0x90],
        replacementBytes: [0x90, 0x90]);

    private sealed class SyntheticPatchMemory : IPatchMemory
    {
        private readonly long _moduleBaseAddress;

        public SyntheticPatchMemory(byte[] bytes, long moduleBaseAddress)
        {
            Bytes = bytes;
            _moduleBaseAddress = moduleBaseAddress;
        }

        public byte[] Bytes { get; }

        public Task<byte[]> ReadAsync(long address, int length, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var offset = checked((int)(address - _moduleBaseAddress));
            return Task.FromResult(Bytes[offset..(offset + length)]);
        }

        public Task WriteAsync(long address, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var offset = checked((int)(address - _moduleBaseAddress));
            bytes.CopyTo(Bytes.AsMemory(offset, bytes.Length));
            return Task.CompletedTask;
        }
    }
}
