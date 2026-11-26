using System.Text;

using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Verifies <see cref="ActivityCsvReader"/> against specs/005-table-page/contracts/activity-csv-reader.md and
/// the 19-field format of specs/006-activity-states/contracts/csv-format.md: the golden round trip, the
/// accepted input, and every failure result.
/// </summary>
public class ActivityCsvReaderTests
{
    // Feature 002's header, before State and StateSeconds were added (feature 006 FR-017).
    private const string SeventeenFieldHeader =
        "ActivityId|Date|DeviceName|DeviceId|Sport|TrackpointNum|Time|ElapsedSeconds|ElapsedTime|"
        + "Latitude|Longitude|HeartRateBpm|RunCadence|DistanceMeters|DistanceMiles|AltitudeMeters|AltitudeFeet";

    public static TheoryData<string, int> GoldenFiles => new()
    {
        { "2026-11-01_071500_multi-activity.csv", 3 },
        { "2026-11-01_183005_multi-activity.csv", 3 },
        { "fractional-times.csv", 5 },
        { "missing-optional.csv", 8 },
        { "no-trackpoints.csv", 0 },
        { "ride-positions.csv", 4 },
        { "run-two-laps.csv", 10 },
        { "run-states.csv", 14 },
    };

    public static TheoryData<string> MismatchedHeaders => new()
    {
        string.Empty,
        "hello\n",
        ActivityCsvWriter.Header.ToLowerInvariant() + "\n",
        ActivityCsvWriter.Header[..ActivityCsvWriter.Header.LastIndexOf('|')] + "\n",
        ActivityCsvWriter.Header + "|X\n",
        ActivityCsvWriter.Header + " \n",
    };

