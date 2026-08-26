namespace Vexel.Core.Minecraft;

public interface IMinecraftDetector
{
    Task<MinecraftDetectionResult> DetectAsync(CancellationToken cancellationToken = default);
}
