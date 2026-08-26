namespace Vexel.Core.Settings;

public sealed record AppSettings
{
    public bool ItemUseDelayPreferred { get; init; }

    public bool TeleportRotationPreferred { get; init; }

    public bool AutoSprintPreferred { get; init; }

    public bool NoHurtCamPreferred { get; init; }

    public double GuiScale { get; init; } = 1.0;

    public static AppSettings Default { get; } = new();
}
