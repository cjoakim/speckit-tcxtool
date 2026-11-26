using System.Globalization;

using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Web.Timeline;

/// <summary>
/// The Timeline page's header card values (specs/007-timeline-page/data-model.md "TimelineHeader";
/// spec FR-010, research.md R13).
/// </summary>
/// <param name="FileName">The current CSV's file name.</param>
/// <param name="ActivityId">The first StateSet's ActivityId.</param>
/// <param name="Date">The file name's leading <c>yyyy-MM-dd</c>, or empty when it has none.</param>
/// <param name="ActivityDistanceMiles">The last non-empty ActivityDistanceMiles, or empty.</param>
/// <param name="ActivityElapsedTime">The last non-empty ActivityElapsedTime, or empty.</param>
public sealed record TimelineHeader(
    string FileName, string ActivityId, string Date, string ActivityDistanceMiles, string ActivityElapsedTime)
{
    private const int DateLength = 10;

    /// <summary>Builds the header from the current CSV's name and its States CSV rows.</summary>
    /// <param name="csvFileName">The current CSV's file name.</param>
    /// <param name="rows">The States CSV rows, in file order; at least one.</param>
    /// <returns>The header.</returns>
    /// <exception cref="ArgumentException"><paramref name="rows"/> is empty.</exception>
    public static TimelineHeader From(string csvFileName, IReadOnlyList<StatesCsvRow> rows)
    {
        ArgumentNullException.ThrowIfNull(csvFileName);
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
        {
            throw new ArgumentException("A timeline needs at least one StateSet.", nameof(rows));
        }

        return new TimelineHeader(
            csvFileName,
            rows[0].ActivityId,
            LeadingDate(csvFileName),
            LastNonEmpty(rows, row => row.ActivityDistanceMiles),
            LastNonEmpty(rows, row => row.ActivityElapsedTime));
    }

    private static string LeadingDate(string fileName) =>
        fileName.Length >= DateLength
        && DateOnly.TryParseExact(fileName[..DateLength], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
            ? fileName[..DateLength]
            : string.Empty;

    private static string LastNonEmpty(IReadOnlyList<StatesCsvRow> rows, Func<StatesCsvRow, string> field)
    {
        for (var i = rows.Count - 1; i >= 0; i--)
        {
            if (field(rows[i]).Length > 0)
            {
                return field(rows[i]);
            }
        }

        return string.Empty;
    }
}
