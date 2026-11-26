using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Web.Conversion;
using JoakimSoftware.TcxTool.Web.State;

using Microsoft.Extensions.Options;

namespace JoakimSoftware.TcxTool.Web.Analysis;

/// <summary>
/// Loads the current CSV's States CSV for the States page (specs/006-activity-states/contracts/pages.md
/// "States page"). It asks <see cref="CurrentCsvStore"/> for the current pair, then opens and reads the
/// States CSV with <see cref="StatesCsvReader"/>, through <see cref="AppCsvFile"/>. It writes nothing, and
/// never throws for a missing, unreadable, or malformed file. A current CSV whose own file is missing
/// does not stop it (research R9).
/// </summary>
public sealed partial class CurrentStatesLoader
{
    private readonly CurrentCsvStore _store;
    private readonly string _csvDirectory;
    private readonly ILogger<CurrentStatesLoader> _logger;

    /// <summary>Creates the loader over the configured CSV directory.</summary>
    /// <param name="store">The current-CSV state.</param>
    /// <param name="options">The <c>TcxTool</c> options.</param>
    /// <param name="environment">The host environment, for resolving relative paths.</param>
    /// <param name="logger">The logger.</param>
    public CurrentStatesLoader(
        CurrentCsvStore store,
        IOptions<TcxToolOptions> options,
        IHostEnvironment environment,
        ILogger<CurrentStatesLoader> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        _store = store;
        _csvDirectory = TcxToolPaths.CsvDirectory(options.Value, environment);
        _logger = logger;
    }

    /// <summary>Loads the current CSV's States CSV.</summary>
    /// <param name="cancellationToken">Cancels the load.</param>
    /// <returns>The current states, in one of the seven statuses.</returns>
    public async Task<CurrentStates> LoadAsync(CancellationToken cancellationToken)
    {
        var current = await _store.ReadAsync(cancellationToken);
        if (current.Status == CurrentCsvStatus.None)
        {
            return CurrentStates.NoCurrentCsv;
        }

        var csvFileName = current.FileName!;
        if (current.StatesFileName is not { } statesFileName)
        {
            return CurrentStates.NoStatesCsv(csvFileName);
        }

        var (outcome, result, exceptionType) = await AppCsvFile.ReadAsync(
            Path.Combine(_csvDirectory, statesFileName), StatesCsvReader.ReadAsync, cancellationToken);
        switch (outcome)
        {
            case AppCsvFile.Outcome.Missing:
                return CurrentStates.Missing(csvFileName, statesFileName);
            case AppCsvFile.Outcome.Unreadable:
                LogUnreadable(statesFileName, exceptionType!);
                return CurrentStates.Unreadable(csvFileName, statesFileName);
            case AppCsvFile.Outcome.Read:
            default:
                break;
        }

        if (result!.Error is { } error)
        {
            LogWrongFormat(statesFileName, error.Kind, error.LineNumber);
            return CurrentStates.WrongFormat(csvFileName, statesFileName);
        }

        return CurrentStates.Read(csvFileName, statesFileName, result.Rows);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "States CSV {FileName} could not be read: {ExceptionType}")]
    private partial void LogUnreadable(string fileName, string exceptionType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "States CSV {FileName} is not in the expected format: {ErrorKind} at line {LineNumber}")]
    private partial void LogWrongFormat(string fileName, ActivityCsvErrorKind errorKind, int lineNumber);
}
