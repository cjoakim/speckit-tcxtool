using JoakimSoftware.TcxTool.Web.Analysis;

namespace JoakimSoftware.TcxTool.Tests.Web.Analysis;

/// <summary>
/// Verifies <see cref="CurrentStatesMessages"/> against specs/006-activity-states/data-model.md
/// <c>CurrentStatesMessages</c>. The texts are written here, because <c>specs/</c> is not in the
/// Compose build context.
/// </summary>
public class CurrentStatesMessagesTests
{
    public static TheoryData<CurrentStatesStatus, string> Texts => new()
    {
        { CurrentStatesStatus.NoCurrentCsv, "No CSV is selected yet." },
        { CurrentStatesStatus.NoStatesCsv, "The current CSV has no States CSV:" },
        { CurrentStatesStatus.Missing, "The States CSV is no longer in the CSV folder:" },
        { CurrentStatesStatus.Unreadable, "The States CSV could not be read:" },
        { CurrentStatesStatus.WrongFormat, "The States CSV is not in the format this app writes:" },
        { CurrentStatesStatus.NoStateSets, "This activity has no state changes." },
        { CurrentStatesStatus.Loaded, "" },
    };

    [Theory]
    [MemberData(nameof(Texts))]
    public void EachStatusHasItsText(CurrentStatesStatus status, string text)
    {
        CurrentStatesMessages.For(status).Should().Be(text);
    }

    [Fact]
    public void EveryStatusIsCovered()
    {
        Texts.Select(row => (CurrentStatesStatus)row[0]).Should().BeEquivalentTo(Enum.GetValues<CurrentStatesStatus>());
    }

    [Fact]
    public void AnOutOfRangeStatusThrows()
    {
        var act = () => CurrentStatesMessages.For((CurrentStatesStatus)99);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
