using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Web.Analysis;

/// <summary>
/// The current CSV's States CSV as the States page sees it (specs/006-activity-states/data-model.md
/// <c>CurrentStates</c>). Each status is built by its own member, so it can't come with the wrong
/// properties.
/// </summary>
public sealed record CurrentStates
{
    private CurrentStates(CurrentStatesStatus status, string? csvFileName, string? statesFileName, IReadOnlyList<StatesCsvRow> rows)
    {
        Status = status;
        CsvFileName = csvFileName;
        StatesFileName = statesFileName;
        Rows = rows;
    }

    /// <summary>There is no current CSV.</summary>
    public static CurrentStates NoCurrentCsv { get; } = new(CurrentStatesStatus.NoCurrentCsv, null, null, []);

    /// <summary>What loading found.</summary>
    public CurrentStatesStatus Status { get; }

    /// <summary>The current CSV's file name; null exactly when <see cref="Status"/> is <see cref="CurrentStatesStatus.NoCurrentCsv"/>.</summary>
    public string? CsvFileName { get; }

    /// <summary>
    /// The States CSV's file name; null exactly when <see cref="Status"/> is
    /// <see cref="CurrentStatesStatus.NoCurrentCsv"/> or <see cref="CurrentStatesStatus.NoStatesCsv"/>.
    /// </summary>
    public string? StatesFileName { get; }

    /// <summary>The rows in file order; non-empty exactly when <see cref="Status"/> is <see cref="CurrentStatesStatus.Loaded"/>.</summary>
    public IReadOnlyList<StatesCsvRow> Rows { get; }

    /// <summary>The current CSV has no States CSV of its own.</summary>
    /// <param name="csvFileName">The current CSV's file name.</param>
    /// <returns>The value.</returns>
    public static CurrentStates NoStatesCsv(string csvFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvFileName);
        return new(CurrentStatesStatus.NoStatesCsv, csvFileName, null, []);
    }

    /// <summary>The States CSV is missing.</summary>
    /// <param name="csvFileName">The current CSV's file name.</param>
    /// <param name="statesFileName">The States CSV's file name.</param>
    /// <returns>The value.</returns>
    public static CurrentStates Missing(string csvFileName, string statesFileName) =>
        Failed(CurrentStatesStatus.Missing, csvFileName, statesFileName);

    /// <summary>The States CSV could not be opened or read.</summary>
    /// <param name="csvFileName">The current CSV's file name.</param>
    /// <param name="statesFileName">The States CSV's file name.</param>
    /// <returns>The value.</returns>
    public static CurrentStates Unreadable(string csvFileName, string statesFileName) =>
        Failed(CurrentStatesStatus.Unreadable, csvFileName, statesFileName);

    /// <summary>The States CSV is not in the format this app writes.</summary>
    /// <param name="csvFileName">The current CSV's file name.</param>
    /// <param name="statesFileName">The States CSV's file name.</param>
    /// <returns>The value.</returns>
    public static CurrentStates WrongFormat(string csvFileName, string statesFileName) =>
        Failed(CurrentStatesStatus.WrongFormat, csvFileName, statesFileName);

    /// <summary>A States CSV that read successfully: <see cref="CurrentStatesStatus.NoStateSets"/> for no rows, otherwise <see cref="CurrentStatesStatus.Loaded"/>.</summary>
    /// <param name="csvFileName">The current CSV's file name.</param>
    /// <param name="statesFileName">The States CSV's file name.</param>
    /// <param name="rows">The rows, in file order.</param>
    /// <returns>The value.</returns>
    public static CurrentStates Read(string csvFileName, string statesFileName, IReadOnlyList<StatesCsvRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return rows.Count == 0
            ? Failed(CurrentStatesStatus.NoStateSets, csvFileName, statesFileName)
            : Validated(CurrentStatesStatus.Loaded, csvFileName, statesFileName, rows);
    }

    private static CurrentStates Failed(CurrentStatesStatus status, string csvFileName, string statesFileName) =>
        Validated(status, csvFileName, statesFileName, []);

    private static CurrentStates Validated(CurrentStatesStatus status, string csvFileName, string statesFileName, IReadOnlyList<StatesCsvRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(statesFileName);
        return new(status, csvFileName, statesFileName, rows);
    }
}
