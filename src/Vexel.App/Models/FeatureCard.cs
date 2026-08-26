namespace Vexel.App.Models;

public sealed record FeatureCard(
    string Name,
    string Description,
    string Status,
    string Detail,
    bool IsAvailable);
