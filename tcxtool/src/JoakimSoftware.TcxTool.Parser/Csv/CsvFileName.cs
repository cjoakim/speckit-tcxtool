using System.Globalization;

using JoakimSoftware.TcxTool.Parser.Conversion;
using JoakimSoftware.TcxTool.Parser.Tcx;

namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>
/// Names the CSV written for each activity (data-model.md "Output file names"). Date and time come
/// from the activity Id as written, in its own offset.
/// </summary>
public static class CsvFileName
{
    /// <summary>
    /// Names one CSV per activity: <c>&lt;Date&gt;_&lt;ActivityId&gt;.csv</c> for a single activity,
    /// <c>&lt;Date&gt;_&lt;HHmmss&gt;_&lt;ActivityId&gt;.csv</c> for each of several.
    /// </summary>
    /// <param name="activities">The parsed activities.</param>
    /// <param name="activityId">The ActivityId from <see cref="ActivityIdName"/>.</param>
    /// <param name="fileNames">The names in activity order, or empty on failure.</param>
    /// <param name="error">The reason no names could be produced, or null.</param>
    /// <returns>
    /// True when every activity has its own name; false with
    /// <see cref="TcxErrorKind.DuplicateActivityStart"/> when two activities start in the same second.
    /// </returns>
    public static bool TryFor(
        IReadOnlyList<TcxActivity> activities,
        string activityId,
        out IReadOnlyList<string> fileNames,
        out TcxError? error)
    {
        ArgumentNullException.ThrowIfNull(activities);
        ArgumentNullException.ThrowIfNull(activityId);

        string[] names = activities.Count == 1
            ? [$"{Date(activities[0])}_{activityId}.csv"]
            : [.. activities.Select(activity =>
                $"{Date(activity)}_{activity.Id.Value.ToString("HHmmss", CultureInfo.InvariantCulture)}_{activityId}.csv")];

        if (names.Distinct(StringComparer.Ordinal).Count() != names.Length)
        {
            fileNames = [];
            error = TcxError.Of(TcxErrorKind.DuplicateActivityStart);
            return false;
        }

        fileNames = names;
        error = null;
        return true;
    }

    /// <summary>The <c>yyyy-MM-dd</c> date of <paramref name="activity"/>'s Id, in its own offset.</summary>
    /// <param name="activity">The activity.</param>
    /// <returns>The date text.</returns>
    internal static string Date(TcxActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return activity.Id.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
