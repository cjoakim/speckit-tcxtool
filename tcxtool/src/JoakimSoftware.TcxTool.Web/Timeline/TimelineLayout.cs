using System.Globalization;

using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Web.Timeline;

/// <summary>
/// The whole Timeline drawing as numbers (specs/007-timeline-page/data-model.md "TimelineLayout";
/// research.md R4, R5). The browser only scales these fractions to its width, so every rule here is
/// tested without a browser. It is serialized to the page as contracts/timeline-data.md describes.
/// </summary>
public sealed class TimelineLayout
{
    private TimelineLayout(double totalSeconds, IReadOnlyList<TimelineSegment> segments, IReadOnlyList<TimelineChart> charts)
    {
        TotalSeconds = totalSeconds;
        Segments = segments;
        Charts = charts;
    }

    /// <summary>The left-to-right sum of every set's seconds.</summary>
    public double TotalSeconds { get; }

    /// <summary>The 16 States CSV header names, in file order.</summary>
    public IReadOnlyList<string> FieldNames { get; } = StatesCsvWriter.Header.Split('|');

    /// <summary>One segment per StateSet, in file order.</summary>
    public IReadOnlyList<TimelineSegment> Segments { get; }

    /// <summary>Exactly three charts: Run Cadence, Heart Rate, and Altitude, in that order.</summary>
    public IReadOnlyList<TimelineChart> Charts { get; }

    /// <summary>Builds the layout of <paramref name="rows"/>.</summary>
    /// <param name="rows">The States CSV rows, in file order; at least one.</param>
    /// <returns>The layout.</returns>
    /// <exception cref="ArgumentException"><paramref name="rows"/> is empty.</exception>
    public static TimelineLayout From(IReadOnlyList<StatesCsvRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
        {
            throw new ArgumentException("A timeline needs at least one StateSet.", nameof(rows));
        }

        var seconds = new double[rows.Count];
        var total = 0.0;
        for (var i = 0; i < rows.Count; i++)
        {
            seconds[i] = Number(rows[i].StateElapsedSeconds) is { } value && value >= 0 ? value : 0.0;
            total += seconds[i];
        }

        var segments = new TimelineSegment[rows.Count];
        var before = 0.0;
        for (var i = 0; i < rows.Count; i++)
        {
            // When every set is 0 seconds, the sets share the bar equally (spec FR-013).
            var start = total > 0 ? before / total : (double)i / rows.Count;
            before += seconds[i];
            var end = i == rows.Count - 1 ? 1.0 : total > 0 ? before / total : (double)(i + 1) / rows.Count;
            segments[i] = new TimelineSegment(rows[i].State, start, end, (start + end) / 2, Values(rows[i]));
        }

        TimelineChart[] charts =
        [
            Chart("Run Cadence", "AverageRunCadence", rows, segments, row => row.AverageRunCadence),
            Chart("Heart Rate", "AverageHeartRateBpm", rows, segments, row => row.AverageHeartRateBpm),
            Chart("Altitude", "AverageAltitudeMeters", rows, segments, row => row.AverageAltitudeMeters),
        ];
        return new TimelineLayout(total, segments, charts);
    }

    // A set with no finite value is skipped, so the line joins its neighbours (spec FR-021, research.md R6).
    private static TimelineChart Chart(
        string label, string field, IReadOnlyList<StatesCsvRow> rows, TimelineSegment[] segments, Func<StatesCsvRow, string> value)
    {
        var points = new List<TimelinePoint>(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            if (Number(value(rows[i])) is { } number)
            {
                points.Add(new TimelinePoint(segments[i].Center, number));
            }
        }

        if (points.Count == 0)
        {
            return new TimelineChart(label, field, null, null, points);
        }

        var min = points[0].Value;
        var max = points[0].Value;
        foreach (var point in points)
        {
            min = Math.Min(min, point.Value);
            max = Math.Max(max, point.Value);
        }

        // All values equal: a small range around the value (spec FR-020).
        return min == max
            ? new TimelineChart(label, field, min - 1, max + 1, points)
            : new TimelineChart(label, field, min, max, points);
    }

    // The invariant, finite-only parse shared by the seconds and the chart values. Anything else is
    // null: for seconds it counts as 0 (spec FR-013), and for a chart the set is skipped.
    private static double? Number(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)
            ? value
            : null;

    private static string[] Values(StatesCsvRow row) =>
    [
        row.ActivityId, row.ActivityDistanceMiles, row.ActivityElapsedTime, row.ActivityElapsedSeconds, row.State,
        row.StateEndDate, row.StateEndTrackpointNum, row.StateTrackpointCount, row.StateElapsedSeconds,
        row.StateDistanceMiles, row.AverageHeartRateBpm, row.AverageRunCadence, row.AverageLatitude,
        row.AverageLongitude, row.AverageAltitudeMeters, row.AverageAltitudeFeet,
    ];
}
