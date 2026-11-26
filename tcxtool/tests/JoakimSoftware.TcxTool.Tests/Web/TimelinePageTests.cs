using System.Text.Json;

using AngleSharp.Dom;

using Bunit;

using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Tests.Support;
using JoakimSoftware.TcxTool.Web.Analysis;
using JoakimSoftware.TcxTool.Web.Components.Pages;
using JoakimSoftware.TcxTool.Web.Conversion;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies the Timeline page against specs/007-timeline-page/contracts/pages.md "Timeline page" and
/// contracts/timeline-data.md. Every path is inside a test-owned temp directory (FR-024), and every
/// value is made up.
/// </summary>
public class TimelinePageTests : BunitContext
{
    private const string Name = "2026-11-01_fixture.csv";
    private const string StatesName = "2026-11-01_fixture.states.csv";

    private readonly TempDirectory _root = new();
    private readonly TcxToolOptions _options;

    public TimelinePageTests()
    {
        _options = BunitTcxToolServices.Add(Services, _root);
    }

    public enum Setup
    {
        NoState,
        NoStatesCsv,
        MissingFile,
        LockedFile,
        WrongHeader,
        HeaderOnly,
        ThreeLines,
    }

    public static TheoryData<Setup> EverySetup => [.. Enum.GetValues<Setup>()];

    public static TheoryData<Setup, CurrentStatesStatus> NoticeSetups => new()
    {
        { Setup.NoState, CurrentStatesStatus.NoCurrentCsv },
        { Setup.NoStatesCsv, CurrentStatesStatus.NoStatesCsv },
        { Setup.MissingFile, CurrentStatesStatus.Missing },
        { Setup.LockedFile, CurrentStatesStatus.Unreadable },
        { Setup.WrongHeader, CurrentStatesStatus.WrongFormat },
        { Setup.HeaderOnly, CurrentStatesStatus.NoStateSets },
    };

