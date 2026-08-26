namespace Vexel.Patching.Engine;

public interface IPatchMemory
{
    Task<byte[]> ReadAsync(long address, int length, CancellationToken cancellationToken = default);

    Task WriteAsync(long address, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default);
}
