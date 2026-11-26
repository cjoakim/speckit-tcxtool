using AngleSharp.Dom;

using Bunit;

using JoakimSoftware.TcxTool.Tests.Support;
using JoakimSoftware.TcxTool.Web.Analysis;
using JoakimSoftware.TcxTool.Web.Components.Pages;
using JoakimSoftware.TcxTool.Web.Conversion;

using Microsoft.AspNetCore.Components;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies the States page against specs/006-activity-states/contracts/pages.md "States page". Every
/// path is inside a test-owned temp directory (FR-026).
/// </summary>
public class StatesPageTests : BunitContext
{
    private const string Name = "2026-11-01_fixture.csv";
    private const string StatesName = "2026-11-01_fixture.states.csv";

    private readonly TempDirectory _root = new();
    private readonly TcxToolOptions _options;

    public StatesPageTests()
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

    private string StatesPath => Path.Combine(_options.CsvOutputDirectory, StatesName);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _root.Dispose();
    }

    [Fact]
    public void ThePageRoutesToStatesOnly()
    {
        typeof(StatesPage).GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>().Select(route => route.Template)
            .Should().Equal("/states");
    }

    [Theory]
    [MemberData(nameof(EverySetup))]
    public void EveryStateHasOneStatesHeading(Setup setup)
    {
        using var lockOnFile = Arrange(setup);

        var page = RenderAndWait();

        var heading = page.FindAll("h1").Should().ContainSingle().Subject;
        heading.ClassList.Should().Contain(["h3", "mb-3"]);
        heading.TextContent.Trim().Should().Be("States");
    }

    [Fact]
    public void NoCurrentCsvShowsTheInfoNoticeWithBothLinks()
    {
        using var lockOnFile = Arrange(Setup.NoState);
        var page = RenderAndWait();

        var status = page.Find("div.alert.alert-info#states-status[role=status] > p.mb-0");
        status.TextContent.Should().StartWith(CurrentStatesMessages.For(CurrentStatesStatus.NoCurrentCsv));
        status.QuerySelector("a[href='']")!.TextContent.Should().Be("Select a CSV file");
        status.QuerySelector("a[href='convert']")!.TextContent.Should().Be("convert a TCX file");
        ShouldShowNoFileLineAndNoTable(page);
    }

    [Fact]
    public void NoStatesCsvNamesTheCsvAndLinksToConvert()
    {
        using var lockOnFile = Arrange(Setup.NoStatesCsv);
        var page = RenderAndWait();

        ShouldShowWarning(page, CurrentStatesStatus.NoStatesCsv, Name, convertLink: true);
    }

    [Fact]
    public void AMissingStatesCsvNamesItAndLinksToConvert()
    {
        using var lockOnFile = Arrange(Setup.MissingFile);
        var page = RenderAndWait();

        ShouldShowWarning(page, CurrentStatesStatus.Missing, StatesName, convertLink: true);
    }

    [Fact]
    public void AnUnreadableStatesCsvNamesItWithNoLink()
    {
        using var lockOnFile = Arrange(Setup.LockedFile);
        var page = RenderAndWait();

        ShouldShowWarning(page, CurrentStatesStatus.Unreadable, StatesName, convertLink: false);
    }

    [Fact]
    public void AWrongFormatNamesItAndLinksToConvert()
    {
        using var lockOnFile = Arrange(Setup.WrongHeader);
        var page = RenderAndWait();

        ShouldShowWarning(page, CurrentStatesStatus.WrongFormat, StatesName, convertLink: true);
    }

    [Fact]
    public void AHeaderOnlyFileShowsTheFileLineThenTheNoStateSetsNotice()
    {
        using var lockOnFile = Arrange(Setup.HeaderOnly);
        var page = RenderAndWait();

        ShouldShowTheFileLine(page);
        var status = page.Find("div.alert.alert-info#states-status[role=status] > p.mb-0");
        status.TextContent.Trim().Should().Be(CurrentStatesMessages.For(CurrentStatesStatus.NoStateSets));
        page.Markup.IndexOf("id=\"states-file\"", StringComparison.Ordinal)
            .Should().BeLessThan(page.Markup.IndexOf("id=\"states-status\"", StringComparison.Ordinal));
        page.FindAll("table").Should().BeEmpty();
    }

    [Fact]
    public void ALoadedFileShowsTheFileLineThenTheTable()
    {
        using var lockOnFile = Arrange(Setup.ThreeLines);
        var page = RenderAndWait();

        ShouldShowTheFileLine(page);
        page.FindAll("#states-status").Should().BeEmpty();
        page.Markup.IndexOf("id=\"states-file\"", StringComparison.Ordinal)
            .Should().BeLessThan(page.Markup.IndexOf("states-table-scroll", StringComparison.Ordinal));
        page.FindAll("table#states > tbody > tr").Select(tr => tr.Children[6].TextContent).Should().Equal("3", "7", "9");
    }

    private static void ShouldShowTheFileLine(IRenderedComponent<StatesPage> page)
    {
        var file = page.Find("p.mb-2#states-file");
        file.TextContent.Should().Be("States CSV: " + StatesName);
        file.QuerySelector("strong")!.TextContent.Should().Be(StatesName);
    }

    private static void ShouldShowNoFileLineAndNoTable(IRenderedComponent<StatesPage> page)
    {
        page.FindAll("#states-file").Should().BeEmpty();
        page.FindAll("table").Should().BeEmpty();
    }

    private static void ShouldShowWarning(IRenderedComponent<StatesPage> page, CurrentStatesStatus status, string name, bool convertLink)
    {
        var text = page.Find("div.alert.alert-warning#states-status[role=alert] > p.mb-0");
        var expected = $"{CurrentStatesMessages.For(status)} <strong>{name}</strong>."
            + (convertLink ? " <a href=\"convert\">Convert its TCX file again</a>." : string.Empty);
        text.InnerHtml.Should().Be(expected);
        ShouldShowNoFileLineAndNoTable(page);
    }

    private IRenderedComponent<StatesPage> RenderAndWait()
    {
        var page = Render<StatesPage>();
        page.WaitForAssertion(() => page.FindAll("#states-status, #states-file").Should().NotBeEmpty());
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
                    StatesCsvFixture.Line(stateEndTrackpointNum: "3"),
                    StatesCsvFixture.Line(stateEndTrackpointNum: "7"),
                    StatesCsvFixture.Line(stateEndTrackpointNum: "9"),
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
