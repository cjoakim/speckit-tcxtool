namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>The outcome of reading a States CSV: the rows, or the first error.</summary>
public sealed record StatesCsvReadResult
{
    private StatesCsvReadResult(IReadOnlyList<StatesCsvRow> rows, ActivityCsvError? error)
    {
        Rows = rows;
        Error = error;
    }

    /// <summary>The rows in file order. Empty on failure, and empty for a header-only file.</summary>
    public IReadOnlyList<StatesCsvRow> Rows { get; }

    /// <summary>The error, or null on success.</summary>
    public ActivityCsvError? Error { get; }

    /// <summary>True exactly when <see cref="Error"/> is null.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>Creates a successful result.</summary>
    /// <param name="rows">The rows, in file order.</param>
    /// <returns>The result.</returns>
    public static StatesCsvReadResult Success(IReadOnlyList<StatesCsvRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return new(rows, null);
    }

    /// <summary>Creates a failed result, with no rows.</summary>
    /// <param name="error">The error.</param>
    /// <returns>The result.</returns>
    public static StatesCsvReadResult Failure(ActivityCsvError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new([], error);
    }
}
