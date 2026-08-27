using System.Diagnostics;
using Vexel.Core.Minecraft;
using Vexel.Platform.Windows.Memory;

namespace Vexel.Core.Tests;

public sealed class LoadedModuleFingerprintReaderTests
{
    [Fact]
    public async Task ReadAsyncHashesCurrentProcessMainModule()
    {
        using var current = Process.GetCurrentProcess();
        var module = current.MainModule;
        Assert.NotNull(module);

        var process = new MinecraftProcess(
            current.Id,
            module.FileName,
            new DateTimeOffset(current.StartTime.ToUniversalTime()),
            module.BaseAddress.ToInt64(),
            module.ModuleMemorySize);

        var fingerprint = await LoadedModuleFingerprintReader.ReadAsync(process, "test-build");

        Assert.Equal(FingerprintSource.LoadedModule, fingerprint.Source);
        Assert.Equal(64, fingerprint.Sha256.Length);
        Assert.Equal(module.ModuleMemorySize, fingerprint.FileSize);
    }
}
