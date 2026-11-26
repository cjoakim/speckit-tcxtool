using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Web.Analysis;

/// <summary>
/// The Table page's header box values (spec 005 FR-009): the CSV file name and the activity values of
/// the first data row. Every value is non-null; an empty one is <c>""</c>.
/// </summary>
/// <param name="FileName">The current CSV's file name.</param>
/// <param name="ActivityId">The first row's ActivityId.</param>
/// <param name="Date">The first row's Date.</param>
/// <param name="DeviceName">The first row's DeviceName.</param>
/// <param name="DeviceId">The first row's DeviceId.</param>
/// <param name="Sport">The first row's Sport.</param>
public sealed record ActivitySummary(string FileName, string ActivityId, string Date, string DeviceName, string DeviceId, string Sport)
{
    /// <summary>Builds the summary from the first row, or with empty activity values when there is none.</summary>
    /// <param name="fileName">The current CSV's file name.</param>
    /// <param name="firstRow">The first data row, or null for a header-only file.</param>
    /// <returns>The summary.</returns>
    public static ActivitySummary From(string fileName, ActivityCsvRow? firstRow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return firstRow is null
            ? new(fileName, "", "", "", "", "")
            : new(fileName, firstRow.ActivityId, firstRow.Date, firstRow.DeviceName, firstRow.DeviceId, firstRow.Sport);
    }
}
