using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Vexel.Core.Minecraft;

namespace Vexel.Platform.Windows.Detection;

public static class ExecutableFingerprintReader
{
    public static async Task<MinecraftBuildFingerprint> ReadAsync(
        string executablePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        var file = new FileInfo(executablePath);
        if (!file.Exists)
        {
            throw new FileNotFoundException("The Minecraft executable was not found.", executablePath);
        }

        var version = FileVersionInfo.GetVersionInfo(executablePath);
        await using var input = File.OpenRead(executablePath);
        var sha256 = await SHA256.HashDataAsync(input, cancellationToken);
        var (timestamp, architecture) = ReadPeMetadata(executablePath);

        return new MinecraftBuildFingerprint(
            version.FileVersion ?? "Unknown",
            version.ProductVersion ?? "Unknown",
            file.Length,
            Convert.ToHexString(sha256),
            timestamp,
            architecture);
    }

    private static (DateTimeOffset? Timestamp, ExecutableArchitecture Architecture) ReadPeMetadata(string executablePath)
    {
        using var stream = File.OpenRead(executablePath);
        using var reader = new PEReader(stream);
        var coffHeader = reader.PEHeaders.CoffHeader;
        DateTimeOffset? timestamp = coffHeader.TimeDateStamp == 0
            ? null
            : DateTimeOffset.FromUnixTimeSeconds(coffHeader.TimeDateStamp);

        return (timestamp, coffHeader.Machine switch
        {
            Machine.I386 => ExecutableArchitecture.X86,
            Machine.Amd64 => ExecutableArchitecture.X64,
            Machine.Arm64 => ExecutableArchitecture.Arm64,
            _ => ExecutableArchitecture.Unknown,
        });
    }
}
