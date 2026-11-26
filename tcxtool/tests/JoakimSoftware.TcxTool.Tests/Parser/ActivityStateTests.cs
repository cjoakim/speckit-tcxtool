using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Verifies <see cref="ActivityState"/> against specs/006-activity-states/data-model.md
/// <c>ActivityState</c> (spec FR-003, research R3). The input is already doubled, in steps per minute.
/// </summary>
public class ActivityStateTests
{
    [Fact]
    public void TheNamesAndTheRunningEdgeAreTheContractValues()
    {
        ActivityState.Stopped.Should().Be("stopped");
        ActivityState.Walking.Should().Be("walking");
        ActivityState.Running.Should().Be("running");
        ActivityState.Unknown.Should().Be("unknown");
        ActivityState.RunningStepsPerMinute.Should().Be(150);
    }

    [Theory]
    [InlineData(0.0, "stopped")]
    [InlineData(2.0, "walking")]
    [InlineData(148.0, "walking")]
    [InlineData(149.999, "walking")]
    [InlineData(150.0, "running")]
    [InlineData(198.0, "running")]
    [InlineData(-2.0, "unknown")]
    public void EachValueFallsInExactlyOneState(double stepsPerMinute, string expected)
    {
        ActivityState.For(stepsPerMinute).Should().Be(expected);
    }

    [Fact]
    public void AMissingValueIsUnknown()
    {
        ActivityState.For(null).Should().Be(ActivityState.Unknown);
    }
}