    private string StatesPath => Path.Combine(_options.CsvOutputDirectory, StatesName);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _root.Dispose();
    }

    [Theory]
    [MemberData(nameof(EverySetup))]
    public void EveryStateHasOneTimelineHeading(Setup setup)
    {
        using var lockOnFile = Arrange(setup);
        var page = RenderAndWait();

        page.FindAll("h1").Should().ContainSingle().Which.TextContent.Should().Be("Timeline");
    }

    [Theory]
    [MemberData(nameof(NoticeSetups))]
    public void EachNoticeIsTheStatesPagesNoticeWithTheTimelineId(Setup setup, CurrentStatesStatus status)
    {
        using var lockOnFile = Arrange(setup);
        var page = RenderAndWait();

        var notice = page.Find("div.alert#timeline-status > p.mb-0");
        notice.TextContent.Should().StartWith(CurrentStatesMessages.For(status));
        page.FindAll(".card").Should().BeEmpty();
        page.FindAll("tcx-timeline").Should().BeEmpty();
        page.FindAll("#timeline-legend").Should().BeEmpty();
    }

    [Fact]
    public void ALoadedFileShowsTheHeaderCardWithItsFiveValues()
    {
        using var lockOnFile = Arrange(Setup.ThreeLines);
        var page = RenderAndWait();

        var card = page.Find("div.card.mb-3 > div.card-body#timeline-summary");
        card.QuerySelector("h2.card-title.h5")!.TextContent.Should().Be("Activity");
        card.QuerySelectorAll("dt.col-sm-3").Select(dt => dt.TextContent)
            .Should().Equal("CSV file", "ActivityId", "Date", "ActivityDistanceMiles", "ActivityElapsedTime");
        card.QuerySelectorAll("dd.col-sm-9").Select(dd => dd.TextContent.Trim())
            .Should().Equal(Name, "fixture-run", "2026-11-01", "0.15", "00:00:29");
        page.FindAll("#timeline-status").Should().BeEmpty();
    }

    [Fact]
    public void AnEmptyHeaderValueIsAMutedDash()
    {
        ActivityCsvFixture.WriteState(_options.StateFilePath, Name, StatesName);
        StatesCsvFixture.Write(StatesPath, [StatesCsvFixture.Line(activityDistanceMiles: string.Empty)]);
        var page = RenderAndWait();

        var value = page.FindAll("#timeline-summary dd")[3];
        value.QuerySelector("span.text-muted")!.TextContent.Should().Be("—");
    }

    [Fact]
    public void TheLegendNamesTheFourStatesWithTheirColorClasses()
    {
        using var lockOnFile = Arrange(Setup.ThreeLines);
        var page = RenderAndWait();

        var items = page.FindAll("ul#timeline-legend > li.list-inline-item");
        items.Select(li => li.TextContent.Trim()).Should().Equal("stopped", "walking", "running", "unknown");
        items.Select(li => li.QuerySelector("span.timeline-swatch")!.ClassList.Last())
            .Should().Equal("state-stopped", "state-walking", "state-running", "state-unknown");
    }

    [Fact]
    public void TheTimelineElementCarriesTheLayoutAsCamelCaseJson()
    {
        using var lockOnFile = Arrange(Setup.ThreeLines);
        var page = RenderAndWait();

        var element = page.Find("section#timeline > tcx-timeline");
        using var json = JsonDocument.Parse(element.GetAttribute("data-timeline")!);
        var root = json.RootElement;
        root.GetProperty("totalSeconds").GetDouble().Should().Be(27);
        root.GetProperty("fieldNames").EnumerateArray().Select(name => name.GetString())
            .Should().Equal(StatesCsvWriter.Header.Split('|'));
        var segments = root.GetProperty("segments").EnumerateArray().ToList();
        segments.Should().HaveCount(3);
        segments.Select(segment => segment.GetProperty("state").GetString()).Should().Equal("walking", "running", "walking");
        segments[^1].GetProperty("end").GetDouble().Should().Be(1.0);
        segments.Should().OnlyContain(segment => segment.GetProperty("values").GetArrayLength() == 16);
        segments.Should().AllSatisfy(segment =>
            segment.EnumerateObject().Select(property => property.Name).Should().Equal("state", "start", "end", "center", "values"));
    }

    [Fact]
    public void TheTimelineElementHoldsTheBarThenEachChartsLabelAndContainer()
    {
        ActivityCsvFixture.WriteState(_options.StateFilePath, Name, StatesName);
        StatesCsvFixture.Write(StatesPath,
        [
            StatesCsvFixture.Line(averageRunCadence: "140", averageHeartRateBpm: "", averageAltitudeMeters: ""),
            StatesCsvFixture.Line(averageRunCadence: "", averageHeartRateBpm: "121", averageAltitudeMeters: ""),
        ]);
        var page = RenderAndWait();

        var children = page.Find("tcx-timeline").Children;
        children.Select(Describe).Should().Equal(
            "div.timeline-bar",
            "h2.h6.mt-3.mb-1 Run Cadence", "div.timeline-chart[0]",
            "h2.h6.mt-3.mb-1 Heart Rate", "div.timeline-chart[1]",
            "h2.h6.mt-3.mb-1 Altitude", "p.text-muted.small.timeline-no-data No data.");
    }

    [Fact]
    public void TheJsonHasThreeChartsWithCamelCaseProperties()
    {
        using var lockOnFile = Arrange(Setup.ThreeLines);
        var page = RenderAndWait();

        using var json = JsonDocument.Parse(page.Find("tcx-timeline").GetAttribute("data-timeline")!);
        var charts = json.RootElement.GetProperty("charts").EnumerateArray().ToList();
        charts.Select(chart => chart.GetProperty("label").GetString()).Should().Equal("Run Cadence", "Heart Rate", "Altitude");
        charts.Should().AllSatisfy(chart =>
            chart.EnumerateObject().Select(property => property.Name).Should().Equal("label", "field", "min", "max", "points"));
        charts[0].GetProperty("points")[0].EnumerateObject().Select(property => property.Name).Should().Equal("center", "value");
    }

    [Fact]
    public void TheNoScriptNoteFollowsTheTimeline()
    {
        using var lockOnFile = Arrange(Setup.ThreeLines);
        var page = RenderAndWait();

        page.Markup.Should().Contain(
            "<noscript><div class=\"alert alert-secondary mt-2\" id=\"timeline-noscript\">The timeline and charts need JavaScript.</div></noscript>");
    }

    private static string Describe(IElement element)
    {
        var name = element.LocalName + "." + string.Join('.', element.ClassList);
        return element.GetAttribute("data-chart") is { } index
            ? $"{name}[{index}]"
            : element.TextContent.Length > 0 ? $"{name} {element.TextContent}" : name;
    }

    private IRenderedComponent<TimelinePage> RenderAndWait()
    {
        var page = Render<TimelinePage>();
        page.WaitForAssertion(() => page.FindAll("#timeline-status, #timeline-summary").Should().NotBeEmpty());
        return page;
    }

    private FileStream? Arrange(Setup setup)
    {
        if (setup == Setup.NoState)
        {
            return null;
        }

        ActivityCsvFixture.Write(Path.Combine(_options.CsvOutputDirectory, Name), [ActivityCsvFixture.Line()]);
        ActivityCsvFixture.WriteState(_options.StateFilePath, Name, setup == Setup.NoStatesCsv ? null : StatesName);
        switch (setup)
        {
            case Setup.LockedFile:
                StatesCsvFixture.Write(StatesPath, [StatesCsvFixture.Line()]);
                return new FileStream(StatesPath, FileMode.Open, FileAccess.Read, FileShare.None);
            case Setup.WrongHeader:
                ActivityCsvFixture.WriteRaw(StatesPath, "hello\n");
                break;
            case Setup.HeaderOnly:
                StatesCsvFixture.Write(StatesPath, []);
                break;
            case Setup.ThreeLines:
                StatesCsvFixture.Write(StatesPath,
                [
                    StatesCsvFixture.Line(stateElapsedSeconds: "9"),
                    StatesCsvFixture.Line(state: "running", stateElapsedSeconds: "10"),
                    StatesCsvFixture.Line(stateElapsedSeconds: "8", activityDistanceMiles: "0.15", activityElapsedTime: "00:00:29"),
                ]);
                break;
            case Setup.NoState:
            case Setup.NoStatesCsv:
            case Setup.MissingFile:
            default:
                break;
        }

        return null;
    }
}