    [Theory]
    [MemberData(nameof(GoldenFiles))]
    public async Task EveryGoldenCsvReadsBackExactlyAsync(string golden, int rows)
    {
        using var input = new StreamReader(Fixture.ExpectedPath(golden), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var result = await ActivityCsvReader.ReadAsync(input, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Rows.Should().HaveCount(rows);
        var lines = Fixture.ReadExpected(golden).Split('\n')[1..^1];
        result.Rows.Select(Join).Should().Equal(lines);
    }

    [Fact]
    public async Task EachFieldLandsInItsOwnPropertyAsync()
    {
        var line = string.Join('|', Enumerable.Range(1, ActivityCsvReader.FieldCount).Select(i => $"v{i}"));

        var row = (await ReadAsync(ActivityCsvWriter.Header + "\n" + line + "\n")).Rows.Should().ContainSingle().Subject;

        row.Should().BeEquivalentTo(new
        {
            ActivityId = "v1",
            Date = "v2",
            DeviceName = "v3",
            DeviceId = "v4",
            Sport = "v5",
            TrackpointNum = "v6",
            Time = "v7",
            ElapsedSeconds = "v8",
            ElapsedTime = "v9",
            Latitude = "v10",
            Longitude = "v11",
            HeartRateBpm = "v12",
            RunCadence = "v13",
            State = "v14",
            StateSeconds = "v15",
            DistanceMeters = "v16",
            DistanceMiles = "v17",
            AltitudeMeters = "v18",
            AltitudeFeet = "v19",
        });
    }

    [Fact]
    public async Task AnEmptyFieldReadsAsAnEmptyStringAsync()
    {
        var csv = ActivityCsvWriter.Header + "\n" + ActivityCsvFixture.Line(deviceName: string.Empty, heartRateBpm: string.Empty) + "\n";

        var row = (await ReadAsync(csv)).Rows.Should().ContainSingle().Subject;

        row.DeviceName.Should().NotBeNull().And.BeEmpty();
        row.HeartRateBpm.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task AHeaderOnlyFileIsASuccessWithNoRowsAsync()
    {
        var result = await ReadAsync(ActivityCsvWriter.Header + "\n");

        result.IsSuccess.Should().BeTrue();
        result.Rows.Should().BeEmpty();
    }

    [Theory]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public async Task OtherLineEndingsReadTheSameAsLineFeedsAsync(string ending)
    {
        string[] lines = [ActivityCsvWriter.Header, ActivityCsvFixture.Line(trackpointNum: "1"), ActivityCsvFixture.Line(trackpointNum: "2")];
        var expected = await ReadAsync(string.Join('\n', lines) + "\n");

        var result = await ReadAsync(string.Join(ending, lines) + ending);

        result.IsSuccess.Should().BeTrue();
        result.Rows.Should().Equal(expected.Rows);
    }

    [Fact]
    public async Task AByteOrderMarkIsNotPartOfTheHeaderAsync()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(ActivityCsvWriter.Header + "\n" + ActivityCsvFixture.Line() + "\n")).ToArray();
        using var input = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var result = await ActivityCsvReader.ReadAsync(input, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Rows.Should().ContainSingle();
    }

    [Theory]
    [MemberData(nameof(MismatchedHeaders))]
    public async Task AnyOtherFirstLineIsAHeaderMismatchAtLineOneAsync(string csv)
    {
        var result = await ReadAsync(csv + ActivityCsvFixture.Line() + "\n");

        ShouldFail(result, ActivityCsvErrorKind.HeaderMismatch, 1);
    }

    [Fact]
    public async Task AFeature002SeventeenFieldFileIsAHeaderMismatchAtLineOneAsync()
    {
        var line = string.Join('|', Enumerable.Repeat("x", 17));

        ShouldFail(await ReadAsync(SeventeenFieldHeader + "\n" + line + "\n"), ActivityCsvErrorKind.HeaderMismatch, 1);
    }

    [Fact]
    public async Task EmptyInputIsAHeaderMismatchAtLineOneAsync()
    {
        ShouldFail(await ReadAsync(string.Empty), ActivityCsvErrorKind.HeaderMismatch, 1);
    }

    [Theory]
    [InlineData(18)]
    [InlineData(20)]
    [InlineData(1)]
    public async Task TheFirstLineWithTheWrongFieldCountFailsWithItsLineNumberAndNoRowsAsync(int fields)
    {
        var bad = fields == 1 ? string.Empty : string.Join('|', Enumerable.Repeat("x", fields));
        var csv = string.Join('\n', ActivityCsvWriter.Header, ActivityCsvFixture.Line(), ActivityCsvFixture.Line(), bad, ActivityCsvFixture.Line()) + "\n";

        ShouldFail(await ReadAsync(csv), ActivityCsvErrorKind.FieldCount, 4);
    }

    [Fact]
    public async Task ANullInputThrowsAsync()
    {
        var read = () => ActivityCsvReader.ReadAsync(null!, CancellationToken.None);

        await read.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ACancelledTokenThrowsAsync()
    {
        using var input = new StringReader(ActivityCsvWriter.Header + "\n" + ActivityCsvFixture.Line() + "\n");

        var read = () => ActivityCsvReader.ReadAsync(input, new CancellationToken(canceled: true));

        await read.Should().ThrowAsync<OperationCanceledException>();
    }

    private static async Task<ActivityCsvReadResult> ReadAsync(string csv)
    {
        using var input = new StringReader(csv);
        return await ActivityCsvReader.ReadAsync(input, CancellationToken.None);
    }

    private static void ShouldFail(ActivityCsvReadResult result, ActivityCsvErrorKind kind, int lineNumber)
    {
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(new ActivityCsvError(kind, lineNumber));
        result.Rows.Should().BeEmpty();
    }

    private static string Join(ActivityCsvRow row) => string.Join('|',
        row.ActivityId, row.Date, row.DeviceName, row.DeviceId, row.Sport, row.TrackpointNum, row.Time,
        row.ElapsedSeconds, row.ElapsedTime, row.Latitude, row.Longitude, row.HeartRateBpm, row.RunCadence,
        row.State, row.StateSeconds, row.DistanceMeters, row.DistanceMiles, row.AltitudeMeters, row.AltitudeFeet);
}
