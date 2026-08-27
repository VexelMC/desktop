namespace Vexel.Platform.Windows.Compatibility;

public sealed record FeatureProbeResult(string FeatureId, int MatchCount, string Detail)
{
    public bool HasSingleCandidate => MatchCount == 1;
}
