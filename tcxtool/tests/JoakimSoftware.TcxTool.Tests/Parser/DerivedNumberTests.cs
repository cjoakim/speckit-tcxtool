using System.Globalization;

using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Verifies the shortest-decimal format for derived values (spec FR-011a, research R5,
/// contracts/csv-format.md "Shortest decimal").
/// </summary>
public class DerivedNumberTests
{
    [Theory]
    [InlineData(0.0, "0")]
    [InlineData(1.5199999809265137 / 1609.344, "0.0009444842003490327")]
    [InlineData(0.01 / 1609.344, "0.000006213711922373339")]
    [InlineData(42481.4296875 / 1609.344, "26.39673661286835")]
    [InlineData(-2 / 0.3048, "-6.561679790026246")]
    [InlineData(3725.0, "3725")]
    [InlineData(3725.6, "3725.6")]
    [InlineData(1e16, "10000000000000000")]
    [InlineData(1.5e-10, "0.00000000015")]
    public void FormatsAsShortestPlainDecimal(double value, string expected)
    {
        DerivedNumber.Format(value).Should().Be(expected);
    }

    [Fact]
    public void FormatsNegativeZeroAsZero()
    {
        DerivedNumber.Format(-0.0).Should().Be("0");
    }

    [Theory]
    [InlineData(0.0009444842003490327)]
    [InlineData(0.000006213711922373339)]
    [InlineData(339.8950081171952)]
    [InlineData(-6.561679790026246)]
    [InlineData(123456789012345680000.0)]
    [InlineData(1.5e-10)]
    public void FormattedValueReadsBackAsTheSameDouble(double value)
    {
        var text = DerivedNumber.Format(value);

        text.Should().NotContainAny("E", "e");
        double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture).Should().Be(value);
    }

    [Fact]
    public void FormatIgnoresTheCurrentCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            DerivedNumber.Format(3725.6).Should().Be("3725.6");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
