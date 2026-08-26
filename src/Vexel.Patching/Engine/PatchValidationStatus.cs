namespace Vexel.Patching.Engine;

public enum PatchValidationStatus
{
    Available,
    SignatureNotFound,
    AmbiguousSignature,
    OriginalBytesMismatch,
    InvalidDefinition,
}
