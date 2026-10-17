using System.Collections.Concurrent;

namespace BioVic.Api.Logging;

public sealed class RollingFileLoggerProvider(string directory) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, RollingFileLogger> _loggers = new(StringComparer.Ordinal);
    private readonly object _writeLock = new();

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, category => new RollingFileLogger(category, directory, _writeLock));

    public void Dispose() => _loggers.Clear();
}

internal sealed class RollingFileLogger(string category, string directory, object writeLock) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        var line = $"{DateTimeOffset.UtcNow:O} [{logLevel}] {category}: {formatter(state, exception)}";
        if (exception is not null) line += Environment.NewLine + exception;
        lock (writeLock)
        {
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, $"biovic-{DateTime.UtcNow:yyyy-MM-dd}.log"), line + Environment.NewLine);
        }
    }
}
