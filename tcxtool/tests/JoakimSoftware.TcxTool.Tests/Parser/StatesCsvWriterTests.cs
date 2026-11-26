using System.Globalization;
using System.Text;

using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Verifies <see cref="StatesCsvWriter"/> against the States goldens and the rules of
/// specs/007-timeline-page/contracts/states-csv-format.md (feature 006 FR-007 to FR-012; feature 007
/// FR-001 to FR-003; data-model.md <c>StatesCsvRow</c>). Every value is made up.
/// </summary>
public class StatesCsvWriterTests
{
    private const string ExpectedHeader =
        "ActivityId|ActivityDistanceMiles|ActivityElapsedTime|ActivityElapsedSeconds|State|StateEndDate|"
        + "StateEndTrackpointNum|StateTrackpointCount|StateElapsedSeconds|StateDistanceMiles|"
        + "AverageHeartRateBpm|AverageRunCadence|AverageLatitude|AverageLongitude|AverageAltitudeMeters|"
        + "AverageAltitudeFeet";

    public static TheoryData<string> GoldenStems => new()
    {
        "2026-11-01_071500_multi-activity",
        "2026-11-01_183005_multi-activity",
        "fractional-times",
        "missing-optional",
        "no-trackpoints",
        "ride-positions",
        "run-two-laps",
        "run-states",
    };

    [Fact]
    public void HeaderAndFieldCountMatchTheContract()
    {
        StatesCsvWriter.Header.Should().Be(ExpectedHeader);
        StatesCsvWriter.FieldCount.Should().Be(16);
    }

