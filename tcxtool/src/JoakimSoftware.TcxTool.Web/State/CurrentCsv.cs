using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Web.State;

/// <summary>
/// The current CSV, as read from the application state (spec 004 FR-009). Pages use
/// <see cref="FileName"/> only when <see cref="Status"/> is <see cref="CurrentCsvStatus.Present"/>.
/// </summary>
public sealed record CurrentCsv
{
    private CurrentCsv(string? fileName, CurrentCsvStatus status, string? statesFileName)
    {
        FileName = fileName;
        Status = status;
        StatesFileName = statesFileName;
    }

    /// <summary>There is no current CSV.</summary>
    public static CurrentCsv None { get; } = new(null, CurrentCsvStatus.None, null);

    /// <summary>The file name within the CSV directory; null exactly when <see cref="Status"/> is <see cref="CurrentCsvStatus.None"/>.</summary>
    public string? FileName { get; }

    /// <summary>Whether there is a current CSV and whether its file exists.</summary>
    public CurrentCsvStatus Status { get; }

    /// <summary>
    /// The current CSV's States CSV name, recorded with it in the state file; null when none was
    /// recorded or the recorded name is not <c>StatesCsvFileName.For(FileName)</c> (feature 006 FR-016).
    /// Whether the file exists is not checked here.
    /// </summary>
    public string? StatesFileName { get; }

    /// <summary>A current CSV whose file exists.</summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="statesFileName">Its States CSV name, or null.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentException"><paramref name="statesFileName"/> is not the States CSV name of <paramref name="fileName"/>.</exception>
    public static CurrentCsv Present(string fileName, string? statesFileName = null) =>
        Create(fileName, CurrentCsvStatus.Present, statesFileName);

    /// <summary>A current CSV whose file is no longer in the CSV directory.</summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="statesFileName">Its States CSV name, or null.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentException"><paramref name="statesFileName"/> is not the States CSV name of <paramref name="fileName"/>.</exception>
    public static CurrentCsv Missing(string fileName, string? statesFileName = null) =>
        Create(fileName, CurrentCsvStatus.Missing, statesFileName);

    private static CurrentCsv Create(string fileName, CurrentCsvStatus status, string? statesFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (statesFileName is not null && !string.Equals(statesFileName, StatesCsvFileName.For(fileName), StringComparison.Ordinal))
        {
            throw new ArgumentException("The States CSV name must belong to the CSV.", nameof(statesFileName));
        }

        return new(fileName, status, statesFileName);
    }
}
