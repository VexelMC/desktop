using System.Diagnostics;
using Vexel.Core.Minecraft;
using Vexel.Patching.Patterns;
using Vexel.Platform.Windows.Memory;

namespace Vexel.Core.Tests;

public sealed class LoadedModulePatternScannerTests
{
    [Fact]
    public async Task FindAllAsyncReadsCurrentProcessWithoutWriting()
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

        var matches = await LoadedModulePatternScanner.FindAllAsync(process, BytePattern.Parse("4D 5A"));

        Assert.Contains(module.BaseAddress.ToInt64(), matches);
    }
}
