namespace Vexel.Core.Minecraft;

public sealed record MinecraftBuildFingerprint(
    string FileVersion,
    string ProductVersion,
    long FileSize,
    string Sha256,
    DateTimeOffset? PeTimestamp,
    ExecutableArchitecture Architecture,
    FingerprintSource Source);
