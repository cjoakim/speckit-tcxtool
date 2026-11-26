using System.Globalization;

namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>
/// Groups an activity's rows into StateSets and writes one States CSV line per set
/// (specs/007-timeline-page/contracts/states-csv-format.md, 16 fields). Every value comes from the activity
/// CSV's written values: end values are copied as text, and means and distances are calculated from
/// the parsed text (research R2).
/// </summary>
public static class StatesCsvWriter
{
    /// <summary>The exact header line, without its line feed.</summary>
    public const string Header =
        "ActivityId|ActivityDistanceMiles|ActivityElapsedTime|ActivityElapsedSeconds|State|StateEndDate|"
        + "StateEndTrackpointNum|StateTrackpointCount|StateElapsedSeconds|StateDistanceMiles|"
        + "AverageHeartRateBpm|AverageRunCadence|AverageLatitude|AverageLongitude|AverageAltitudeMeters|"
        + "AverageAltitudeFeet";

    /// <summary>The number of <c>|</c>-delimited fields in every line.</summary>
    public const int FieldCount = 16;

    private const char Delimiter = '|';
    private const char LineFeed = '\n';

    /// <summary>
    /// Groups consecutive rows with the same <see cref="ActivityCsvRow.State"/> into StateSets, in
    /// order, and summarizes each.
    /// </summary>
    /// <param name="rows">An activity's rows, in file order.</param>
    /// <returns>One row per StateSet; empty when <paramref name="rows"/> is empty.</returns>
    public static IReadOnlyList<StatesCsvRow> Rows(IReadOnlyList<ActivityCsvRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        ActivityCsvRow[] all = [.. rows];
        var states = new List<StatesCsvRow>();
        // The last finite DistanceMiles before the current set; null when there is none.
        double? milesBefore = null;
        var start = 0;
        while (start < all.Length)
        {
            var end = start;
            while (end + 1 < all.Length && string.Equals(all[end + 1].State, all[start].State, StringComparison.Ordinal))
            {
                end++;
            }

            var set = new ArraySegment<ActivityCsvRow>(all, start, end - start + 1);
            var milesAtEnd = LastFinite(set, row => row.DistanceMiles);
            // The activity values are copied as written, like every other end value (feature 007 research R1).
            states.Add(new StatesCsvRow(
                set[0].ActivityId,
                LastNonEmpty(set, row => row.DistanceMiles),
                LastNonEmpty(set, row => row.ElapsedTime),
                LastNonEmpty(set, row => row.ElapsedSeconds),
                set[0].State,
                LastNonEmpty(set, row => row.Time),
                set[^1].TrackpointNum,
                set.Count.ToString(CultureInfo.InvariantCulture),
                LastNonEmpty(set, row => row.StateSeconds),
                milesAtEnd is { } miles ? DerivedNumber.Format(miles - (milesBefore ?? 0.0)) : string.Empty,
                Mean(set, row => row.HeartRateBpm),
                Mean(set, row => row.RunCadence),
                Mean(set, row => row.Latitude),
                Mean(set, row => row.Longitude),
                Mean(set, row => row.AltitudeMeters),
                Mean(set, row => row.AltitudeFeet)));

            milesBefore = milesAtEnd ?? milesBefore;

            start = end + 1;
        }

        return states;
    }

    /// <summary>Writes the header and one line per row, applying <see cref="CsvText.Clean"/> to every field.</summary>
    /// <param name="rows">The rows, usually from <see cref="Rows"/>.</param>
    /// <param name="output">The writer; every line ends with <c>\n</c>.</param>
    /// <returns>The number of data rows written.</returns>
    public static int Write(IReadOnlyList<StatesCsvRow> rows, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(output);

        output.Write(Header);
        output.Write(LineFeed);
        foreach (var row in rows)
        {
            string[] fields =
            [
                row.ActivityId, row.ActivityDistanceMiles, row.ActivityElapsedTime, row.ActivityElapsedSeconds,
                row.State, row.StateEndDate, row.StateEndTrackpointNum, row.StateTrackpointCount,
                row.StateElapsedSeconds, row.StateDistanceMiles, row.AverageHeartRateBpm, row.AverageRunCadence,
                row.AverageLatitude, row.AverageLongitude, row.AverageAltitudeMeters, row.AverageAltitudeFeet,
            ];
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

        return rows.Count;
    }

    private static string LastNonEmpty(ArraySegment<ActivityCsvRow> set, Func<ActivityCsvRow, string> field)
    {
        for (var i = set.Count - 1; i >= 0; i--)
        {
            var value = field(set[i]);
            if (value.Length > 0)
            {
                return value;
            }
        }

        return string.Empty;
    }

    // A left-to-right sum of the finite values divided by their count (research R2).
    private static string Mean(ArraySegment<ActivityCsvRow> set, Func<ActivityCsvRow, string> field)
    {
        var total = 0.0;
        var count = 0;
        foreach (var row in set)
        {
            if (Parse(field(row)) is { } value)
            {
                total += value;
                count++;
            }
        }

        return count == 0 ? string.Empty : DerivedNumber.Format(total / count);
    }

    private static double? LastFinite(ArraySegment<ActivityCsvRow> set, Func<ActivityCsvRow, string> field)
    {
        for (var i = set.Count - 1; i >= 0; i--)
        {
            if (Parse(field(set[i])) is { } value)
            {
                return value;
            }
        }

        return null;
    }

    private static double? Parse(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)
            ? value
            : null;
}
