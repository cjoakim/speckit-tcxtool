using Microsoft.Extensions.Logging;

namespace JoakimSoftware.TcxTool.Tests.Support;

/// <summary>
/// An <see cref="ILogger{TCategoryName}"/> fake that records each entry, for checking what is logged
/// (spec FR-018). A plain fake avoids adding NSubstitute for one boundary.
/// </summary>
public sealed class ListLogger<T> : ILogger<T>
{
    private readonly List<(LogLevel Level, string Message)> _entries = [];

    /// <summary>The recorded entries, in order.</summary>
    public IReadOnlyList<(LogLevel Level, string Message)> Entries => _entries;

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        _entries.Add((logLevel, formatter(state, exception)));
    }
}
