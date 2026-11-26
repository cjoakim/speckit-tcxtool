using System.Globalization;
using System.Text;

using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Tests.Support;

/// <summary>
/// Writes activity CSVs in the specs/006-activity-states/contracts/csv-format.md layout, and the state file
/// that names one, for tests (spec 005 research R9). Every value is made up: no real GPS position is
/// ever written, and no row is copied from <c>data/csv/</c>.
/// </summary>
public static class ActivityCsvFixture
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Builds one 19-field data line in contract order, joined with <c>|</c>.</summary>
    /// <returns>The line, without a line feed.</returns>
    public static string Line(
        string activityId = "fixture-run",
        string date = "2026-11-01",
        string deviceName = "Forerunner 999",
        string deviceId = "77",
        string sport = "Running",
        string trackpointNum = "1",
        string time = "2026-11-01T06:00:00.000Z",
        string elapsedSeconds = "0",
        string elapsedTime = "00:00:00",
        string latitude = "1.5",
        string longitude = "2.5",
        string heartRateBpm = "120",
        string runCadence = "80",
        string state = "walking",
        string stateSeconds = "0",
        string distanceMeters = "0.0",
        string distanceMiles = "0",
        string altitudeMeters = "10.0",
        string altitudeFeet = "32.808398950131235") =>
        string.Join('|',
            activityId, date, deviceName, deviceId, sport, trackpointNum, time, elapsedSeconds, elapsedTime,
            latitude, longitude, heartRateBpm, runCadence, state, stateSeconds, distanceMeters, distanceMiles,
            altitudeMeters, altitudeFeet);

    /// <summary>
    /// Writes <see cref="ActivityCsvWriter.Header"/> and then each line, every one followed by <c>\n</c>,
    /// as UTF-8 with no byte order mark. The parent directory is created.
    /// </summary>
    /// <param name="path">The CSV path.</param>
    /// <param name="lines">The data lines.</param>
    public static void Write(string path, IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var text = new StringBuilder(ActivityCsvWriter.Header).Append('\n');
        foreach (var line in lines)
        {
            text.Append(line).Append('\n');
        }

        WriteRaw(path, text.ToString());
    }

    /// <summary>Writes <paramref name="content"/> unchanged, for malformed inputs. The parent directory is created.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="content">The exact content.</param>
    public static void WriteRaw(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, Utf8NoBom);
    }

    /// <summary>
    /// Builds <paramref name="count"/> lines whose TrackpointNum runs from 1 to <paramref name="count"/>,
    /// with made-up values that vary from row to row. The rows are one second apart, in runs of
    /// <c>150</c> walking then <c>150</c> running rows, with State and StateSeconds that follow the rule in
    /// specs/006-activity-states/contracts/csv-format.md.
    /// </summary>
    /// <param name="count">The number of lines.</param>
    /// <returns>The lines.</returns>
    public static IReadOnlyList<string> Synthetic(int count)
    {
        var start = new DateTime(2026, 11, 1, 6, 0, 0, DateTimeKind.Utc);
        var lines = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            const int runLength = 150;
            var elapsed = TimeSpan.FromSeconds(i);
            var run = i / runLength;
            var cadence = run % 2 == 0 ? 140 + (i % 10) : 160 + (i % 30);
            var setStart = run == 0 ? 0 : (run * runLength) - 1;
            lines.Add(Line(
                trackpointNum: Text(i + 1),
                time: start.AddSeconds(i).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                elapsedSeconds: Text(i),
                elapsedTime: string.Create(CultureInfo.InvariantCulture, $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"),
                latitude: Text(1.5 + (i * 1e-6)),
                longitude: Text(2.5 - (i * 1e-6)),
                heartRateBpm: Text(80 + (i % 110)),
                runCadence: Text(cadence),
                state: ActivityState.For(cadence),
                stateSeconds: Text(i - setStart),
                distanceMeters: Text(i * 2.7),
                distanceMiles: Text(i * 2.7 / 1609.344),
                altitudeMeters: Text(10 + (i % 50 * 0.2)),
                altitudeFeet: Text((10 + (i % 50 * 0.2)) / 0.3048)));
        }

        return lines;
    }

    /// <summary>
    /// Writes the state file naming <paramref name="csvFileName"/>, and <paramref name="statesCsvFileName"/>
    /// when it is given, in the exact format of specs/004-about-and-csv-state/contracts/state-file.md and
    /// specs/006-activity-states/contracts/state-file.md. The parent directory is created.
    /// </summary>
    /// <param name="statePath">The state file path.</param>
    /// <param name="csvFileName">The CSV file name to record.</param>
    /// <param name="statesCsvFileName">The States CSV file name to record, or null to leave it out.</param>
    public static void WriteState(string statePath, string csvFileName, string? statesCsvFileName = null) =>
        WriteRaw(statePath, statesCsvFileName is null
            ? $"{{\n  \"csvFilename\": \"{csvFileName}\"\n}}\n"
            : $"{{\n  \"csvFilename\": \"{csvFileName}\",\n  \"statesCsvFilename\": \"{statesCsvFileName}\"\n}}\n");

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Text(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