    [Theory]
    [MemberData(nameof(GoldenStems))]
    public async Task TheStatesOfEveryActivityGoldenEqualTheStatesGoldenAsync(string stem)
    {
        using var input = new StreamReader(Fixture.ExpectedPath(stem + ".csv"), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var activity = await ActivityCsvReader.ReadAsync(input, CancellationToken.None);
        activity.IsSuccess.Should().BeTrue();

        var text = WriteText(StatesCsvWriter.Rows(activity.Rows));

        text.Should().Be(Fixture.ReadExpected(stem + ".states.csv"));
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Should().OnlyContain(line => line.Split('|').Length == 16);
    }

    // The spec's US2 scenario (contracts/states-csv-format.md "Worked example").
    [Fact]
    public void ThirtyEightWalkingThenOneHundredThirteenRunningRowsGiveTwoStateSets()
    {
        var rows = new List<ActivityCsvRow>();
        for (var i = 0; i < 151; i++)
        {
            var walking = i < 38;
            var setStart = walking ? 0 : 37;
            rows.Add(Row(i + 1, state: walking ? ActivityState.Walking : ActivityState.Running, elapsed: i, stateSeconds: Text(i - setStart)));
        }

        var states = StatesCsvWriter.Rows(rows);

        states.Select(row => (row.State, row.StateEndTrackpointNum, row.StateElapsedSeconds, row.ActivityElapsedTime))
            .Should().Equal(
                (ActivityState.Walking, "38", "37", "00:00:37"),
                (ActivityState.Running, "151", "113", "00:02:30"));
    }

    // Feature 007 FR-001: the set's row count (data-model.md StatesCsvRow.StateTrackpointCount).
    [Fact]
    public void StateTrackpointCountIsTheNumberOfRowsInEachSet()
    {
        var states = StatesCsvWriter.Rows(
        [
            Row(1), Row(2), Row(3),
            Row(4, state: ActivityState.Running),
            Row(5, state: ActivityState.Stopped), Row(6, state: ActivityState.Stopped),
        ]);

        states.Select(row => row.StateTrackpointCount).Should().Equal("3", "1", "2");
    }

    // Feature 007 FR-001: the last value in the set, copied as written (data-model.md
    // StatesCsvRow.ActivityDistanceMiles and StatesCsvRow.ActivityElapsedSeconds).
    [Fact]
    public void TheActivityValuesAreTheSetsLastNonEmptyValuesAsWritten()
    {
        var states = StatesCsvWriter.Rows(
        [
            Row(1, elapsed: 1, distanceMiles: "0.010"),
            Row(2, elapsed: 2.5, distanceMiles: "0.020"),
            Row(3, elapsed: null, distanceMiles: ""),
            Row(4, state: ActivityState.Running, elapsed: null, distanceMiles: ""),
            Row(5, state: ActivityState.Stopped, elapsed: 7, distanceMiles: "41.250"),
        ]);

        states.Select(row => (row.ActivityDistanceMiles, row.ActivityElapsedSeconds))
            .Should().Equal(("0.020", "2.5"), (string.Empty, string.Empty), ("41.250", "7"));
    }

    [Fact]
    public async Task TheCountsAddUpToTheActivityRowsAndTheLastRowEndsTheActivityAsync()
    {
        using var input = new StreamReader(Fixture.ExpectedPath("run-states.csv"), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var activity = await ActivityCsvReader.ReadAsync(input, CancellationToken.None);

        var states = StatesCsvWriter.Rows(activity.Rows);

        var total = 0;
        foreach (var state in states)
        {
            total += int.Parse(state.StateTrackpointCount, CultureInfo.InvariantCulture);
        }

        total.Should().Be(activity.Rows.Count);
        var lastMiles = activity.Rows.Last(row => row.DistanceMiles.Length > 0).DistanceMiles;
        var lastSeconds = activity.Rows.Last(row => row.ElapsedSeconds.Length > 0).ElapsedSeconds;
        (states[^1].ActivityDistanceMiles, states[^1].ActivityElapsedSeconds).Should().Be((lastMiles, lastSeconds));
    }

    [Fact]
    public void AOneRowSetsAveragesAreItsOwnValues()
    {
        var state = StatesCsvWriter.Rows([Row(1, latitude: "1.25", longitude: "-2.5", heartRate: "131", cadence: "164", altitudeMeters: "12.5", altitudeFeet: "41.25")])
            .Should().ContainSingle().Subject;

        state.Should().BeEquivalentTo(new
        {
            ActivityId = "fixture-run",
            State = ActivityState.Walking,
            StateEndTrackpointNum = "1",
            AverageLatitude = "1.25",
            AverageLongitude = "-2.5",
            AverageHeartRateBpm = "131",
            AverageRunCadence = "164",
            AverageAltitudeMeters = "12.5",
            AverageAltitudeFeet = "41.25",
        });
    }

    [Fact]
    public void ASetWithNoTimedRowHasEmptyEndTimes()
    {
        var state = StatesCsvWriter.Rows([Row(1, elapsed: null), Row(2, elapsed: null)]).Should().ContainSingle().Subject;

        state.StateEndDate.Should().BeEmpty();
        state.StateElapsedSeconds.Should().BeEmpty();
        state.ActivityElapsedTime.Should().BeEmpty();
        state.StateEndTrackpointNum.Should().Be("2");
    }

    [Fact]
    public void TheEndTimesComeFromTheSetsLastTimedRow()
    {
        var state = StatesCsvWriter.Rows([Row(1, elapsed: 4), Row(2, elapsed: 5, stateSeconds: "5"), Row(3, elapsed: null)])
            .Should().ContainSingle().Subject;

        state.StateEndDate.Should().Be("2026-11-01T06:00:05.000Z");
        state.StateElapsedSeconds.Should().Be("5");
        state.ActivityElapsedTime.Should().Be("00:00:05");
        state.StateEndTrackpointNum.Should().Be("3");
    }

    [Fact]
    public void ASetWithNoDistanceHasNoneAndTheNextSetMeasuresFromTheLastValueBeforeIt()
    {
        var states = StatesCsvWriter.Rows(
        [
            Row(1, distanceMiles: "0.5"),
            Row(2, distanceMiles: "1.5"),
            Row(3, state: ActivityState.Running, distanceMiles: ""),
            Row(4, state: ActivityState.Stopped, distanceMiles: "2"),
            Row(5, state: ActivityState.Stopped, distanceMiles: "2.25"),
        ]);

        states.Select(row => row.StateDistanceMiles).Should().Equal("1.5", "", "0.75");
    }

    [Fact]
    public void EmptyValuesAreLeftOutOfAveragesAndAnAllEmptyColumnIsEmpty()
    {
        var state = StatesCsvWriter.Rows(
        [
            Row(1, heartRate: "100", cadence: "", latitude: ""),
            Row(2, heartRate: "", cadence: "", latitude: ""),
            Row(3, heartRate: "110", cadence: "", latitude: ""),
        ]).Should().ContainSingle().Subject;

        state.AverageHeartRateBpm.Should().Be("105");
        state.AverageRunCadence.Should().BeEmpty();
        state.AverageLatitude.Should().BeEmpty();
    }

    [Fact]
    public void AnUnknownRunIsItsOwnStateSet()
    {
        var states = StatesCsvWriter.Rows(
        [
            Row(1),
            Row(2, state: ActivityState.Unknown, cadence: ""),
            Row(3, state: ActivityState.Unknown, cadence: ""),
            Row(4),
        ]);

        states.Select(row => (row.State, row.StateEndTrackpointNum))
            .Should().Equal((ActivityState.Walking, "1"), (ActivityState.Unknown, "3"), (ActivityState.Walking, "4"));
    }

    [Fact]
    public void EmptyInputGivesNoRowsAndOnlyTheHeader()
    {
        var states = StatesCsvWriter.Rows([]);

        states.Should().BeEmpty();
        WriteText(states).Should().Be(ExpectedHeader + "\n");
    }

    [Fact]
    public void WriteCleansEveryField()
    {
        var text = WriteText([StatesCsvWriter.Rows([Row(1)])[0] with { ActivityId = "a|b\r\nc" }]);

        text.Split('\n')[1].Should().StartWith("a b" + "c|0|00:00:00|0|walking|");
    }

    private static ActivityCsvRow Row(
        int trackpointNum,
        string state = ActivityState.Walking,
        double? elapsed = 0,
        string? stateSeconds = null,
        string latitude = "1.5",
        string longitude = "2.5",
        string heartRate = "120",
        string cadence = "140",
        string distanceMiles = "0",
        string altitudeMeters = "10",
        string altitudeFeet = "32.808398950131235")
    {
        var time = elapsed is { } seconds
            ? new DateTime(2026, 11, 1, 6, 0, 0, DateTimeKind.Utc).AddSeconds(seconds).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)
            : string.Empty;
        var elapsedSeconds = elapsed is { } value ? Text(value) : string.Empty;
        var clock = elapsed is { } whole
            ? TimeSpan.FromSeconds(whole).ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
            : string.Empty;
        return new ActivityCsvRow(
            "fixture-run", "2026-11-01", "Forerunner 999", "77", "Running", Text(trackpointNum), time, elapsedSeconds, clock,
            latitude, longitude, heartRate, cadence, state, stateSeconds ?? elapsedSeconds, "0", distanceMiles,
            altitudeMeters, altitudeFeet);
    }

    private static string Text(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static string WriteText(IReadOnlyList<StatesCsvRow> rows)
    {
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        StatesCsvWriter.Write(rows, output);
        return output.ToString();
    }
}
