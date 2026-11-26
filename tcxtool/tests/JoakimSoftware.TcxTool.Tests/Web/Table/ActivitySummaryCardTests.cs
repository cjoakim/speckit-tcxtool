using Bunit;

using JoakimSoftware.TcxTool.Web.Analysis;
using JoakimSoftware.TcxTool.Web.Components.Table;

namespace JoakimSoftware.TcxTool.Tests.Web.Table;

/// <summary>
/// Verifies <see cref="ActivitySummaryCard"/> against specs/005-table-page/contracts/pages.md "Header box"
/// (spec FR-008 to FR-010).
/// </summary>
public class ActivitySummaryCardTests : BunitContext
{
    private static readonly string[] Labels = ["CSV file", "ActivityId", "Date", "DeviceName", "DeviceId", "Sport"];

    [Fact]
    public void TheCardHasTheHomePageCardStructureAndTitle()
    {
        var card = RenderCard(new("a.csv", "run", "2026-11-01", "Forerunner 999", "77", "Running"));

        card.Find("div.card.mb-3 > div.card-body#activity-summary").Should().NotBeNull();
        card.Find("#activity-summary > h2.card-title.h5").TextContent.Trim().Should().Be("Activity");
    }

    [Fact]
    public void TheSixValuesFollowTheirLabelsInOrder()
    {
        var card = RenderCard(new("2026-11-01_run.csv", "run", "2026-11-01", "Forerunner 999", "77", "Running"));

        card.FindAll("dl.row.mb-0 > dt.col-sm-3").Select(dt => dt.TextContent.Trim()).Should().Equal(Labels);
        card.FindAll("dl.row.mb-0 > dd.col-sm-9").Select(dd => dd.TextContent.Trim())
            .Should().Equal("2026-11-01_run.csv", "run", "2026-11-01", "Forerunner 999", "77", "Running");
        card.Find("dl").Children.Select(child => child.TagName).Should().Equal("DT", "DD", "DT", "DD", "DT", "DD", "DT", "DD", "DT", "DD", "DT", "DD");
    }

    [Fact]
    public void AnEmptyValueIsAMutedDashAndItsLabelStays()
    {
        var card = RenderCard(new("a.csv", "run", "2026-11-01", "", "", "Running"));

        card.FindAll("dt").Select(dt => dt.TextContent.Trim()).Should().Equal(Labels);
        var values = card.FindAll("dd");
        foreach (var dd in new[] { values[3], values[4] })
        {
            var dash = dd.QuerySelector("span.text-muted");
            dash.Should().NotBeNull();
            dash!.TextContent.Should().Be("—");
            dd.TextContent.Trim().Should().Be("—");
        }

        values[1].QuerySelector("span.text-muted").Should().BeNull();
    }

    private IRenderedComponent<ActivitySummaryCard> RenderCard(ActivitySummary summary) =>
        Render<ActivitySummaryCard>(parameters => parameters.Add(card => card.Summary, summary));
}
