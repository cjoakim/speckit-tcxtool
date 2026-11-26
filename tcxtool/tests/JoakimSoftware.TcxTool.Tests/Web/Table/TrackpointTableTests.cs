using AngleSharp.Dom;

using Bunit;

using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Web.Components.Table;
using JoakimSoftware.TcxTool.Web.Table;

namespace JoakimSoftware.TcxTool.Tests.Web.Table;

/// <summary>
/// Verifies <see cref="TrackpointTable"/> against specs/005-table-page/contracts/pages.md "Table"
/// (spec FR-012, FR-013, FR-016, FR-019).
/// </summary>
public class TrackpointTableTests : BunitContext
{
    private const int HeartRateColumn = 4;
    private const int RunCadenceColumn = 5;

    private static readonly string[] Headings =
    [
        "TrackpointNum", "Time", "ElapsedSeconds", "ElapsedTime", "HeartRateBpm",
        "RunCadence", "DistanceMiles", "Latitude", "Longitude", "AltitudeFeet",
    ];

    [Fact]
    public void TheTableSitsInTheScrollBox()
    {
        var table = RenderTable(Row());

        table.Find("div.table-responsive.trackpoint-table-scroll > table#trackpoints").Should().NotBeNull();
    }

    [Fact]
    public void TheTableHasTheContractClassesAndNoStripingOrHover()
    {
        var table = RenderTable(Row()).Find("table#trackpoints");

        table.ClassList.Should().Contain(["table", "table-sm", "table-bordered", "small", "mb-0"]);
        table.ClassList.Should().NotContain("table-striped").And.NotContain("table-hover");
    }

    [Fact]
    public void TheHeadHasTheTenColumnHeadingsInOrder()
    {
        var table = RenderTable(Row());

        table.FindAll("thead.table-light > tr").Should().ContainSingle();
        var headings = table.FindAll("thead.table-light > tr > th");
        headings.Should().HaveCount(10).And.OnlyContain(th => th.GetAttribute("scope") == "col");
        headings.Select(th => th.TextContent).Should().Equal(Headings);
    }

    [Fact]
    public void EachRowIsOneBodyRowInInputOrder()
    {
        var table = RenderTable(Row(trackpointNum: "1"), Row(trackpointNum: "2"), Row(trackpointNum: "3"));

        var rows = table.FindAll("tbody > tr");
        rows.Should().HaveCount(3).And.OnlyContain(tr => tr.Children.Length == 10);
        rows.Select(tr => tr.Children[0].TextContent).Should().Equal("1", "2", "3");
    }

    [Fact]
    public void EachCellHoldsItsColumnsFieldExactly()
    {
        var row = new ActivityCsvRow(
            "f1-activity", "f2-date", "f3-device", "f4-unit", "f5-sport", "6", "2026-11-01T06:00:07.000Z", "7.5",
            "00:00:07", "12.345678901234567", "-23.456789012345678", "131", "168", "f-state", "f-seconds",
            "f16-meters", "0.0009444842003490327", "f18-altitude", "339.8950081171952");

        var cells = RenderTable(row).FindAll("tbody > tr > td").Select(td => td.TextContent).ToList();

        cells.Should().Equal(
            "6", "2026-11-01T06:00:07.000Z", "7.5", "00:00:07", "131", "168", "0.0009444842003490327",
            "12.345678901234567", "-23.456789012345678", "339.8950081171952");
        cells.Should().NotContain(cell => cell.StartsWith('f'));
    }

    [Fact]
    public void EmptyValuesAreEmptyCellsAndTheRowStays()
    {
        var table = RenderTable(Row(time: "", latitude: "", longitude: "", distanceMiles: "", altitudeFeet: ""));

        var cells = table.FindAll("tbody > tr > td");
        cells.Should().HaveCount(10);
        new[] { cells[1], cells[6], cells[7], cells[8], cells[9] }.Should().OnlyContain(td => td.TextContent.Length == 0);
    }

    [Fact]
    public void TheUnbandedCellsHaveNoAttributes()
    {
        var table = RenderTable(Row(), Row(trackpointNum: "2"));

        foreach (var tr in table.FindAll("tbody > tr"))
        {
            UnbandedCells(tr).Should().OnlyContain(td => td.Attributes.Length == 0);
        }
    }

    [Fact]
    public void TheHeartRateAndCadenceCellsCarryTheirBandClass()
    {
        (string HeartRate, string Cadence)[] values = [("89", "19"), ("90", "20"), ("140", "150"), ("180", "180")];
        var table = RenderTable([.. values.Select((value, i) => Row(trackpointNum: $"{i + 1}", heartRateBpm: value.HeartRate, runCadence: value.Cadence))]);

        var rows = table.FindAll("tbody > tr");
        rows.Select(tr => tr.Children[HeartRateColumn].GetAttribute("class"))
            .Should().Equal(values.Select(value => TrackpointBands.HeartRateClass(value.HeartRate)));
        rows.Select(tr => tr.Children[RunCadenceColumn].GetAttribute("class"))
            .Should().Equal(values.Select(value => TrackpointBands.RunCadenceClass(value.Cadence)));
        rows.Select(tr => tr.Children[HeartRateColumn].GetAttribute("class"))
            .Should().Equal("hr-band-1", "hr-band-2", "hr-band-4", "hr-band-6");
        rows.Select(tr => tr.Children[RunCadenceColumn].GetAttribute("class"))
            .Should().Equal("cadence-band-1", "cadence-band-2", "cadence-band-3", "cadence-band-4");
        foreach (var tr in rows)
        {
            UnbandedCells(tr).Should().OnlyContain(td => td.Attributes.Length == 0);
        }
    }

    [Fact]
    public void EmptyOrNonNumericValuesLeaveTheCellWithoutAClassAttribute()
    {
        var tr = RenderTable(Row(heartRateBpm: "", runCadence: "abc")).Find("tbody > tr");

        tr.Children[HeartRateColumn].HasAttribute("class").Should().BeFalse();
        tr.Children[RunCadenceColumn].HasAttribute("class").Should().BeFalse();
        tr.Children[RunCadenceColumn].TextContent.Should().Be("abc");
    }

    private static IEnumerable<IElement> UnbandedCells(IElement tr) =>
        tr.Children.Where((_, column) => column is not HeartRateColumn and not RunCadenceColumn);

    private static ActivityCsvRow Row(
        string trackpointNum = "1",
        string time = "2026-11-01T06:00:00.000Z",
        string latitude = "1.5",
        string longitude = "2.5",
        string heartRateBpm = "120",
        string runCadence = "80",
        string distanceMiles = "0",
        string altitudeFeet = "32.808398950131235") =>
        new("fixture-run", "2026-11-01", "Forerunner 999", "77", "Running", trackpointNum, time, "0", "00:00:00",
            latitude, longitude, heartRateBpm, runCadence, "walking", "0", "0.0", distanceMiles, "10.0", altitudeFeet);

    private IRenderedComponent<TrackpointTable> RenderTable(params ActivityCsvRow[] rows) =>
        Render<TrackpointTable>(parameters => parameters.Add(table => table.Rows, rows));
}
