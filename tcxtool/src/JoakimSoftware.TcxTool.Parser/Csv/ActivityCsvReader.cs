namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>
/// Reads back exactly the 19-field format <see cref="ActivityCsvWriter"/> writes
/// (specs/005-table-page/contracts/activity-csv-reader.md, with the fields of
/// specs/006-activity-states/contracts/csv-format.md). It never throws for bad content: anything that
/// is not an activity CSV is a typed <see cref="ActivityCsvError"/>. A file written before feature 006,
/// with 17 fields, has a different header, so it is a <see cref="ActivityCsvErrorKind.HeaderMismatch"/>.
/// </summary>
public static class ActivityCsvReader
{
    /// <summary>The number of <c>|</c>-delimited fields in every line, <see cref="ActivityCsvWriter.FieldCount"/>.</summary>
    public const int FieldCount = ActivityCsvWriter.FieldCount;

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
    public static async Task<ActivityCsvReadResult> ReadAsync(TextReader input, CancellationToken cancellationToken)
    {
        var (rows, error) = await PipeDelimitedLines.ReadAsync(
            input, ActivityCsvWriter.Header, FieldCount, ToRow, cancellationToken).ConfigureAwait(false);
        return error is null ? ActivityCsvReadResult.Success(rows) : ActivityCsvReadResult.Failure(error);
    }

    private static ActivityCsvRow ToRow(string[] fields) => new(
        fields[0], fields[1], fields[2], fields[3], fields[4], fields[5], fields[6], fields[7], fields[8],
        fields[9], fields[10], fields[11], fields[12], fields[13], fields[14], fields[15], fields[16],
        fields[17], fields[18]);
}
