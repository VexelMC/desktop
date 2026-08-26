namespace Vexel.Core.Minecraft;

public sealed record MinecraftDetectionResult(
    MinecraftInstallation? Installation,
    IReadOnlyList<MinecraftProcess> Processes,
    MinecraftBuildFingerprint? Fingerprint,
    string? Diagnostic)
{
    public bool IsInstalled => Installation is not null;

    public bool IsRunning => Processes.Count > 0;
}
