using JoakimSoftware.TcxTool.Web.State;

namespace JoakimSoftware.TcxTool.Tests.Web.State;

/// <summary>
/// Verifies the name rule in specs/004-about-and-csv-state/contracts/state-file.md "Name rule"
/// (spec FR-010), with States CSV names left out (feature 006 FR-015, contracts/state-file.md).
/// </summary>
public class CsvFileNameRuleTests
{
    [Theory]
    [InlineData("2026-11-01_24932322230.csv", true)]
    [InlineData("2026-11-01_120407_24932322230.csv", true)]
    [InlineData("a.csv", true)]
    [InlineData("notes.txt", false)]
    [InlineData("REPORT.CSV", false)]
    [InlineData("report.Csv", false)]
    [InlineData(".csv", false)]
    [InlineData("../secrets.csv", false)]
    [InlineData("/etc/passwd.csv", false)]
    [InlineData("sub/x.csv", false)]
    [InlineData("sub\\x.csv", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(".", false)]
    [InlineData("..", false)]
    [InlineData("bad\0name.csv", false)]
    [InlineData(null, false)]
    [InlineData("2026-11-01_24932322230.states.csv", false)]
    [InlineData("x.STATES.csv", false)]
    [InlineData("states.csv", true)]
    [InlineData("a_states.csv", true)]
    public void AcceptsOnlyPlainCsvNames(string? name, bool expected)
    {
        CsvFileNameRule.IsPlainCsvName(name).Should().Be(expected);
    }
}
