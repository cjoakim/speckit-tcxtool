using System.Globalization;

using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Parser.Tcx;
using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Verifies <see cref="ActivityCsvWriter"/> against the golden files and the 19-field contract
/// (specs/006-activity-states/contracts/csv-format.md; feature 002 SC-003, SC-004; feature 006 FR-001 to FR-005).
/// </summary>
public class ActivityCsvWriterTests
{
    private const string ExpectedHeader =
        "ActivityId|Date|DeviceName|DeviceId|Sport|TrackpointNum|Time|ElapsedSeconds|ElapsedTime|"
        + "Latitude|Longitude|HeartRateBpm|RunCadence|State|StateSeconds|DistanceMeters|DistanceMiles|AltitudeMeters|AltitudeFeet";

    public static TheoryData<string, string, int, string> GoldenCases => new()
    {
        { "run-two-laps.tcx", "run-two-laps", 0, "run-two-laps.csv" },
        { "ride-positions.tcx", "ride-positions", 0, "ride-positions.csv" },
        { "multi-activity.tcx", "multi-activity", 0, "2026-11-01_071500_multi-activity.csv" },
        { "multi-activity.tcx", "multi-activity", 1, "2026-11-01_183005_multi-activity.csv" },
        { "fractional-times.tcx", "fractional-times", 0, "fractional-times.csv" },
        { "missing-optional.tcx", "missing-optional", 0, "missing-optional.csv" },
        { "no-trackpoints.tcx", "no-trackpoints", 0, "no-trackpoints.csv" },
        { "run-states.tcx", "run-states", 0, "run-states.csv" },
    };

    [Fact]
    public void HeaderMatchesTheContract()
    {
        ActivityCsvWriter.Header.Should().Be(ExpectedHeader);
    }

    [Theory]
    [MemberData(nameof(GoldenCases))]
    public async Task OutputEqualsTheGoldenFileAsync(string fixture, string activityId, int activityIndex, string golden)
    {
        var (text, rows) = await WriteAsync(fixture, activityId, activityIndex, CancellationToken.None);

        text.Should().Be(Fixture.ReadExpected(golden));
        rows.Should().Be(text.Count(c => c == '\n') - 1);
    }

    [Theory]
    [MemberData(nameof(GoldenCases))]
    public async Task EveryLineHasNineteenFieldsAndEndsWithLineFeedAsync(string fixture, string activityId, int activityIndex, string golden)
    {
        _ = golden;
        var (text, _) = await WriteAsync(fixture, activityId, activityIndex, CancellationToken.None);

        text.Should().EndWith("\n").And.NotContain("\r");
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Should().OnlyContain(line => line.Split('|').Length == 19);
    }

    [Theory]
    [InlineData(59.0, "59", "118")]
    [InlineData(59.5, "59.5", "119")]
    [InlineData(0.0, "0", "0")]
    public void RunCadenceIsTwiceTheTcxValue(double value, string text, string expected)
    {
        var rows = Rows(("2026-11-01T06:00:00.000Z", new RecordedNumber(text, value)));

        rows[0].RunCadence.Should().Be(expected);
    }

    [Fact]
    public void AMissingRunCadenceIsEmptyAndUnknown()
    {
        var rows = Rows(("2026-11-01T06:00:00.000Z", null));

        rows[0].RunCadence.Should().BeEmpty();
        rows[0].State.Should().Be(ActivityState.Unknown);
    }

    // contracts/csv-format.md "Worked example": a new set starts at the previous set's last timed row.
    [Fact]
    public void StateSecondsCountFromTheEndOfThePreviousStateSet()
    {
        var rows = Rows(
            ("2026-11-01T06:00:00.000Z", Cadence(70)),
            ("2026-11-01T06:00:01.000Z", Cadence(70)),
            ("2026-11-01T06:00:02.000Z", Cadence(80)),
            ("2026-11-01T06:00:03.000Z", Cadence(80)),
            ("2026-11-01T06:00:04.000Z", null));

        rows.Select(row => row.State).Should().Equal("walking", "walking", "running", "running", "unknown");
        rows.Select(row => row.StateSeconds).Should().Equal("0", "1", "1", "2", "1");
    }

