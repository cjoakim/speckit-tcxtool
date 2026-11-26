namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>
/// Reads back exactly the 16-field format <see cref="StatesCsvWriter"/> writes
/// (specs/007-timeline-page/contracts/states-csv-format.md). A feature 006 file is a header mismatch. It never throws for bad content: anything
/// that is not a States CSV is a typed <see cref="ActivityCsvError"/>.
/// </summary>
public static class StatesCsvReader
{
    /// <summary>The number of <c>|</c>-delimited fields in every line, <see cref="StatesCsvWriter.FieldCount"/>.</summary>
    public const int FieldCount = StatesCsvWriter.FieldCount;

    /// <summary>
    /// Reads the header line and every data line of <paramref name="input"/>. Lines may end in
    /// <c>\n</c>, <c>\r\n</c>, or <c>\r</c>. Fields are kept exactly as written. Reading stops at the
    /// first bad line, and a failure carries no rows.
    /// </summary>
    /// <param name="input">The CSV text. A byte order mark is the caller's reader's concern.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The rows in file order, or the first error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
    /// <exception cref="OperationCanceledException">The read was cancelled.</exception>
    public static async Task<StatesCsvReadResult> ReadAsync(TextReader input, CancellationToken cancellationToken)
    {
        var (rows, error) = await PipeDelimitedLines.ReadAsync(
            input, StatesCsvWriter.Header, FieldCount, ToRow, cancellationToken).ConfigureAwait(false);
        return error is null ? StatesCsvReadResult.Success(rows) : StatesCsvReadResult.Failure(error);
    }

    private static StatesCsvRow ToRow(string[] fields) => new(
        fields[0], fields[1], fields[2], fields[3], fields[4], fields[5], fields[6], fields[7], fields[8],
        fields[9], fields[10], fields[11], fields[12], fields[13], fields[14], fields[15]);
}
