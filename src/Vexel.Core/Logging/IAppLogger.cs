namespace Vexel.Core.Logging;

public interface IAppLogger
{
    Task WriteAsync(LogEntry entry, CancellationToken cancellationToken = default);
}