    [Fact]
    public void AnUntimedRowHasNoStateSecondsAndTheSetStartsAtTheLastTimedRowBeforeIt()
    {
        var rows = Rows(
            ("2026-11-01T06:00:00.000Z", Cadence(70)),
            ("2026-11-01T06:00:01.000Z", Cadence(70)),
            (null, Cadence(80)),
            ("2026-11-01T06:00:03.000Z", Cadence(80)));

        rows.Select(row => row.State).Should().Equal("walking", "walking", "running", "running");
        rows.Select(row => row.StateSeconds).Should().Equal("0", "1", "", "2");
    }

    // 3725.6 - 0.3 is 3725.2999999999997 as doubles; durations give exactly 3725.3 (research R2).
    [Fact]
    public void StateSecondsAreExactForFractionalTimes()
    {
        var rows = Rows(
            ("2026-11-01T06:00:00.000Z", Cadence(0)),
            ("2026-11-01T06:00:00.300Z", Cadence(0)),
            ("2026-11-01T07:02:05.600Z", Cadence(70)));

        rows[2].ElapsedSeconds.Should().Be("3725.6");
        rows[2].StateSeconds.Should().Be("3725.3");
    }

    [Fact]
    public async Task WritingTheRowsGivesTheSameTextAsWritingTheActivityAsync()
    {
        var document = await TcxParserTests.ParseFixtureAsync("run-states.tcx", CancellationToken.None);
        var activity = document.Activities[0];
        using var direct = new StringWriter(CultureInfo.InvariantCulture);
        using var viaRows = new StringWriter(CultureInfo.InvariantCulture);

        ActivityCsvWriter.Write(activity, "run-states", direct);
        var rows = ActivityCsvWriter.Rows(activity, "run-states");
        ActivityCsvWriter.Write(rows, viaRows);

        viaRows.ToString().Should().Be(direct.ToString());
        rows.Should().HaveCount(activity.Laps.Sum(lap => lap.Trackpoints.Count));
    }

    [Fact]
    public async Task ReturnsTheTrackpointCountAsync()
    {
        var (_, rows) = await WriteAsync("run-two-laps.tcx", "run-two-laps", 0, CancellationToken.None);

        rows.Should().Be(10);
    }

    [Fact]
    public async Task AnActivityWithoutTrackpointsWritesOnlyTheHeaderAsync()
    {
        var (text, rows) = await WriteAsync("no-trackpoints.tcx", "no-trackpoints", 0, CancellationToken.None);

        text.Should().Be(ExpectedHeader + "\n");
        rows.Should().Be(0);
    }

    [Fact]
    public async Task OutputIgnoresTheCurrentCultureAsync()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            var (text, _) = await WriteAsync("fractional-times.tcx", "fractional-times", 0, CancellationToken.None);

            text.Should().Be(Fixture.ReadExpected("fractional-times.csv"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private static RecordedNumber Cadence(double value) =>
        new(value.ToString("R", CultureInfo.InvariantCulture), value);

    private static IReadOnlyList<ActivityCsvRow> Rows(params (string? Time, RecordedNumber? Cadence)[] points)
    {
        var trackpoints = points
            .Select(point => new TcxTrackpoint(
                point.Time is null ? null : new RecordedTime(point.Time, DateTimeOffset.Parse(point.Time, CultureInfo.InvariantCulture)),
                null, null, null, null, point.Cadence, null, null))
            .ToList();
        var start = new RecordedTime("2026-11-01T06:00:00.000Z", DateTimeOffset.Parse("2026-11-01T06:00:00.000Z", CultureInfo.InvariantCulture));
        var lap = new TcxLap(start, null, null, null, null, null, null, null, null, trackpoints);
        return ActivityCsvWriter.Rows(new TcxActivity(start, "Running", null, [lap]), "inline");
    }

    internal static async Task<(string Text, int Rows)> WriteAsync(string fixture, string activityId, int activityIndex, CancellationToken cancellationToken)
    {
        var document = await TcxParserTests.ParseFixtureAsync(fixture, cancellationToken);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        var rows = ActivityCsvWriter.Write(document.Activities[activityIndex], activityId, output);
        return (output.ToString(), rows);
    }
}
