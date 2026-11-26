using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Verifies <see cref="StatesCsvFileName"/> (specs/006-activity-states/data-model.md
/// <c>StatesCsvFileName.For</c> and <c>StatesCsvFileName.IsStatesCsvName</c>; research R4).
/// </summary>
public class StatesCsvFileNameTests
{
    [Fact]
    public void TheSuffixIsTheContractValue()
    {
        StatesCsvFileName.Suffix.Should().Be(".states.csv");
    }

    [Theory]
    [InlineData("2026-11-01_24932322230.csv", "2026-11-01_24932322230.states.csv")]
    [InlineData("2026-11-01_120407_24932322230.csv", "2026-11-01_120407_24932322230.states.csv")]
    public void ForReplacesTheFinalCsvExtension(string csvFileName, string expected)
    {
        StatesCsvFileName.For(csvFileName).Should().Be(expected);
    }

    [Theory]
    [InlineData("x.txt")]
    [InlineData("csv")]
    public void ForThrowsWhenTheNameDoesNotEndInCsv(string name)
    {
        var act = () => StatesCsvFileName.For(name);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("a.states.csv", true)]
    [InlineData("A.STATES.CSV", true)]
    [InlineData("y.States.csv", true)]
    [InlineData("a.csv", false)]
    [InlineData("a.states.csv.bak", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsStatesCsvNameIgnoresCase(string? name, bool expected)
    {
        StatesCsvFileName.IsStatesCsvName(name).Should().Be(expected);
    }
}
