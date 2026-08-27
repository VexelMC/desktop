using Vexel.Core.Minecraft;
using Vexel.Patching.Patterns;
using Vexel.Platform.Windows.Memory;

namespace Vexel.Platform.Windows.Compatibility;

/// <summary>
/// Investigates historical signatures without changing the target process.
/// A result is deliberately not a patch definition or a compatibility approval.
/// </summary>
public sealed class MinecraftFeatureProbe
{
    private readonly Candidate[] _candidates =
    [
        new(
            "item-use-delay",
            BytePattern.Parse("FF 15 ? ? ? ? 48 8B ? 48 8B ? 48 8B ? ? ? ? ? FF 15 ? ? ? ? 32 DB"),
            "Historical Item Use Delay candidate"),
        new(
            "auto-sprint",
            BytePattern.Parse("48 8D 05 ? ? ? ? 48 89 01 48 89 51 ? 48 C7 41"),
            "Historical AutoSprint candidate"),
    ];

    public async Task<IReadOnlyList<FeatureProbeResult>> ProbeAsync(
        MinecraftDetectionResult detection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(detection);

        var process = detection.Processes.FirstOrDefault(candidate =>
            candidate.ModuleBaseAddress is not null && candidate.ModuleSize is not null);
        if (process is null)
        {
            return _candidates
                .Select(candidate => new FeatureProbeResult(candidate.Id, 0, "No readable Minecraft module is available."))
                .ToArray();
        }

        var results = new List<FeatureProbeResult>(_candidates.Length);
        foreach (var candidate in _candidates)
        {
            var matches = await LoadedModulePatternScanner.FindAllAsync(process, candidate.Pattern, cancellationToken);
            var detail = matches.Length switch
            {
                0 => $"{candidate.Description}: no match in this loaded build.",
                1 => $"{candidate.Description}: one candidate found; disassembly and offline behaviour tests are still required.",
                _ => $"{candidate.Description}: {matches.Length} candidates found; the match is ambiguous.",
            };
            results.Add(new FeatureProbeResult(candidate.Id, matches.Length, detail));
        }

        return results;
    }

    private sealed record Candidate(string Id, BytePattern Pattern, string Description);
}
