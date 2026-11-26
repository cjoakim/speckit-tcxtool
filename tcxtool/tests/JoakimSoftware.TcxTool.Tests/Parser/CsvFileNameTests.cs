using JoakimSoftware.TcxTool.Parser.Conversion;
using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Verifies <see cref="CsvFileName.TryFor"/> (contracts/parser-api.md, data-model.md "Output file names").
/// </summary>
public class CsvFileNameTests
{
    [Fact]
    public async Task OneActivityIsNamedByDateAndActivityIdAsync()
    {
        var document = await TcxParserTests.ParseFixtureAsync("run-two-laps.tcx", CancellationToken.None);

        CsvFileName.TryFor(document.Activities, "run-two-laps", out var names, out var error).Should().BeTrue();

        names.Should().Equal("2026-11-01_run-two-laps.csv");
        error.Should().BeNull();
    }

    [Fact]
    public async Task SeveralActivitiesAddTheirStartTimesAsync()
    {
        var document = await TcxParserTests.ParseFixtureAsync("multi-activity.tcx", CancellationToken.None);

        CsvFileName.TryFor(document.Activities, "multi-activity", out var names, out _).Should().BeTrue();

        names.Should().Equal("2026-11-01_071500_multi-activity.csv", "2026-11-01_183005_multi-activity.csv");
    }

    [Fact]
    public async Task UsesTheDateWrittenInTheIdNotUtcAsync()
    {
        // 2026-11-01T21:30:00-05:00 is 2026-11-02 in UTC.
        var document = await TcxParserTests.ParseFixtureAsync("fractional-times.tcx", CancellationToken.None);

        CsvFileName.TryFor(document.Activities, "fractional-times", out var names, out _).Should().BeTrue();

        names.Should().Equal("2026-11-01_fractional-times.csv");
    }

    [Fact]
    public async Task ActivitiesStartingInTheSameSecondCannotBeNamedAsync()
    {
        var document = await TcxParserTests.ParseFixtureAsync("duplicate-start.tcx", CancellationToken.None);

        CsvFileName.TryFor(document.Activities, "duplicate-start", out var names, out var error).Should().BeFalse();

        names.Should().BeEmpty();
        error!.Kind.Should().Be(TcxErrorKind.DuplicateActivityStart);
    }
}
