namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>The outcome of reading an activity CSV: the rows, or the first error.</summary>
public sealed record ActivityCsvReadResult
{
    private ActivityCsvReadResult(IReadOnlyList<ActivityCsvRow> rows, ActivityCsvError? error)
    {
        Rows = rows;
        Error = error;
    }

    /// <summary>The rows in file order. Empty on failure, and empty for a header-only file.</summary>
    public IReadOnlyList<ActivityCsvRow> Rows { get; }

    /// <summary>The error, or null on success.</summary>
    public ActivityCsvError? Error { get; }

    /// <summary>True exactly when <see cref="Error"/> is null.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>Creates a successful result.</summary>
    /// <param name="rows">The rows, in file order.</param>
    /// <returns>The result.</returns>
    public static ActivityCsvReadResult Success(IReadOnlyList<ActivityCsvRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return new(rows, null);
    }

    /// <summary>Creates a failed result, with no rows.</summary>
    /// <param name="error">The error.</param>
    /// <returns>The result.</returns>
    public static ActivityCsvReadResult Failure(ActivityCsvError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new([], error);
    }
}
