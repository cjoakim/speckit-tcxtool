using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Web.Analysis;

/// <summary>
/// The current CSV as an analysis page sees it: a status, and for a readable file its summary and rows
/// (specs/005-table-page/data-model.md). Each status is built by its own member, so it can't come with
/// the wrong properties.
/// </summary>
public sealed record CurrentActivity
{
    private CurrentActivity(CurrentActivityStatus status, string? fileName, ActivitySummary? summary, IReadOnlyList<ActivityCsvRow> rows)
    {
        Status = status;
        FileName = fileName;
        Summary = summary;
        Rows = rows;
    }

    /// <summary>There is no current CSV.</summary>
    public static CurrentActivity NoCurrentCsv { get; } = new(CurrentActivityStatus.NoCurrentCsv, null, null, []);

    /// <summary>What loading found.</summary>
    public CurrentActivityStatus Status { get; }

    /// <summary>The current CSV's file name; null exactly when <see cref="Status"/> is <see cref="CurrentActivityStatus.NoCurrentCsv"/>.</summary>
    public string? FileName { get; }

    /// <summary>The header box values; set only for <see cref="CurrentActivityStatus.NoTrackpoints"/> and <see cref="CurrentActivityStatus.Loaded"/>.</summary>
    public ActivitySummary? Summary { get; }

    /// <summary>The rows in file order; non-empty only for <see cref="CurrentActivityStatus.Loaded"/>.</summary>
    public IReadOnlyList<ActivityCsvRow> Rows { get; }

    /// <summary>The current CSV's file is missing.</summary>
    /// <param name="fileName">The recorded file name.</param>
    /// <returns>The value.</returns>
    public static CurrentActivity Missing(string fileName) => Failed(CurrentActivityStatus.Missing, fileName);

    /// <summary>The current CSV could not be opened or read.</summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The value.</returns>
    public static CurrentActivity Unreadable(string fileName) => Failed(CurrentActivityStatus.Unreadable, fileName);

    /// <summary>The current CSV is not in the format this app writes.</summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The value.</returns>
    public static CurrentActivity WrongFormat(string fileName) => Failed(CurrentActivityStatus.WrongFormat, fileName);

    /// <summary>A CSV that read successfully: <see cref="CurrentActivityStatus.NoTrackpoints"/> for no rows, otherwise <see cref="CurrentActivityStatus.Loaded"/>.</summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="rows">The rows, in file order.</param>
    /// <returns>The value.</returns>
    public static CurrentActivity Read(string fileName, IReadOnlyList<ActivityCsvRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var summary = ActivitySummary.From(fileName, rows.Count > 0 ? rows[0] : null);
        return rows.Count == 0
            ? new(CurrentActivityStatus.NoTrackpoints, fileName, summary, [])
            : new(CurrentActivityStatus.Loaded, fileName, summary, rows);
    }

    private static CurrentActivity Failed(CurrentActivityStatus status, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return new(status, fileName, null, []);
    }
}
