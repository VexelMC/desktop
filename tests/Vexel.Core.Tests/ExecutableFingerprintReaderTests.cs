using Vexel.Core.Minecraft;
using Vexel.Platform.Windows.Detection;

namespace Vexel.Core.Tests;

public sealed class ExecutableFingerprintReaderTests
{
    [Fact]
    public async Task ReadAsyncCreatesStableFingerprintForManagedAssembly()
    {
        var assemblyPath = typeof(ExecutableFingerprintReaderTests).Assembly.Location;

        var fingerprint = await ExecutableFingerprintReader.ReadAsync(assemblyPath);

        Assert.Equal(64, fingerprint.Sha256.Length);
        Assert.True(fingerprint.FileSize > 0);
        Assert.NotEqual(ExecutableArchitecture.Unknown, fingerprint.Architecture);
    }
}
