using JoakimSoftware.TcxTool.Web.Analysis;

namespace JoakimSoftware.TcxTool.Tests.Web.Analysis;

/// <summary>
/// Verifies <see cref="CurrentActivityMessages"/> against specs/005-table-page/contracts/pages.md "Messages".
/// </summary>
public class CurrentActivityMessagesTests
{
    public static TheoryData<CurrentActivityStatus, string> Messages => new()
    {
        { CurrentActivityStatus.NoCurrentCsv, "No CSV is selected yet." },
        { CurrentActivityStatus.Missing, "The current CSV is no longer in the CSV folder:" },
        { CurrentActivityStatus.Unreadable, "The current CSV could not be read:" },
        { CurrentActivityStatus.WrongFormat, "The current CSV is not in the format this app writes:" },
        { CurrentActivityStatus.NoTrackpoints, "This activity has no trackpoints." },
        { CurrentActivityStatus.Loaded, "" },
    };

    [Theory]
    [MemberData(nameof(Messages))]
    public void EachStatusHasItsContractMessage(CurrentActivityStatus status, string message)
    {
        CurrentActivityMessages.For(status).Should().Be(message);
    }

    [Fact]
    public void AnUnknownStatusThrows()
    {
        var message = () => CurrentActivityMessages.For((CurrentActivityStatus)99);

        message.Should().Throw<ArgumentOutOfRangeException>();
    }
}
