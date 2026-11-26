using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Web.Timeline;

namespace JoakimSoftware.TcxTool.Tests.Web.Timeline;

/// <summary>
/// Verifies <see cref="TimelineLayout"/> against specs/007-timeline-page/data-model.md "TimelineLayout"
/// and "TimelineSegment", and contracts/timeline-data.md (spec FR-012, FR-013, FR-017, FR-019 to FR-021, research.md R5, R6). Every
/// value is made up.
/// </summary>
public class TimelineLayoutTests
{
    private static readonly StatesCsvRow BaseRow = new(
        "fixture-run", "0.05", "00:00:09", "9", "walking", "2026-11-01T06:00:09.000Z", "10", "10", "9", "0.01",
        "120", "140", "1.5", "2.5", "10", "32.808398950131235");

    [Fact]
    public void TheFieldNamesAreTheSixteenHeaderNamesInOrder()
    {
        TimelineLayout.From([BaseRow]).FieldNames.Should().Equal(StatesCsvWriter.Header.Split('|'));
    }

    [Fact]
    public void WidthsAreProportionalToStateElapsedSecondsAndTheLastSegmentEndsAtOne()
    {
        var layout = TimelineLayout.From([Seconds("30"), Seconds("10"), Seconds("60")]);

        layout.TotalSeconds.Should().Be(100);
        layout.Segments.Select(segment => segment.Start).Should().Equal(0, 0.3, 0.4);
        layout.Segments.Select(segment => segment.End).Should().Equal(0.3, 0.4, 1.0);
        layout.Segments.Should().OnlyContain(segment => segment.Center == (segment.Start + segment.End) / 2);
    }

