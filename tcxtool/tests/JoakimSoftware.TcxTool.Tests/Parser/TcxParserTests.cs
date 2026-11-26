using JoakimSoftware.TcxTool.Parser.Conversion;
using JoakimSoftware.TcxTool.Parser.Tcx;
using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Verifies <see cref="TcxParser"/> against the fixtures (contracts/parser-api.md,
/// data-model.md "Parsed TCX model", constitution Principle IV).
/// </summary>
public class TcxParserTests
{
    [Fact]
    public async Task ParsesARunWithLapsInDocumentOrderAsync()
    {
        var activity = (await ParseFixtureAsync("run-two-laps.tcx", CancellationToken.None)).Activities.Should().ContainSingle().Subject;

        activity.Sport.Should().Be("Running");
        activity.Id.Text.Should().Be("2026-11-01T12:04:07.000Z");
        activity.Id.Value.Should().Be(new DateTimeOffset(2026, 11, 1, 12, 4, 7, TimeSpan.Zero));
        activity.Laps.Should().HaveCount(2);
        activity.Laps.Select(lap => lap.Trackpoints.Count).Should().Equal(5, 5);
        activity.Laps.SelectMany(lap => lap.Trackpoints).Select(tp => tp.Time!.Text).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public async Task KeepsNumbersExactlyAsWrittenAsync()
    {
        var first = (await ParseFixtureAsync("run-two-laps.tcx", CancellationToken.None)).Activities[0].Laps[0].Trackpoints[0];

        first.DistanceMeters!.Text.Should().Be("0.0");
        first.DistanceMeters.Value.Should().Be(0);
        first.AltitudeMeters!.Text.Should().Be("103.5999984741211");
        first.AltitudeMeters.Value.Should().Be(103.5999984741211);
        first.Position!.LatitudeDegrees.Text.Should().Be("10.000012345678901");
        first.Position.LongitudeDegrees.Text.Should().Be("-20.000098765432101");
        first.HeartRateBpm!.Text.Should().Be("83");
    }

    [Fact]
    public async Task ReadsTheCreatorThatFollowsTheLapsAsync()
    {
        var creator = (await ParseFixtureAsync("run-two-laps.tcx", CancellationToken.None)).Activities[0].Creator;

        creator.Should().Be(new TcxDevice("Forerunner 999", "1234567890"));
    }

    [Fact]
    public async Task ReadsExtensionValuesFromTheActivityExtensionNamespaceAsync()
    {
        var first = (await ParseFixtureAsync("run-two-laps.tcx", CancellationToken.None)).Activities[0].Laps[0].Trackpoints[0];

        first.RunCadence!.Text.Should().Be("59");
        first.Speed!.Text.Should().Be("1.053999948501587");
        first.Watts!.Text.Should().Be("130");
    }

    [Fact]
    public async Task ReadsLapSummaryValuesAsync()
    {
        var lap = (await ParseFixtureAsync("run-two-laps.tcx", CancellationToken.None)).Activities[0].Laps[0];

        lap.StartTime!.Text.Should().Be("2026-11-01T12:04:07.000Z");
        lap.TotalTimeSeconds!.Text.Should().Be("10.0");
        lap.DistanceMeters!.Text.Should().Be("100.0");
        lap.Calories!.Text.Should().Be("12");
        lap.Intensity.Should().Be("Active");
        lap.TriggerMethod.Should().Be("Manual");
        lap.AverageHeartRateBpm.Should().BeNull();
        lap.MaximumSpeed.Should().BeNull();
    }

    [Fact]
    public async Task IgnoresElementsInUnknownNamespacesAsync()
    {
        var activity = (await ParseFixtureAsync("run-two-laps.tcx", CancellationToken.None)).Activities[0];

        activity.Laps.SelectMany(lap => lap.Trackpoints).Should().HaveCount(10);
        activity.Laps[0].Trackpoints[3].RunCadence!.Text.Should().Be("62");
    }

    [Fact]
    public async Task ParsesEveryActivityInOrderAsync()
    {
        var activities = (await ParseFixtureAsync("multi-activity.tcx", CancellationToken.None)).Activities;

        activities.Select(activity => activity.Id.Text).Should().Equal("2026-11-01T07:15:00.000Z", "2026-11-01T18:30:05.000Z");
        activities.Should().OnlyContain(activity => activity.Laps.Single().Trackpoints.Count == 3);
    }

    [Fact]
    public async Task ParsesARideWithPositionsAsync()
    {
        var activity = (await ParseFixtureAsync("ride-positions.tcx", CancellationToken.None)).Activities.Single();

        activity.Sport.Should().Be("Biking");
        var trackpoints = activity.Laps.Single().Trackpoints;
        trackpoints.Should().HaveCount(4).And.OnlyContain(tp => tp.Position != null && tp.RunCadence == null);
        trackpoints[2].Position.Should().Be(new TcxPosition(new RecordedNumber("-33.8602", -33.8602), new RecordedNumber("151.2102", 151.2102)));
    }

    [Fact]
    public async Task KeepsTheOffsetWrittenInTimesAsync()
    {
        var activity = (await ParseFixtureAsync("fractional-times.tcx", CancellationToken.None)).Activities.Single();

        activity.Id.Value.Offset.Should().Be(TimeSpan.FromHours(-5));
        activity.Laps[0].Trackpoints[1].Time!.Value.Millisecond.Should().Be(600);
    }

    [Fact]
    public async Task MissingElementsGiveNullFieldsAsync()
    {
        var activity = (await ParseFixtureAsync("missing-optional.tcx", CancellationToken.None)).Activities.Single();
        var tps = activity.Laps.Single().Trackpoints;

        activity.Creator.Should().BeNull();
        tps.Should().HaveCount(8);
        tps[0].Time.Should().BeNull();
        tps[1].HeartRateBpm.Should().BeNull();
        tps[2].Position.Should().BeNull();
        tps[4].RunCadence.Should().BeNull();
        tps[4].Speed.Should().BeNull();
        tps[5].AltitudeMeters.Should().BeNull();
        tps[6].DistanceMeters.Should().BeNull();
    }

    [Fact]
    public async Task APositionMissingOneCoordinateIsNullAsync()
    {
        var tps = (await ParseFixtureAsync("missing-optional.tcx", CancellationToken.None)).Activities[0].Laps[0].Trackpoints;

        tps[3].Position.Should().BeNull();
    }

    [Fact]
    public async Task AnEmptyElementCountsAsMissingAsync()
    {
        var tps = (await ParseFixtureAsync("missing-optional.tcx", CancellationToken.None)).Activities[0].Laps[0].Trackpoints;

        tps[7].AltitudeMeters.Should().BeNull();
        tps[7].DistanceMeters!.Text.Should().Be("7.0");
    }

    [Fact]
    public async Task LapsWithoutTrackpointsAreKeptAsync()
    {
        var activity = (await ParseFixtureAsync("no-trackpoints.tcx", CancellationToken.None)).Activities.Single();

        activity.Laps.Should().HaveCount(2).And.OnlyContain(lap => lap.Trackpoints.Count == 0);
    }

    [Theory]
    [InlineData("malformed.tcx", TcxErrorKind.Malformed)]
    [InlineData("not-tcx.tcx", TcxErrorKind.NotTcx)]
    [InlineData("dtd.tcx", TcxErrorKind.ProhibitedContent)]
    [InlineData("invalid-number.tcx", TcxErrorKind.InvalidValue)]
    [InlineData("invalid-number-nan.tcx", TcxErrorKind.InvalidValue)]
    [InlineData("invalid-time.tcx", TcxErrorKind.InvalidValue)]
    [InlineData("missing-id.tcx", TcxErrorKind.InvalidActivityId)]
    public async Task InvalidFilesGiveTypedErrorsWithoutThrowingAsync(string fixture, TcxErrorKind expected)
    {
        var result = await ParseErrorAsync(fixture, CancellationToken.None);

        result.Kind.Should().Be(expected);
        result.Message.Should().Be(TcxErrorMessages.For(expected, element: result.Element, trackpointNumber: result.TrackpointNumber));
    }

    [Fact]
    public async Task TheProhibitedEntityIsNeverReadAsync()
    {
        var error = await ParseErrorAsync("dtd.tcx", CancellationToken.None);

        error.Message.Should().NotContainAny("localhost", "hosts", "&x;");
        error.Element.Should().BeNull();
    }

    [Fact]
    public async Task AnInvalidNestedValueIsNamedByItsParentAsync()
    {
        var error = await ParseErrorAsync("invalid-number.tcx", CancellationToken.None);

        error.Element.Should().Be("HeartRateBpm");
        error.TrackpointNumber.Should().Be(3);
        error.Message.Should().NotContain("abc");
    }

    [Fact]
    public async Task AnInvalidTimeNamesTheElementAndTrackpointAsync()
    {
        var error = await ParseErrorAsync("invalid-time.tcx", CancellationToken.None);

        error.Element.Should().Be("Time");
        error.TrackpointNumber.Should().Be(2);
    }

    [Fact]
    public async Task NotANumberIsInvalidAsync()
    {
        var error = await ParseErrorAsync("invalid-number-nan.tcx", CancellationToken.None);

        error.Element.Should().Be("AltitudeMeters");
        error.TrackpointNumber.Should().Be(2);
    }

    [Fact]
    public async Task ANonSeekableStreamStillGivesMalformedAsync()
    {
        await using var stream = new NonSeekableStream(Fixture.OpenRead("malformed.tcx"));

        var result = await TcxParser.ParseAsync(stream, CancellationToken.None);

        result.Error!.Kind.Should().Be(TcxErrorKind.Malformed);
    }

    [Fact]
    public async Task ANonSeekableStreamStillDetectsTheDoctypeAsync()
    {
        await using var stream = new NonSeekableStream(Fixture.OpenRead("dtd.tcx"));

        var result = await TcxParser.ParseAsync(stream, CancellationToken.None);

        result.Error!.Kind.Should().Be(TcxErrorKind.ProhibitedContent);
    }

    private static async Task<TcxError> ParseErrorAsync(string name, CancellationToken cancellationToken)
    {
        await using var stream = Fixture.OpenRead(name);
        var result = await TcxParser.ParseAsync(stream, cancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Document.Should().BeNull();
        return result.Error!;
    }

    internal static async Task<TcxDocument> ParseFixtureAsync(string name, CancellationToken cancellationToken)
    {
        await using var stream = Fixture.OpenRead(name);
        var result = await TcxParser.ParseAsync(stream, cancellationToken);

        result.Error.Should().BeNull();
        result.IsSuccess.Should().BeTrue();
        return result.Document!;
    }
}
