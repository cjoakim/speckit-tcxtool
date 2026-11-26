using Bunit;

using JoakimSoftware.TcxTool.Tests.Support;
using JoakimSoftware.TcxTool.Web.Analysis;
using JoakimSoftware.TcxTool.Web.Components.Pages;
using JoakimSoftware.TcxTool.Web.Conversion;

using Microsoft.AspNetCore.Components;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies the Table page against specs/005-table-page/contracts/pages.md "Page" and "Content by
/// status". Every path is inside a test-owned temp directory (FR-022).
/// </summary>
public class TablePageTests : BunitContext
{
    private const string Name = "2026-11-01_fixture.csv";

    private readonly TempDirectory _root = new();
    private readonly TcxToolOptions _options;

    public TablePageTests()
    {
        _options = BunitTcxToolServices.Add(Services, _root);
    }

    public enum Setup
    {
        NoState,
        MissingFile,
        LockedFile,
        WrongHeader,
        HeaderOnly,
        ThreeLines,
    }

    public static TheoryData<Setup> EverySetup => [.. Enum.GetValues<Setup>()];

    private string CsvPath => Path.Combine(_options.CsvOutputDirectory, Name);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _root.Dispose();
    }

    // ---- Carried over from PlaceholderPageTests (they pass from the start) ----

    [Fact]
    public void ThePageRoutesToTableOnly()
    {
        typeof(TablePage).GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>().Select(route => route.Template)
            .Should().Equal("/table");
    }

    [Theory]
    [MemberData(nameof(EverySetup))]
    public void EveryStateHasOneTableHeading(Setup setup)
    {
        Arrange(setup);
        using var lockOnFile = setup == Setup.LockedFile ? LockCsv() : null;

        var page = Render<TablePage>();

        page.WaitForAssertion(() => page.FindAll("#table-status, #activity-summary").Should().NotBeEmpty());
        var heading = page.FindAll("h1").Should().ContainSingle().Subject;
        heading.ClassList.Should().Contain(["h3", "mb-3"]);
        heading.TextContent.Trim().Should().Be("Table");
    }

    // ---- Content by status ----

    [Fact]
    public void NoCurrentCsvShowsTheInfoNoticeWithBothLinks()
    {
        var page = RenderAndWait(Setup.NoState);

        var status = page.Find("div.alert.alert-info#table-status[role=status] > p.mb-0");
        status.TextContent.Should().Contain(CurrentActivityMessages.For(CurrentActivityStatus.NoCurrentCsv));
        status.QuerySelector("a[href='']")!.TextContent.Should().Be("Select a CSV file");
        status.QuerySelector("a[href='convert']")!.TextContent.Should().Be("convert a TCX file");
        ShouldShowNoSummaryAndNoTable(page);
    }

    [Fact]
    public void AMissingFileShowsTheWarningWithTheNameAndAHomeLink()
    {
        var page = RenderAndWait(Setup.MissingFile);

        var status = page.Find("div.alert.alert-warning#table-status[role=alert] > p.mb-0");
        status.TextContent.Should().Contain(CurrentActivityMessages.For(CurrentActivityStatus.Missing));
        status.QuerySelector("strong")!.TextContent.Should().Be(Name);
        status.QuerySelector("a[href='']")!.TextContent.Should().Be("Select another CSV file");
        ShouldShowNoSummaryAndNoTable(page);
    }

    [Fact]
    public void AnUnreadableFileShowsTheWarningWithTheName()
    {
        Arrange(Setup.LockedFile);
        using var lockOnFile = LockCsv();

        var page = Render<TablePage>();
        page.WaitForElement("#table-status");

        ShouldShowWarning(page, CurrentActivityStatus.Unreadable);
        page.Find("#table-status").QuerySelectorAll("a").Should().BeEmpty();
    }

    [Fact]
    public void AWrongFormatShowsTheWarningWithTheName()
    {
        var page = RenderAndWait(Setup.WrongHeader);

        ShouldShowWarning(page, CurrentActivityStatus.WrongFormat);
        var link = page.Find("#table-status > p.mb-0").QuerySelector("a[href='convert']")!;
        link.TextContent.Should().Be("Convert its TCX file again");
        page.Find("#table-status > p.mb-0").InnerHtml.Should()
            .EndWith("<strong>" + Name + "</strong>. <a href=\"convert\">Convert its TCX file again</a>.");
    }

    [Fact]
    public void AHeaderOnlyFileShowsTheCardThenTheNoTrackpointsNotice()
    {
        var page = RenderAndWait(Setup.HeaderOnly);

        page.Find("#activity-summary").TextContent.Should().Contain(Name);
        var status = page.Find("div.alert.alert-info#table-status[role=status] > p.mb-0");
        status.TextContent.Trim().Should().Be(CurrentActivityMessages.For(CurrentActivityStatus.NoTrackpoints));
        page.Markup.IndexOf("id=\"activity-summary\"", StringComparison.Ordinal)
            .Should().BeLessThan(page.Markup.IndexOf("id=\"table-status\"", StringComparison.Ordinal));
        page.FindAll("table").Should().BeEmpty();
    }

    [Fact]
    public void ALoadedFileShowsTheCardWithTheFirstRowsValues()
    {
        var page = RenderAndWait(Setup.ThreeLines);

        var values = page.FindAll("#activity-summary dd").Select(dd => dd.TextContent.Trim());
        values.Should().Equal(Name, "run-a", "2026-10-31", "Edge 1040", "555", "Biking");
        page.FindAll("#table-status").Should().BeEmpty();
    }

    [Fact]
    public void ALoadedFileShowsTheTableAfterTheCardWithOneRowPerLine()
    {
        var page = RenderAndWait(Setup.ThreeLines);

        page.Markup.IndexOf("id=\"activity-summary\"", StringComparison.Ordinal)
            .Should().BeLessThan(page.Markup.IndexOf("trackpoint-table-scroll", StringComparison.Ordinal));
        page.Find("div.trackpoint-table-scroll > table#trackpoints").Should().NotBeNull();
        page.FindAll("table#trackpoints > tbody > tr").Select(tr => tr.Children[0].TextContent).Should().Equal("1", "2", "3");
    }

    private static void ShouldShowNoSummaryAndNoTable(IRenderedComponent<TablePage> page)
    {
        page.FindAll("#activity-summary").Should().BeEmpty();
        page.FindAll("table").Should().BeEmpty();
    }

    private static void ShouldShowWarning(IRenderedComponent<TablePage> page, CurrentActivityStatus status)
    {
        var text = page.Find("div.alert.alert-warning#table-status[role=alert] > p.mb-0");
        text.TextContent.Should().Contain(CurrentActivityMessages.For(status));
        text.QuerySelector("strong")!.TextContent.Should().Be(Name);
        ShouldShowNoSummaryAndNoTable(page);
    }

    private IRenderedComponent<TablePage> RenderAndWait(Setup setup)
    {
        Arrange(setup);
        var page = Render<TablePage>();
        page.WaitForAssertion(() => page.FindAll("#table-status, #activity-summary").Should().NotBeEmpty());
        return page;
    }

    private void Arrange(Setup setup)
    {
        if (setup == Setup.NoState)
        {
            return;
        }

        ActivityCsvFixture.WriteState(_options.StateFilePath, Name);
        switch (setup)
        {
            case Setup.LockedFile:
                ActivityCsvFixture.Write(CsvPath, [ActivityCsvFixture.Line()]);
                break;
            case Setup.WrongHeader:
                ActivityCsvFixture.WriteRaw(CsvPath, "hello\n");
                break;
            case Setup.HeaderOnly:
                ActivityCsvFixture.Write(CsvPath, []);
                break;
            case Setup.ThreeLines:
                ActivityCsvFixture.Write(CsvPath,
                [
                    ActivityCsvFixture.Line(activityId: "run-a", date: "2026-10-31", deviceName: "Edge 1040", deviceId: "555", sport: "Biking", trackpointNum: "1"),
                    ActivityCsvFixture.Line(trackpointNum: "2"),
                    ActivityCsvFixture.Line(trackpointNum: "3"),
                ]);
                break;
            case Setup.MissingFile:
            case Setup.NoState:
            default:
                break;
        }
    }

    private FileStream LockCsv() => new(CsvPath, FileMode.Open, FileAccess.Read, FileShare.None);
}
