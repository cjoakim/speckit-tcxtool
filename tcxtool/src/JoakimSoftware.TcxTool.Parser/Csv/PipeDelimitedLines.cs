namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>
/// The line reader both CSV readers share (specs/006-activity-states/research.md R8): it checks the exact
/// header, splits each later line on <c>|</c>, and stops at the first line whose field count is wrong.
/// </summary>
internal static class PipeDelimitedLines
{
    private const char Delimiter = '|';

    /// <summary>
    /// Reads the header line and every data line of <paramref name="input"/>, mapping each data line's
    /// fields with <paramref name="map"/>. Lines may end in <c>\n</c>, <c>\r\n</c>, or <c>\r</c>.
    /// </summary>
    /// <typeparam name="TRow">The row type.</typeparam>
    /// <param name="input">The CSV text.</param>
    /// <param name="header">The exact header line.</param>
    /// <param name="fieldCount">The number of fields in every data line.</param>
    /// <param name="map">Builds a row from exactly <paramref name="fieldCount"/> fields.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The rows in file order and no error, or no rows and the first error.</returns>
    public static async Task<(IReadOnlyList<TRow> Rows, ActivityCsvError? Error)> ReadAsync<TRow>(
        TextReader input,
        string header,
        int fieldCount,
        Func<string[], TRow> map,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var first = await input.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (!string.Equals(first, header, StringComparison.Ordinal))
        {
            return ([], new(ActivityCsvErrorKind.HeaderMismatch, 1));
        }

        var rows = new List<TRow>();
        var lineNumber = 1;
        while (await input.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            lineNumber++;
            var fields = line.Split(Delimiter);
            if (fields.Length != fieldCount)
            {
                return ([], new(ActivityCsvErrorKind.FieldCount, lineNumber));
            }

            rows.Add(map(fields));
        }

        return (rows, null);
    }
}
