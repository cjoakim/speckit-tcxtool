using Bunit;

using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Web.Analysis;
using JoakimSoftware.TcxTool.Web.Components.States;

namespace JoakimSoftware.TcxTool.Tests.Web.States;

/// <summary>
/// Verifies <see cref="StatesNotice"/> against specs/007-timeline-page/contracts/pages.md "StatesNotice"
/// (research.md R14): the feature 006 notices, with the id the page passes in.
/// </summary>
public class StatesNoticeTests : BunitContext
{
    private const string Name = "2026-11-01_fixture.csv";
    private const string StatesName = "2026-11-01_fixture.states.csv";
    private const string ConvertLink = " <a href=\"convert\">Convert its TCX file again</a>.";

    public static TheoryData<string> Ids => ["states-status", "timeline-status"];

    [Theory]
    [MemberData(nameof(Ids))]
    public void NoCurrentCsvIsTheInfoNoticeWithBothLinks(string id)
    {
        var notice = Render(CurrentStates.NoCurrentCsv, id).Find($"div.alert.alert-info#{id}[role=status] > p.mb-0");

        notice.InnerHtml.Should().Be(CurrentStatesMessages.For(CurrentStatesStatus.NoCurrentCsv)
            + " <a href=\"\">Select a CSV file</a> or <a href=\"convert\">convert a TCX file</a>.");
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void NoStatesCsvNamesTheCsvAndLinksToConvert(string id)
    {
        ShouldWarn(CurrentStates.NoStatesCsv(Name), id, CurrentStatesStatus.NoStatesCsv, Name, ConvertLink);
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void MissingAndWrongFormatNameTheStatesCsvAndLinkToConvert(string id)
    {
        ShouldWarn(CurrentStates.Missing(Name, StatesName), id, CurrentStatesStatus.Missing, StatesName, ConvertLink);
        ShouldWarn(CurrentStates.WrongFormat(Name, StatesName), id, CurrentStatesStatus.WrongFormat, StatesName, ConvertLink);
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void UnreadableNamesTheStatesCsvWithNoLink(string id)
    {
        ShouldWarn(CurrentStates.Unreadable(Name, StatesName), id, CurrentStatesStatus.Unreadable, StatesName, string.Empty);
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void NoStateSetsIsTheInfoNoticeWithNoName(string id)
    {
        var notice = Render(CurrentStates.Read(Name, StatesName, []), id).Find($"div.alert.alert-info#{id}[role=status] > p.mb-0");

        notice.InnerHtml.Should().Be(CurrentStatesMessages.For(CurrentStatesStatus.NoStateSets));
    }

    [Fact]
    public void LoadedRendersNothing()
    {
        StatesCsvRow row = new(
            "fixture-run", "0.05", "00:00:09", "9", "walking", "2026-11-01T06:00:09.000Z", "10", "10", "9", "0.01",
            "120", "140", "1.5", "2.5", "10", "32.8");

        Render(CurrentStates.Read(Name, StatesName, [row]), "states-status").Markup.Trim().Should().BeEmpty();
    }

    private void ShouldWarn(CurrentStates current, string id, CurrentStatesStatus status, string name, string link)
    {
        var notice = Render(current, id).Find($"div.alert.alert-warning#{id}[role=alert] > p.mb-0");

        notice.InnerHtml.Should().Be($"{CurrentStatesMessages.For(status)} <strong>{name}</strong>.{link}");
    }

    private IRenderedComponent<StatesNotice> Render(CurrentStates current, string id) =>
        Render<StatesNotice>(parameters => parameters.Add(notice => notice.Current, current).Add(notice => notice.StatusId, id));
}
