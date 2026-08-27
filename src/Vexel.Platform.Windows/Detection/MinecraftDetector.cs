using System.Diagnostics;
using Microsoft.Win32;
using Vexel.Core.Minecraft;
using Vexel.Platform.Windows.Memory;

namespace Vexel.Platform.Windows.Detection;

public sealed class MinecraftDetector : IMinecraftDetector
{
    private const string AppModelPackagesKey = "Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\AppModel\\Repository\\Packages";
    private const string MinecraftPackagePrefix = "Microsoft.MinecraftUWP_";
    private const string MinecraftExecutableName = "Minecraft.Windows.exe";
    private static readonly string[] MinecraftProcessNames = ["Minecraft.Windows", "Minecraft"];

    public Task<MinecraftDetectionResult> DetectAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => DetectCoreAsync(cancellationToken), cancellationToken);

    private static async Task<MinecraftDetectionResult> DetectCoreAsync(CancellationToken cancellationToken)
    {
        MinecraftInstallation? installation = null;
        string? packageDiagnostic = null;
        try
        {
            installation = FindInstallation();
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or InvalidOperationException)
        {
            packageDiagnostic = $"Package discovery failed: {exception.Message}";
        }

        var processes = FindProcesses();
        var executablePath = processes
            .Select(process => process.ExecutablePath)
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            ?? installation?.ExecutablePath;

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            var diagnostic = processes.Length > 0
                ? "A Minecraft process was found, but its executable path could not be read."
                : packageDiagnostic;
            return new MinecraftDetectionResult(installation, processes, null, diagnostic);
        }

        try
        {
            var fingerprint = await ExecutableFingerprintReader.ReadAsync(executablePath, cancellationToken);
            return new MinecraftDetectionResult(installation, processes, fingerprint, null);
        }
        catch (UnauthorizedAccessException)
        {
            var runningProcess = processes.FirstOrDefault(process => process.ModuleBaseAddress is not null && process.ModuleSize is not null);
            if (runningProcess is null)
            {
                return new MinecraftDetectionResult(installation, processes, null, "The executable is protected and the running module layout is unavailable.");
            }

            try
            {
                var version = installation?.Version ?? "Unknown";
                var fingerprint = await LoadedModuleFingerprintReader.ReadAsync(runningProcess, version, cancellationToken);
                return new MinecraftDetectionResult(installation, processes, fingerprint, null);
            }
            catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                return new MinecraftDetectionResult(installation, processes, null, $"Loaded-module fingerprinting failed: {exception.Message}");
            }
        }
        catch (Exception exception) when (exception is IOException or BadImageFormatException)
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
        var processes = Process.GetProcesses();
        try
        {
            return processes.Where(IsMinecraftProcess).Select(process =>
            {
                try
                {
                    var module = process.MainModule;
                    return new MinecraftProcess(
                        process.Id,
                        module?.FileName,
                        new DateTimeOffset(process.StartTime.ToUniversalTime()),
                        module?.BaseAddress.ToInt64(),
                        module?.ModuleMemorySize);
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

    private static bool IsMinecraftProcess(Process process) =>
        MinecraftProcessNames.Any(name => process.ProcessName.Equals(name, StringComparison.OrdinalIgnoreCase));
}
