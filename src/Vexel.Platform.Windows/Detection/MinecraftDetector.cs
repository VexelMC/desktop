using System.Diagnostics;
using Microsoft.Win32;
using Vexel.Core.Minecraft;

namespace Vexel.Platform.Windows.Detection;

public sealed class MinecraftDetector : IMinecraftDetector
{
    private const string AppModelPackagesKey = "Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\AppModel\\Repository\\Packages";
    private const string MinecraftPackagePrefix = "Microsoft.MinecraftUWP_";
    private const string MinecraftExecutableName = "Minecraft.Windows.exe";

    public Task<MinecraftDetectionResult> DetectAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => DetectCoreAsync(cancellationToken), cancellationToken);

    private static async Task<MinecraftDetectionResult> DetectCoreAsync(CancellationToken cancellationToken)
    {
        MinecraftInstallation? installation;
        try
        {
            installation = FindInstallation();
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or InvalidOperationException)
        {
            return new MinecraftDetectionResult(null, [], null, $"Package discovery failed: {exception.Message}");
        }

        var processes = FindProcesses();
        if (installation is null)
        {
            return new MinecraftDetectionResult(null, processes, null, null);
        }

        try
        {
            var fingerprint = await ExecutableFingerprintReader.ReadAsync(installation.ExecutablePath, cancellationToken);
            return new MinecraftDetectionResult(installation, processes, fingerprint, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or BadImageFormatException)
        {
            return new MinecraftDetectionResult(installation, processes, null, $"Fingerprinting failed: {exception.Message}");
        }
    }

    private static MinecraftInstallation? FindInstallation()
    {
        using var packages = Registry.CurrentUser.OpenSubKey(AppModelPackagesKey, writable: false);
        if (packages is null)
        {
            return null;
        }

        return packages.GetSubKeyNames()
            .Where(name => name.StartsWith(MinecraftPackagePrefix, StringComparison.OrdinalIgnoreCase))
            .Select(ReadInstallation)
            .Where(installation => installation is not null)
            .Cast<MinecraftInstallation>()
            .OrderByDescending(installation => ParseVersion(installation.Version))
            .FirstOrDefault();
    }

    private static MinecraftInstallation? ReadInstallation(string packageFullName)
    {
        using var package = Registry.CurrentUser.OpenSubKey($"{AppModelPackagesKey}\\{packageFullName}", writable: false);
        var installLocation = package?.GetValue("PackageRootFolder") as string;
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            return null;
        }

        var segments = packageFullName.Split('_');
        if (segments.Length < 5)
        {
            return null;
        }

        var publisherId = segments[^1];
        return new MinecraftInstallation(
            packageFullName,
            $"{segments[0]}_{publisherId}",
            segments[1],
            installLocation,
            Path.Combine(installLocation, MinecraftExecutableName));
    }

    private static Version ParseVersion(string version) =>
        Version.TryParse(version, out var parsed) ? parsed : new Version(0, 0);

    private static MinecraftProcess[] FindProcesses()
    {
        var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(MinecraftExecutableName));
        try
        {
            return processes.Select(process =>
            {
                try
                {
                    return new MinecraftProcess(
                        process.Id,
                        process.MainModule?.FileName,
                        new DateTimeOffset(process.StartTime.ToUniversalTime()));
                }
                catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    return new MinecraftProcess(process.Id, null, null);
                }
            }).ToArray();
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }
}
