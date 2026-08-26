namespace Vexel.Core.Logging;

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string EventName,
    string Message,
    IReadOnlyDictionary<string, string>? Properties = null);
