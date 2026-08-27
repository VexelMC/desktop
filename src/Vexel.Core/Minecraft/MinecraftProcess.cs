namespace Vexel.Core.Minecraft;

public sealed record MinecraftProcess(
    int ProcessId,
    string? ExecutablePath,
    DateTimeOffset? StartedAt,
    long? ModuleBaseAddress = null,
    int? ModuleSize = null);
