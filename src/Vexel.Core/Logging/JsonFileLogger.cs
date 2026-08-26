using System.Text.Json;

namespace Vexel.Core.Logging;

public sealed class JsonFileLogger : IAppLogger, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _directory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonFileLogger(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
    }

    public async Task WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, $"vexel-{DateTimeOffset.UtcNow:yyyy-MM-dd}.jsonl");
        var line = JsonSerializer.Serialize(entry, SerializerOptions);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(path, line + Environment.NewLine, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();
}
