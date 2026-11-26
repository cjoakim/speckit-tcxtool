using AngleSharp.Dom;

using Bunit;

using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Web.Components.States;
using JoakimSoftware.TcxTool.Web.Table;

namespace JoakimSoftware.TcxTool.Tests.Web.States;

/// <summary>
/// Verifies <see cref="StatesTable"/> against specs/006-activity-states/contracts/pages.md "Table"
/// (feature 006 FR-020, FR-021), with the 16 columns of specs/007-timeline-page/contracts/pages.md
/// "States page" (feature 007 FR-005).
/// </summary>
public class StatesTableTests : BunitContext
{
    private const int TrackpointNumColumn = 6;
    private const int HeartRateColumn = 10;
    private const int RunCadenceColumn = 11;

    private static readonly string[] Headings =
    [
        "ActivityId", "ActivityDistanceMiles", "ActivityElapsedTime", "ActivityElapsedSeconds", "State", "StateEndDate",
        "StateEndTrackpointNum", "StateTrackpointCount", "StateElapsedSeconds", "StateDistanceMiles",
        "AverageHeartRateBpm", "AverageRunCadence", "AverageLatitude", "AverageLongitude", "AverageAltitudeMeters",
        "AverageAltitudeFeet",
    ];

    [Fact]
    public void TheTableSitsInTheScrollBox()
    {
        RenderTable(Row()).Find("div.table-responsive.states-table-scroll > table#states").Should().NotBeNull();
    }

    [Fact]
    public void TheTableHasTheContractClassesAndNoStripingOrHover()
    {
        var table = RenderTable(Row()).Find("table#states");

        table.ClassList.Should().Contain(["table", "table-sm", "table-bordered", "small", "mb-0"]);
        table.ClassList.Should().NotContain("table-striped").And.NotContain("table-hover");
    }

    [Fact]
    public void TheHeadHasTheSixteenHeadingsInStatesCsvOrder()
    {
        var table = RenderTable(Row());

        table.FindAll("thead.table-light > tr").Should().ContainSingle();
        var headings = table.FindAll("thead.table-light > tr > th");
        headings.Should().HaveCount(16).And.OnlyContain(th => th.GetAttribute("scope") == "col");
        headings.Select(th => th.TextContent).Should().Equal(Headings);
        string.Join('|', Headings).Should().Be(StatesCsvWriter.Header);
    }

    [Fact]
    public void EachRowIsOneBodyRowInInputOrder()
    {
        var table = RenderTable(Row(trackpointNum: "4"), Row(trackpointNum: "9"), Row(trackpointNum: "12"));

        var rows = table.FindAll("tbody > tr");
        rows.Should().HaveCount(3).And.OnlyContain(tr => tr.Children.Length == 16);
        rows.Select(tr => tr.Children[TrackpointNumColumn].TextContent).Should().Equal("4", "9", "12");
    }

    [Fact]
    public void EachCellHoldsItsFieldExactlyAndEmptyCellsAreKept()
    {
        var row = new StatesCsvRow(
            "f1-activity", "0.0937500000000001", "00:02:30", "150", "running", "", "151", "113", "113",
            "0.0700000000000001", "152.5", "171.33333333333334", "12.345678901234567", "-23.456789012345678", "",
            "339.8950081171952");

        var cells = RenderTable(row).FindAll("tbody > tr > td").Select(td => td.TextContent).ToList();

        cells.Should().Equal(
            "f1-activity", "0.0937500000000001", "00:02:30", "150", "running", "", "151", "113", "113",
            "0.0700000000000001", "152.5", "171.33333333333334", "12.345678901234567", "-23.456789012345678", "",
            "339.8950081171952");
    }

    [Fact]
    public void TheAverageHeartRateAndCadenceCellsCarryTheirBandClass()
    {
        (string HeartRate, string Cadence)[] values = [("89", "19"), ("125", "120"), ("165.4", "172"), ("181", "185")];
        var table = RenderTable([.. values.Select((value, i) => Row(trackpointNum: $"{i + 1}", heartRate: value.HeartRate, cadence: value.Cadence))]);

        var rows = table.FindAll("tbody > tr");
        rows.Select(tr => tr.Children[HeartRateColumn].GetAttribute("class"))
            .Should().Equal(values.Select(value => TrackpointBands.HeartRateClass(value.HeartRate)));
        rows.Select(tr => tr.Children[RunCadenceColumn].GetAttribute("class"))
            .Should().Equal(values.Select(value => TrackpointBands.RunCadenceClass(value.Cadence)));
        rows.Select(tr => tr.Children[HeartRateColumn].GetAttribute("class"))
            .Should().Equal("hr-band-1", "hr-band-3", "hr-band-5", "hr-band-6");
        rows.Select(tr => tr.Children[RunCadenceColumn].GetAttribute("class"))
            .Should().Equal("cadence-band-1", "cadence-band-2", "cadence-band-3", "cadence-band-4");
    }

    [Fact]
    public void EveryOtherCellHasNoAttributes()
    {
        var table = RenderTable(Row(), Row(trackpointNum: "2"));

        foreach (var tr in table.FindAll("tbody > tr"))
        {
            tr.Children.Where((_, column) => column is not HeartRateColumn and not RunCadenceColumn)
                .Should().OnlyContain(td => td.Attributes.Length == 0);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("n/a")]
    public void AnAverageWithNoBandHasNoClassAttribute(string value)
    {
        var tr = RenderTable(Row(heartRate: value, cadence: value)).Find("tbody > tr");

        tr.Children[HeartRateColumn].HasAttribute("class").Should().BeFalse();
        tr.Children[RunCadenceColumn].HasAttribute("class").Should().BeFalse();
    }

    private static StatesCsvRow Row(string trackpointNum = "10", string heartRate = "120", string cadence = "140") =>
        new("fixture-run", "0.05", "00:00:09", "9", "walking", "2026-11-01T06:00:09.000Z", trackpointNum, "10", "9",
            "0.01", heartRate, cadence, "1.5", "2.5", "10", "32.808398950131235");

    private IRenderedComponent<StatesTable> RenderTable(params StatesCsvRow[] rows) =>
        Render<StatesTable>(parameters => parameters.Add(table => table.Rows, rows));
}
