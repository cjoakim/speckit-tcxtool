using System.Globalization;

using JoakimSoftware.TcxTool.Parser.Tcx;

namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>
/// Writes one activity as a <c>|</c>-delimited CSV: a header line, then one line per trackpoint with
/// the 19 fields in specs/006-activity-states/contracts/csv-format.md.
/// </summary>
public static class ActivityCsvWriter
{
    /// <summary>The exact header line, without its line feed.</summary>
    public const string Header =
        "ActivityId|Date|DeviceName|DeviceId|Sport|TrackpointNum|Time|ElapsedSeconds|ElapsedTime|"
        + "Latitude|Longitude|HeartRateBpm|RunCadence|State|StateSeconds|DistanceMeters|DistanceMiles|"
        + "AltitudeMeters|AltitudeFeet";

    /// <summary>The number of <c>|</c>-delimited fields in every line.</summary>
    public const int FieldCount = 19;

    private const char Delimiter = '|';
    private const char LineFeed = '\n';
    private const double MetersPerMile = 1609.344;
    private const double MetersPerFoot = 0.3048;
    private const double StepsPerCadenceUnit = 2;

    /// <summary>
    /// Builds one row per trackpoint of <paramref name="activity"/>, in file order. Fields are not yet
    /// cleaned of <c>|</c> and line breaks; <see cref="Write(IReadOnlyList{ActivityCsvRow}, TextWriter)"/> does that.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="activityId">The ActivityId of every row.</param>
    /// <returns>The rows; an empty field is <c>""</c>.</returns>
    public static IReadOnlyList<ActivityCsvRow> Rows(TcxActivity activity, string activityId)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(activityId);

        var trackpoints = activity.Laps.SelectMany(lap => lap.Trackpoints).ToList();
        // Elapsed time counts from the first trackpoint that has a time.
        var reference = trackpoints.FirstOrDefault(trackpoint => trackpoint.Time is not null)?.Time!.Value;
        var date = CsvFileName.Date(activity);

        var rows = new List<ActivityCsvRow>(trackpoints.Count);
        string? previousState = null;
        TimeSpan? lastTimedElapsed = null;
        // A StateSet starts where the previous one's last timed row was, or at 0.
        var setStart = TimeSpan.Zero;
        foreach (var trackpoint in trackpoints)
        {
            var stepsPerMinute = trackpoint.RunCadence?.Value * StepsPerCadenceUnit;
            var state = ActivityState.For(stepsPerMinute);
            if (previousState is not null && !string.Equals(state, previousState, StringComparison.Ordinal))
            {
                setStart = lastTimedElapsed ?? TimeSpan.Zero;
            }

            previousState = state;
            var elapsed = trackpoint.Time?.Value - reference;
            if (elapsed is not null)
            {
                lastTimedElapsed = elapsed;
            }

            rows.Add(new ActivityCsvRow(
                activityId,
                date,
                activity.Creator?.Name ?? string.Empty,
                activity.Creator?.UnitId ?? string.Empty,
                activity.Sport ?? string.Empty,
                (rows.Count + 1).ToString(CultureInfo.InvariantCulture),
                trackpoint.Time?.Text ?? string.Empty,
                elapsed is { } seconds ? ElapsedTime.FormatSeconds(seconds) : string.Empty,
                elapsed is { } clock ? ElapsedTime.FormatClock(clock) : string.Empty,
                trackpoint.Position?.LatitudeDegrees.Text ?? string.Empty,
                trackpoint.Position?.LongitudeDegrees.Text ?? string.Empty,
                trackpoint.HeartRateBpm?.Text ?? string.Empty,
                stepsPerMinute is { } steps ? DerivedNumber.Format(steps) : string.Empty,
                state,
                elapsed is { } inSet ? ElapsedTime.FormatSeconds(inSet - setStart) : string.Empty,
                trackpoint.DistanceMeters?.Text ?? string.Empty,
                Derived(trackpoint.DistanceMeters, MetersPerMile),
                trackpoint.AltitudeMeters?.Text ?? string.Empty,
                Derived(trackpoint.AltitudeMeters, MetersPerFoot)));
        }

        return rows;
    }

    /// <summary>Writes the header and one line per row, applying <see cref="CsvText.Clean"/> to every field.</summary>
    /// <param name="rows">The rows, usually from <see cref="Rows"/>.</param>
    /// <param name="output">The writer; every line ends with <c>\n</c>.</param>
    /// <returns>The number of data rows written.</returns>
    public static int Write(IReadOnlyList<ActivityCsvRow> rows, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(output);

        output.Write(Header);
        output.Write(LineFeed);
        foreach (var row in rows)
        {
            WriteLine(output,
            [
                row.ActivityId, row.Date, row.DeviceName, row.DeviceId, row.Sport, row.TrackpointNum, row.Time,
                row.ElapsedSeconds, row.ElapsedTime, row.Latitude, row.Longitude, row.HeartRateBpm, row.RunCadence,
                row.State, row.StateSeconds, row.DistanceMeters, row.DistanceMiles, row.AltitudeMeters, row.AltitudeFeet,
            ]);
        }

        return rows.Count;
    }

    /// <summary>Writes the header and one line per trackpoint of <paramref name="activity"/>.</summary>
    /// <param name="activity">The activity.</param>
    /// <param name="activityId">The ActivityId written in every row.</param>
    /// <param name="output">The writer; every line ends with <c>\n</c>.</param>
    /// <returns>The number of data rows written.</returns>
    public static int Write(TcxActivity activity, string activityId, TextWriter output) =>
        Write(Rows(activity, activityId), output);

    private static string Derived(RecordedNumber? meters, double metersPerUnit) =>
        meters is null ? string.Empty : DerivedNumber.Format(meters.Value / metersPerUnit);

    private static void WriteLine(TextWriter output, string[] fields)
    {
        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
            {
                output.Write(Delimiter);
            }

            output.Write(CsvText.Clean(fields[i]));
        }

        output.Write(LineFeed);
    }
}
