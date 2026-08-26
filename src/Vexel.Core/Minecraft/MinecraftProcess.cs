namespace Vexel.Core.Minecraft;

public sealed record MinecraftProcess(
    int ProcessId,
    string? ExecutablePath,
    DateTimeOffset? StartedAt);