    [Fact]
    public void SegmentsAreContiguousWithNoGapsOrOverlaps()
    {
        var layout = TimelineLayout.From([.. Enumerable.Range(1, 541).Select(i => Seconds(((i % 13) + 0.5).ToString(System.Globalization.CultureInfo.InvariantCulture)))]);

        layout.Segments[0].Start.Should().Be(0);
        layout.Segments[^1].End.Should().Be(1.0);
        for (var i = 0; i + 1 < layout.Segments.Count; i++)
        {
            layout.Segments[i].End.Should().Be(layout.Segments[i + 1].Start);
            layout.Segments[i].Start.Should().BeLessThanOrEqualTo(layout.Segments[i].End);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    public void AValueThatIsNotAFiniteNonNegativeNumberCountsAsZero(string value)
    {
        var layout = TimelineLayout.From([Seconds("25"), Seconds(value), Seconds("75")]);

        layout.TotalSeconds.Should().Be(100);
        layout.Segments[1].Start.Should().Be(0.25);
        layout.Segments[1].End.Should().Be(0.25);
        layout.Segments[1].Center.Should().Be(0.25);
    }

    [Fact]
    public void WhenEverySetIsZeroSecondsTheSegmentsShareTheBarEqually()
    {
        var layout = TimelineLayout.From([Seconds("0"), Seconds(""), Seconds("0"), Seconds("0")]);

        layout.TotalSeconds.Should().Be(0);
        layout.Segments.Select(segment => segment.Start).Should().Equal(0, 0.25, 0.5, 0.75);
        layout.Segments.Select(segment => segment.End).Should().Equal(0.25, 0.5, 0.75, 1.0);
    }

    [Fact]
    public void OneSetIsOneSegmentAcrossTheWholeBar()
    {
        var segment = TimelineLayout.From([Seconds("0")]).Segments.Should().ContainSingle().Subject;

        (segment.Start, segment.End, segment.Center).Should().Be((0.0, 1.0, 0.5));
    }

    [Fact]
    public void EachSegmentKeepsItsStateAndItsSixteenValuesAsWritten()
    {
        var row = new StatesCsvRow(
            "f1", "0.0937500000000001", "00:02:30", "150", "running", "", "151", "113", "113", "0.07", "152.5",
            "171.33333333333334", "12.5", "-23.25", "", "339.8950081171952");

        var segment = TimelineLayout.From([row]).Segments.Should().ContainSingle().Subject;

        segment.State.Should().Be("running");
        segment.Values.Should().Equal(
            "f1", "0.0937500000000001", "00:02:30", "150", "running", "", "151", "113", "113", "0.07", "152.5",
            "171.33333333333334", "12.5", "-23.25", "", "339.8950081171952");
    }

    [Fact]
    public void NoRowsThrows()
    {
        var build = () => TimelineLayout.From([]);

        build.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ThereAreThreeChartsInOrder()
    {
        var charts = TimelineLayout.From([BaseRow]).Charts;

        charts.Select(chart => chart.Label).Should().Equal("Run Cadence", "Heart Rate", "Altitude");
        charts.Select(chart => chart.Field).Should().Equal("AverageRunCadence", "AverageHeartRateBpm", "AverageAltitudeMeters");
    }

    [Fact]
    public void EachChartHasOnePointPerSetWithAFiniteValueAtItsSegmentsCenter()
    {
        var layout = TimelineLayout.From(
        [
            BaseRow with { StateElapsedSeconds = "10", AverageRunCadence = "150.5", AverageHeartRateBpm = "120" },
            BaseRow with { StateElapsedSeconds = "30", AverageRunCadence = "", AverageHeartRateBpm = "abc" },
            BaseRow with { StateElapsedSeconds = "20", AverageRunCadence = "-3", AverageHeartRateBpm = "NaN" },
            BaseRow with { StateElapsedSeconds = "40", AverageRunCadence = "171.33333333333334", AverageHeartRateBpm = "Infinity" },
        ]);

        var cadence = layout.Charts[0];
        cadence.Points.Should().Equal(
            new TimelinePoint(layout.Segments[0].Center, 150.5),
            new TimelinePoint(layout.Segments[2].Center, -3),
            new TimelinePoint(layout.Segments[3].Center, 171.33333333333334));
        layout.Charts[1].Points.Should().Equal(new TimelinePoint(layout.Segments[0].Center, 120));
    }

    [Fact]
    public void TheDomainIsTheSmallestAndLargestValue()
    {
        var chart = TimelineLayout.From(
            [BaseRow with { AverageAltitudeMeters = "12.5" }, BaseRow with { AverageAltitudeMeters = "-4.25" }, BaseRow with { AverageAltitudeMeters = "30" }])
            .Charts[2];

        (chart.Min, chart.Max).Should().Be((-4.25, 30.0));
    }

    [Fact]
    public void AllEqualValuesGiveOneEitherSide()
    {
        var chart = TimelineLayout.From([BaseRow with { AverageHeartRateBpm = "41.25" }, BaseRow with { AverageHeartRateBpm = "41.25" }])
            .Charts[1];

        (chart.Min, chart.Max).Should().Be((40.25, 42.25));
    }

    [Fact]
    public void NoValuesGiveNoPointsAndNoDomain()
    {
        var chart = TimelineLayout.From([BaseRow with { AverageAltitudeMeters = "" }, BaseRow with { AverageAltitudeMeters = "x" }])
            .Charts[2];

        chart.Points.Should().BeEmpty();
        (chart.Min, chart.Max).Should().Be(((double?)null, (double?)null));
    }

    [Fact]
    public void ASyntheticMarathonLayoutIsBuiltInUnderOneHundredMilliseconds()
    {
        var rows = StatesCsvReaderRows(541);
        TimelineLayout.From(rows);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var layout = TimelineLayout.From(rows);
        stopwatch.Stop();

        layout.Segments.Should().HaveCount(541);
        layout.Charts.Should().OnlyContain(chart => chart.Points.Count == 541);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(100));
    }

    private static StatesCsvRow[] StatesCsvReaderRows(int count)
    {
        var rows = new StatesCsvRow[count];
        for (var i = 0; i < count; i++)
        {
            var text = (i % 97 + 0.5).ToString(System.Globalization.CultureInfo.InvariantCulture);
            rows[i] = BaseRow with
            {
                StateElapsedSeconds = text,
                AverageRunCadence = text,
                AverageHeartRateBpm = text,
                AverageAltitudeMeters = text,
            };
        }

        return rows;
    }

    private static StatesCsvRow Seconds(string value) => BaseRow with { StateElapsedSeconds = value };
}
