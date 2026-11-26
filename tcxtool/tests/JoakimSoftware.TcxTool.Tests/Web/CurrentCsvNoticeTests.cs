using Bunit;

using JoakimSoftware.TcxTool.Web.Components.State;
using JoakimSoftware.TcxTool.Web.State;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies <see cref="CurrentCsvNotice"/> and <see cref="CsvSelectionMessages"/>
/// (specs/004-about-and-csv-state/contracts/pages.md "CurrentCsvNotice" and "Messages").
/// </summary>
public class CurrentCsvNoticeTests : BunitContext
{
    [Fact]
    public void NoResultRendersNothing()
    {
        var notice = Render<CurrentCsvNotice>(parameters => parameters.Add(component => component.Result, null));

        notice.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void ARecordedChoiceNamesTheCurrentCsv()
    {
        var notice = Render<CurrentCsvNotice>(parameters => parameters.Add(
            component => component.Result, new CsvSelectionResult(CsvSelectionOutcome.Recorded, "2026-11-01_a.csv")));

        var status = notice.Find("p.mt-3.mb-0[role=status]");
        status.TextContent.Trim().Should().Be("Current CSV: 2026-11-01_a.csv");
        status.QuerySelector("strong")!.TextContent.Should().Be("2026-11-01_a.csv");
        notice.FindAll("[role=alert]").Should().BeEmpty();
    }

    [Theory]
    [InlineData(CsvSelectionOutcome.NoneChosen, "Choose a CSV file to select.")]
    [InlineData(CsvSelectionOutcome.InvalidName, "That is not a CSV file this app can use.")]
    [InlineData(CsvSelectionOutcome.NotFound, "That CSV file is no longer in the CSV folder.")]
    [InlineData(CsvSelectionOutcome.WriteFailed, "The current CSV could not be recorded.")]
    public void AnUnrecordedChoiceShowsAWarning(CsvSelectionOutcome outcome, string message)
    {
        var notice = Render<CurrentCsvNotice>(parameters => parameters.Add(
            component => component.Result, new CsvSelectionResult(outcome, "x.csv")));

        notice.Find("div.alert.alert-warning.mt-3[role=alert] > p.mb-0").TextContent.Trim().Should().Be(message);
        notice.FindAll("[role=status]").Should().BeEmpty();
    }

    [Theory]
    [InlineData(CsvSelectionOutcome.NoneChosen, "Choose a CSV file to select.")]
    [InlineData(CsvSelectionOutcome.InvalidName, "That is not a CSV file this app can use.")]
    [InlineData(CsvSelectionOutcome.NotFound, "That CSV file is no longer in the CSV folder.")]
    [InlineData(CsvSelectionOutcome.WriteFailed, "The current CSV could not be recorded.")]
    public void MessagesMatchTheContract(CsvSelectionOutcome outcome, string message)
    {
        CsvSelectionMessages.For(outcome).Should().Be(message);
    }
}
