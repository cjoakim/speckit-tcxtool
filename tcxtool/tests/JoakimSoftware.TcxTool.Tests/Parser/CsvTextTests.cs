using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Verifies field cleaning for the unquoted <c>|</c>-delimited format (spec FR-011b).
/// </summary>
public class CsvTextTests
{
    [Theory]
    [InlineData("Edge|1040", "Edge 1040")]
    [InlineData("a\r\nb", "ab")]
    [InlineData("a\nb", "ab")]
    [InlineData("a\rb", "ab")]
    [InlineData("\"quoted\"", "\"quoted\"")]
    [InlineData("Forerunner 265S", "Forerunner 265S")]
    [InlineData(null, "")]
    public void CleansValuesForTheDelimitedFormat(string? value, string expected)
    {
        CsvText.Clean(value).Should().Be(expected);
    }
}
