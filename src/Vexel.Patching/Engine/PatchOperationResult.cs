using Vexel.Patching.Sessions;

namespace Vexel.Patching.Engine;

public sealed record PatchOperationResult(
    PatchOperationStatus Status,
    string Detail,
    PatchSession? Session = null);
