using System.Globalization;
using System.Text;

namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>
/// Formats calculated values (miles, feet, elapsed seconds) as the shortest plain decimal that reads
/// back as the same <see cref="double"/>, independent of the current culture.
/// </summary>
public static class DerivedNumber
{
    /// <summary>
    /// Formats <paramref name="value"/> with the shortest round-trip digits, a period as the decimal
    /// separator, no exponent, no trailing <c>.0</c>, and <c>-0</c> written as <c>0</c>.
    /// </summary>
    /// <param name="value">A finite value.</param>
    /// <returns>The formatted value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not finite.</exception>
    public static string Format(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Only finite values can be formatted.");
        }

        if (value == 0)
        {
            return "0";
        }

        var roundTrip = value.ToString("R", CultureInfo.InvariantCulture);
        var exponentAt = roundTrip.IndexOf('E', StringComparison.Ordinal);
        return exponentAt < 0 ? roundTrip : ExpandExponent(roundTrip, exponentAt);
    }

    // Moves the decimal point of "d.dddE±nn" without changing any digit.
    private static string ExpandExponent(string roundTrip, int exponentAt)
    {
        var negative = roundTrip[0] == '-';
        var mantissa = roundTrip[(negative ? 1 : 0)..exponentAt];
        var exponent = int.Parse(roundTrip[(exponentAt + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

        var pointAt = mantissa.IndexOf('.', StringComparison.Ordinal);
        var digits = pointAt < 0 ? mantissa : mantissa.Remove(pointAt, 1);
        var integerDigits = (pointAt < 0 ? mantissa.Length : pointAt) + exponent;

        var result = new StringBuilder();
        if (negative)
        {
            result.Append('-');
        }

        if (integerDigits <= 0)
        {
            result.Append("0.").Append('0', -integerDigits).Append(digits);
        }
        else if (integerDigits >= digits.Length)
        {
            result.Append(digits).Append('0', integerDigits - digits.Length);
        }
        else
        {
            result.Append(digits, 0, integerDigits).Append('.').Append(digits, integerDigits, digits.Length - integerDigits);
        }

        return result.ToString();
    }
}
