using System.Globalization;
using System.Text;

using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Tests.Support;

/// <summary>
/// Writes States CSVs in the specs/007-timeline-page/contracts/states-csv-format.md layout for tests.
/// Every value is made up: no real GPS position is ever written, and no row is copied from
/// <c>data/csv/</c>.
/// </summary>
public static class StatesCsvFixture
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Builds one 16-field data line in the order of
    /// specs/007-timeline-page/contracts/states-csv-format.md, joined with <c>|</c>.
    /// </summary>
    /// <returns>The line, without a line feed.</returns>
    public static string Line(
        string activityId = "fixture-run",
        string activityDistanceMiles = "0.05",
        string activityElapsedTime = "00:00:09",
        string activityElapsedSeconds = "9",
        string state = "walking",
        string stateEndDate = "2026-11-01T06:00:09.000Z",
        string stateEndTrackpointNum = "10",
        string stateTrackpointCount = "10",
        string stateElapsedSeconds = "9",
        string stateDistanceMiles = "0.01",
        string averageHeartRateBpm = "120",
        string averageRunCadence = "140",
        string averageLatitude = "1.5",
        string averageLongitude = "2.5",
        string averageAltitudeMeters = "10",
        string averageAltitudeFeet = "32.808398950131235") =>
        string.Join('|',
            activityId, activityDistanceMiles, activityElapsedTime, activityElapsedSeconds, state, stateEndDate,
            stateEndTrackpointNum, stateTrackpointCount, stateElapsedSeconds, stateDistanceMiles,
            averageHeartRateBpm, averageRunCadence, averageLatitude, averageLongitude, averageAltitudeMeters,
            averageAltitudeFeet);

    /// <summary>
    /// Writes <see cref="StatesCsvWriter.Header"/> and then each line, every one followed by <c>\n</c>,
    /// as UTF-8 with no byte order mark. The parent directory is created.
    /// </summary>
    /// <param name="path">The States CSV path.</param>
    /// <param name="lines">The data lines.</param>
    public static void Write(string path, IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(lines);
        var text = new StringBuilder(StatesCsvWriter.Header).Append('\n');
        foreach (var line in lines)
        {
            text.Append(line).Append('\n');
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text.ToString(), Utf8NoBom);
    }

    /// <summary>
    /// Builds <paramref name="count"/> lines that alternate walking and running, with end trackpoints
    /// 10 apart and made-up averages that vary from row to row.
    /// </summary>
    /// <param name="count">The number of lines.</param>
    /// <returns>The lines.</returns>
    public static IReadOnlyList<string> Synthetic(int count)
    {
        var start = new DateTime(2026, 11, 1, 6, 0, 0, DateTimeKind.Utc);
        var lines = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var end = TimeSpan.FromSeconds(((i + 1) * 10) - 1);
            lines.Add(Line(
                activityDistanceMiles: Text((i + 1) * 0.0125),
                activityElapsedSeconds: Text((int)end.TotalSeconds),
                state: i % 2 == 0 ? ActivityState.Walking : ActivityState.Running,
                stateEndDate: (start + end).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                stateEndTrackpointNum: Text((i + 1) * 10),
                stateTrackpointCount: "10",
                stateElapsedSeconds: Text(i == 0 ? 9 : 10),
                activityElapsedTime: string.Create(CultureInfo.InvariantCulture, $"{(int)end.TotalHours:00}:{end.Minutes:00}:{end.Seconds:00}"),
                averageLatitude: Text(1.5 + (i * 1e-5)),
                averageLongitude: Text(2.5 - (i * 1e-5)),
                averageHeartRateBpm: Text(90 + (i % 100)),
                averageRunCadence: Text(i % 2 == 0 ? 120 + (i % 30) : 160 + (i % 30)),
                stateDistanceMiles: Text(0.01 + (i % 7 * 0.001)),
                averageAltitudeMeters: Text(10 + (i % 50 * 0.2)),
                averageAltitudeFeet: Text((10 + (i % 50 * 0.2)) / 0.3048)));
        }

        return lines;
    }

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Text(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
