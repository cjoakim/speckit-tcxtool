using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Web.Conversion;
using JoakimSoftware.TcxTool.Web.State;

using Microsoft.Extensions.Options;

namespace JoakimSoftware.TcxTool.Web.Analysis;

/// <summary>
/// Loads the current CSV for the analysis pages (specs/005-table-page research R2). It asks
/// <see cref="CurrentCsvStore"/> which CSV is current, then opens and reads it with
/// <see cref="ActivityCsvReader"/>, through <see cref="AppCsvFile"/>. Pages bind the typed <see cref="CurrentActivity"/> and never touch the
/// file system (constitution Principle III). It writes nothing, and never throws for a missing,
/// unreadable, or malformed file.
/// </summary>
public sealed partial class CurrentActivityLoader
{
    private readonly CurrentCsvStore _store;
    private readonly string _csvDirectory;
    private readonly ILogger<CurrentActivityLoader> _logger;

    /// <summary>Creates the loader over the configured CSV directory.</summary>
    /// <param name="store">The current-CSV state.</param>
    /// <param name="options">The <c>TcxTool</c> options.</param>
    /// <param name="environment">The host environment, for resolving relative paths.</param>
    /// <param name="logger">The logger.</param>
    public CurrentActivityLoader(
        CurrentCsvStore store,
        IOptions<TcxToolOptions> options,
        IHostEnvironment environment,
        ILogger<CurrentActivityLoader> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        _store = store;
        _csvDirectory = TcxToolPaths.CsvDirectory(options.Value, environment);
        _logger = logger;
    }

    /// <summary>Loads the current CSV.</summary>
    /// <param name="cancellationToken">Cancels the load.</param>
    /// <returns>The current activity, in one of the six statuses.</returns>
    public async Task<CurrentActivity> LoadAsync(CancellationToken cancellationToken)
    {
        var current = await _store.ReadAsync(cancellationToken);
        if (current.Status == CurrentCsvStatus.None)
        {
            return CurrentActivity.NoCurrentCsv;
        }

        var fileName = current.FileName!;
        if (current.Status == CurrentCsvStatus.Missing)
        {
            return CurrentActivity.Missing(fileName);
        }

        var (outcome, result, exceptionType) = await AppCsvFile.ReadAsync(
            Path.Combine(_csvDirectory, fileName), ActivityCsvReader.ReadAsync, cancellationToken);
        switch (outcome)
        {
            case AppCsvFile.Outcome.Missing:
                return CurrentActivity.Missing(fileName);
            case AppCsvFile.Outcome.Unreadable:
                LogUnreadable(fileName, exceptionType!);
                return CurrentActivity.Unreadable(fileName);
            case AppCsvFile.Outcome.Read:
            default:
                break;
        }

        if (result!.Error is { } error)
        {
            LogWrongFormat(fileName, error.Kind, error.LineNumber);
            return CurrentActivity.WrongFormat(fileName);
        }

        return CurrentActivity.Read(fileName, result.Rows);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Current CSV {FileName} could not be read: {ExceptionType}")]
    private partial void LogUnreadable(string fileName, string exceptionType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Current CSV {FileName} is not in the expected format: {ErrorKind} at line {LineNumber}")]
    private partial void LogWrongFormat(string fileName, ActivityCsvErrorKind errorKind, int lineNumber);
}
