namespace Vexel.Core.Minecraft;

public sealed record MinecraftInstallation(
    string PackageFullName,
    string PackageFamilyName,
    string Version,
    string InstallLocation,
    string ExecutablePath);
