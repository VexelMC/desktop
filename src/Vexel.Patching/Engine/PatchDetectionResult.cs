namespace Vexel.Patching.Engine;

public sealed record PatchDetectionResult(
    PatchValidationStatus Status,
    int? TargetOffset,
    string Detail)
{
    public bool CanApply => Status == PatchValidationStatus.Available && TargetOffset.HasValue;
}
