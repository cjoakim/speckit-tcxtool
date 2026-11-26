using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Web.Timeline;

namespace JoakimSoftware.TcxTool.Tests.Web.Timeline;

/// <summary>
/// Verifies <see cref="TimelineHeader"/> against specs/007-timeline-page/data-model.md "TimelineHeader"
/// (spec FR-010, research.md R13). Every value is made up.
/// </summary>
public class TimelineHeaderTests
{
    private static readonly StatesCsvRow BaseRow = new(
        "fixture-run", "0.05", "00:00:09", "9", "walking", "2026-11-01T06:00:09.000Z", "10", "10", "9", "0.01",
        "120", "140", "1.5", "2.5", "10", "32.808398950131235");

    [Fact]
    public void TheValuesComeFromTheFileNameTheFirstRowAndTheLastRow()
    {
        var header = TimelineHeader.From("2026-11-01_fixture.csv",
        [
            BaseRow,
            BaseRow with { ActivityId = "other-id", ActivityDistanceMiles = "0.15", ActivityElapsedTime = "00:00:29" },
        ]);

        header.Should().Be(new TimelineHeader("2026-11-01_fixture.csv", "fixture-run", "2026-11-01", "0.15", "00:00:29"));
    }

    [Theory]
    [InlineData("2026-11-01_x.csv", "2026-11-01")]
    [InlineData("2026-11-01_120407_x.csv", "2026-11-01")]
    [InlineData("notes.csv", "")]
    [InlineData("2026-13-01_x.csv", "")]
    [InlineData("2026-11-0.csv", "")]
    [InlineData("x.csv", "")]
    public void TheDateIsTheFileNamesLeadingDateOrEmpty(string fileName, string expected)
    {
        TimelineHeader.From(fileName, [BaseRow]).Date.Should().Be(expected);
    }

    [Fact]
    public void ALastRowWithNoValueTakesTheLastRowThatHasOne()
    {
        var header = TimelineHeader.From("2026-11-01_fixture.csv",
        [
            BaseRow with { ActivityDistanceMiles = "0.10", ActivityElapsedTime = "00:00:19" },
            BaseRow with { ActivityDistanceMiles = string.Empty, ActivityElapsedTime = string.Empty },
        ]);

        (header.ActivityDistanceMiles, header.ActivityElapsedTime).Should().Be(("0.10", "00:00:19"));
    }

    [Fact]
    public void NoRowWithAValueGivesEmptyValues()
    {
        var header = TimelineHeader.From("2026-11-01_fixture.csv",
            [BaseRow with { ActivityDistanceMiles = string.Empty, ActivityElapsedTime = string.Empty }]);

        (header.ActivityDistanceMiles, header.ActivityElapsedTime).Should().Be((string.Empty, string.Empty));
    }

    [Fact]
    public void NoRowsThrows()
    {
        var build = () => TimelineHeader.From("2026-11-01_fixture.csv", []);

        build.Should().Throw<ArgumentException>();
    }
}
