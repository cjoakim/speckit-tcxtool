using System.Text;

using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Verifies <see cref="StatesCsvReader"/> against specs/006-activity-states/data-model.md "Readers" and
/// specs/007-timeline-page/contracts/states-csv-format.md (16 fields): the golden round trip, the
/// accepted input, and every failure result.
/// </summary>
public class StatesCsvReaderTests
{
    public static TheoryData<string, int> GoldenFiles => new()
    {
        { "2026-11-01_071500_multi-activity.states.csv", 1 },
        { "2026-11-01_183005_multi-activity.states.csv", 1 },
        { "fractional-times.states.csv", 1 },
        { "missing-optional.states.csv", 3 },
        { "no-trackpoints.states.csv", 0 },
        { "ride-positions.states.csv", 1 },
        { "run-two-laps.states.csv", 1 },
        { "run-states.states.csv", 7 },
    };

    public static TheoryData<string> MismatchedHeaders => new()
    {
        string.Empty,
        "hello\n",
        ActivityCsvWriter.Header + "\n",
        StatesCsvWriter.Header.ToLowerInvariant() + "\n",
        StatesCsvWriter.Header[..StatesCsvWriter.Header.LastIndexOf('|')] + "\n",
        StatesCsvWriter.Header + "|X\n",
        // Feature 006's 13-field header (feature 007 FR-006, research.md R3).
        "ActivityId|State|StateEndDate|StateEndTrackpointNum|StateElapsedSeconds|ActivityElapsedTime|"
            + "AverageLatitude|AverageLongitude|AverageHeartRateBpm|AverageRunCadence|DistanceMiles|"
            + "AverageAltitudeMeters|AverageAltitudeFeet\n",
    };

    [Theory]
    [MemberData(nameof(GoldenFiles))]
    public async Task EveryGoldenStatesCsvReadsBackExactlyAsync(string golden, int rows)
    {
        using var input = new StreamReader(Fixture.ExpectedPath(golden), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var result = await StatesCsvReader.ReadAsync(input, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Rows.Should().HaveCount(rows);
        var lines = Fixture.ReadExpected(golden).Split('\n')[1..^1];
        result.Rows.Select(Join).Should().Equal(lines);
    }

    [Fact]
    public async Task EachFieldLandsInItsOwnPropertyAsync()
    {
        var line = string.Join('|', Enumerable.Range(1, StatesCsvWriter.FieldCount).Select(i => $"v{i}"));

        var row = (await ReadAsync(StatesCsvWriter.Header + "\n" + line + "\n")).Rows.Should().ContainSingle().Subject;

        row.Should().Be(new StatesCsvRow("v1", "v2", "v3", "v4", "v5", "v6", "v7", "v8", "v9", "v10", "v11", "v12", "v13", "v14", "v15", "v16"));
    }

    [Fact]
    public async Task AHeaderOnlyFileIsASuccessWithNoRowsAsync()
    {
        var result = await ReadAsync(StatesCsvWriter.Header + "\n");

        result.IsSuccess.Should().BeTrue();
        result.Rows.Should().BeEmpty();
    }

    [Theory]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public async Task OtherLineEndingsReadTheSameAsLineFeedsAsync(string ending)
    {
        string[] lines = [StatesCsvWriter.Header, StatesCsvFixture.Line(stateEndTrackpointNum: "1"), StatesCsvFixture.Line(stateEndTrackpointNum: "2")];
        var expected = await ReadAsync(string.Join('\n', lines) + "\n");

        var result = await ReadAsync(string.Join(ending, lines) + ending);

        result.IsSuccess.Should().BeTrue();
        result.Rows.Should().Equal(expected.Rows);
    }

    [Fact]
    public async Task AByteOrderMarkIsNotPartOfTheHeaderAsync()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(StatesCsvWriter.Header + "\n" + StatesCsvFixture.Line() + "\n")).ToArray();
        using var input = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var result = await StatesCsvReader.ReadAsync(input, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Rows.Should().ContainSingle();
    }

    [Theory]
    [MemberData(nameof(MismatchedHeaders))]
    public async Task AnyOtherFirstLineIsAHeaderMismatchAtLineOneAsync(string csv)
    {
        var result = await ReadAsync(csv + StatesCsvFixture.Line() + "\n");

        ShouldFail(result, ActivityCsvErrorKind.HeaderMismatch, 1);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(17)]
    [InlineData(13)]
    [InlineData(1)]
    public async Task TheFirstLineWithTheWrongFieldCountFailsWithItsLineNumberAndNoRowsAsync(int fields)
    {
        var bad = fields == 1 ? string.Empty : string.Join('|', Enumerable.Repeat("x", fields));
        var csv = string.Join('\n', StatesCsvWriter.Header, StatesCsvFixture.Line(), StatesCsvFixture.Line(), bad, StatesCsvFixture.Line()) + "\n";

        ShouldFail(await ReadAsync(csv), ActivityCsvErrorKind.FieldCount, 4);
    }

    [Fact]
    public async Task ANullInputThrowsAsync()
    {
        var read = () => StatesCsvReader.ReadAsync(null!, CancellationToken.None);

        await read.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ACancelledTokenThrowsAsync()
    {
        using var input = new StringReader(StatesCsvWriter.Header + "\n" + StatesCsvFixture.Line() + "\n");

        var read = () => StatesCsvReader.ReadAsync(input, new CancellationToken(canceled: true));

        await read.Should().ThrowAsync<OperationCanceledException>();
    }

    private static async Task<StatesCsvReadResult> ReadAsync(string csv)
    {
        using var input = new StringReader(csv);
        return await StatesCsvReader.ReadAsync(input, CancellationToken.None);
    }

    private static void ShouldFail(StatesCsvReadResult result, ActivityCsvErrorKind kind, int lineNumber)
    {
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(new ActivityCsvError(kind, lineNumber));
        result.Rows.Should().BeEmpty();
    }

    private static string Join(StatesCsvRow row) => string.Join('|',
        row.ActivityId, row.ActivityDistanceMiles, row.ActivityElapsedTime, row.ActivityElapsedSeconds, row.State,
        row.StateEndDate, row.StateEndTrackpointNum, row.StateTrackpointCount, row.StateElapsedSeconds,
        row.StateDistanceMiles, row.AverageHeartRateBpm, row.AverageRunCadence, row.AverageLatitude,
        row.AverageLongitude, row.AverageAltitudeMeters, row.AverageAltitudeFeet);
}
