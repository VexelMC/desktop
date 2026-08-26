namespace Vexel.Patching.Engine;

public enum PatchOperationStatus
{
    Applied,
    Restored,
    ValidationFailed,
    OriginalBytesChanged,
    WriteVerificationFailed,
    RestoreVerificationFailed,
    PatchBytesChanged,
}
