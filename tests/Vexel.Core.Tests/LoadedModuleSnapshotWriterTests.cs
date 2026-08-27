using System.Diagnostics;
using Vexel.Core.Minecraft;
using Vexel.Platform.Windows.Memory;

namespace Vexel.Core.Tests;

public sealed class LoadedModuleSnapshotWriterTests
{
    [Fact]
    public async Task WriteAsyncExportsCurrentProcessWithoutOverwriting()
    {
        using var current = Process.GetCurrentProcess();
        var module = current.MainModule;
        Assert.NotNull(module);

        var destination = Path.Combine(Path.GetTempPath(), $"vexel-{Guid.NewGuid():N}.bin");
        try
        {
            var process = new MinecraftProcess(
                current.Id,
                module.FileName,
                new DateTimeOffset(current.StartTime.ToUniversalTime()),
                module.BaseAddress.ToInt64(),
                module.ModuleMemorySize);

            var snapshot = await LoadedModuleSnapshotWriter.WriteAsync(process, destination);

            Assert.Equal(Path.GetFullPath(destination), snapshot.Path);
            Assert.Equal(module.ModuleMemorySize, snapshot.Size);
            Assert.Equal(64, snapshot.Sha256.Length);
            Assert.Equal(snapshot.Size, new FileInfo(destination).Length);
            await Assert.ThrowsAsync<IOException>(() => LoadedModuleSnapshotWriter.WriteAsync(process, destination));
        }
        finally
        {
            File.Delete(destination);
        }
    }
}
