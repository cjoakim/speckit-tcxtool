using System.Text.Json;

using JoakimSoftware.TcxTool.Parser.Conversion;
using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Web.Conversion;

using Microsoft.Extensions.Options;

namespace JoakimSoftware.TcxTool.Web.State;

/// <summary>
/// The only reader and writer of the application state file, <c>tcxtool-state.json</c>
/// (specs/004-about-and-csv-state/contracts/state-file.md, with the <c>statesCsvFilename</c> additions of
/// specs/006-activity-states/contracts/state-file.md). Pages bind its typed results and never
/// touch the file system (constitution Principle III). It never throws for bad input or an I/O failure.
/// </summary>
public sealed partial class CurrentCsvStore
{
    /// <summary>The largest state file that is read; anything bigger is ignored (research R3).</summary>
    private const int MaxStateBytes = 64 * 1024;

    private const string CsvFilenameProperty = "csvFilename";
    private const string StatesCsvFilenameProperty = "statesCsvFilename";

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true, NewLine = "\n" };

    private readonly string _csvDirectory;
    private readonly string _statePath;
    private readonly ILogger<CurrentCsvStore> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>Creates the store over the configured CSV directory and state file.</summary>
    /// <param name="options">The <c>TcxTool</c> options.</param>
    /// <param name="environment">The host environment, for resolving relative paths.</param>
    /// <param name="logger">The logger.</param>
    public CurrentCsvStore(IOptions<TcxToolOptions> options, IHostEnvironment environment, ILogger<CurrentCsvStore> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _csvDirectory = Path.TrimEndingDirectorySeparator(TcxToolPaths.CsvDirectory(options.Value, environment));
        _statePath = TcxToolPaths.StateFile(options.Value, environment);
        _logger = logger;
    }

    /// <summary>Reads the current CSV. Anything missing or invalid means <see cref="CurrentCsv.None"/>.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The current CSV.</returns>
    public async Task<CurrentCsv> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_statePath))
        {
            return CurrentCsv.None;
        }

        byte[] content;
        try
        {
            await using var stream = new FileStream(
                _statePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 4096, useAsync: true);
            if (stream.Length > MaxStateBytes)
            {
                return Ignored("larger than 64 KiB");
            }

            content = new byte[stream.Length];
            await stream.ReadExactlyAsync(content, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Ignored("could not be read");
        }

        string? fileName;
        // Whether a statesCsvFilename property exists, and its value when it is a string.
        var hasStatesProperty = false;
        string? statesValue = null;
        try
        {
            using var document = JsonDocument.Parse(WithoutByteOrderMark(content));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Ignored("not valid JSON");
            }

            if (!document.RootElement.TryGetProperty(CsvFilenameProperty, out var value) || value.ValueKind != JsonValueKind.String)
            {
                return Ignored("no csvFilename string");
            }

            fileName = value.GetString();
            if (document.RootElement.TryGetProperty(StatesCsvFilenameProperty, out var states))
            {
                hasStatesProperty = true;
                statesValue = states.ValueKind == JsonValueKind.String ? states.GetString() : null;
            }
        }
        catch (JsonException)
        {
            return Ignored("not valid JSON");
        }

        if (!CsvFileNameRule.IsPlainCsvName(fileName) || !IsInsideCsvDirectory(fileName!))
        {
            return Ignored("csvFilename is not a plain CSV name");
        }

        string? statesFileName = null;
        if (hasStatesProperty)
        {
            // The pair is only trusted when the States name is exactly the CSV's own (feature 006 FR-016).
            if (string.Equals(statesValue, StatesCsvFileName.For(fileName!), StringComparison.Ordinal))
            {
                statesFileName = statesValue;
            }
            else
            {
                LogStatesNameMismatch(_statePath);
            }
        }

        return File.Exists(Path.Combine(_csvDirectory, fileName!))
            ? CurrentCsv.Present(fileName!, statesFileName)
            : CurrentCsv.Missing(fileName!, statesFileName);
    }

    /// <summary>
    /// The CSV files that can be selected: top-level names in the CSV directory that pass
    /// <see cref="CsvFileNameRule"/>, newest first (descending ordinal, research R5).
    /// </summary>
    /// <returns>The names, or an empty list when the directory does not exist.</returns>
    public IReadOnlyList<string> ListCsvFiles()
    {
        if (!Directory.Exists(_csvDirectory))
        {
            return [];
        }

        return [.. Directory.EnumerateFiles(_csvDirectory)
            .Select(Path.GetFileName)
            .Where(CsvFileNameRule.IsPlainCsvName)
            .Select(name => name!)
            .OrderDescending(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Makes <paramref name="fileName"/> the current CSV when it is a plain CSV name of a file in the
    /// CSV directory, recording its States CSV too when that file exists (feature 006 FR-014).
    /// Otherwise the state file is left unchanged.
    /// </summary>
    /// <param name="fileName">The chosen file name.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>What happened.</returns>
    public async Task<CsvSelectionResult> SelectAsync(string? fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return new(CsvSelectionOutcome.NoneChosen, null);
        }

        if (!CsvFileNameRule.IsPlainCsvName(fileName) || !IsInsideCsvDirectory(fileName))
        {
            return new(CsvSelectionOutcome.InvalidName, fileName);
        }

        if (!File.Exists(Path.Combine(_csvDirectory, fileName)))
        {
            return new(CsvSelectionOutcome.NotFound, fileName);
        }

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var statesFileName = StatesCsvFileName.For(fileName);
            var statesExists = File.Exists(Path.Combine(_csvDirectory, statesFileName));
            await WriteAsync(fileName, statesExists ? statesFileName : null, cancellationToken);
            LogCurrentCsvSet(fileName);
            return new(CsvSelectionOutcome.Recorded, fileName);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogWriteFailed(_statePath, exception.GetType().Name);
            return new(CsvSelectionOutcome.WriteFailed, fileName);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Records the first CSV a successful conversion saved as the current one (spec 004 FR-011), with
    /// the States CSV the conversion saved beside it (feature 006 FR-013).
    /// A failed conversion, or one that saved nothing, leaves the state file unchanged (FR-013).
    /// </summary>
    /// <param name="result">The conversion result.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>What happened, or null when there was nothing to record.</returns>
    public async Task<CsvSelectionResult?> RecordConversionAsync(ConversionResult result, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSuccess || result.Files.Count == 0)
        {
            return null;
        }

        return await SelectAsync(result.Files[0].FileName, cancellationToken);
    }

    private async Task WriteAsync(string fileName, string? statesFileName, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
        byte[] content = [.. JsonSerializer.SerializeToUtf8Bytes(new TcxToolState(fileName, statesFileName), WriteOptions), (byte)'\n'];
        var temp = $"{_statePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllBytesAsync(temp, content, cancellationToken);
            File.Move(temp, _statePath, overwrite: true);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best effort: the temp name is unique, so a leftover never collides with a later write.
        }
    }

    private bool IsInsideCsvDirectory(string fileName) =>
        Path.GetFullPath(Path.Combine(_csvDirectory, fileName))
            .StartsWith(_csvDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    private static ReadOnlyMemory<byte> WithoutByteOrderMark(byte[] content) =>
        content.AsSpan().StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]) ? content.AsMemory(3) : content;

    private CurrentCsv Ignored(string reason)
    {
        LogIgnored(_statePath, reason);
        return CurrentCsv.None;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "State file {Path} ignored: {Reason}")]
    private partial void LogIgnored(string path, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "State file {Path}: statesCsvFilename does not match csvFilename")]
    private partial void LogStatesNameMismatch(string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "State file {Path} could not be written: {ExceptionType}")]
    private partial void LogWriteFailed(string path, string exceptionType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Current CSV set to {FileName}")]
    private partial void LogCurrentCsvSet(string fileName);
}
