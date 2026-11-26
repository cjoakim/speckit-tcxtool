using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Verifies the ElapsedSeconds and ElapsedTime fields (spec FR-010, research R6).
/// </summary>
public class ElapsedTimeTests
{
    [Theory]
    [InlineData(0L, "0", "00:00:00")]
    [InlineData(3725L * TimeSpan.TicksPerSecond, "3725", "01:02:05")]
    [InlineData(37_256_000_000L, "3725.6", "01:02:05")]
    [InlineData(360000L * TimeSpan.TicksPerSecond, "360000", "100:00:00")]
    [InlineData(-35_000_000L, "-3.5", "-00:00:03")]
    public void FormatsSecondsAndClockTime(long ticks, string expectedSeconds, string expectedTime)
    {
        var elapsed = TimeSpan.FromTicks(ticks);

        ElapsedTime.FormatSeconds(elapsed).Should().Be(expectedSeconds);
        ElapsedTime.FormatClock(elapsed).Should().Be(expectedTime);
    }
}
