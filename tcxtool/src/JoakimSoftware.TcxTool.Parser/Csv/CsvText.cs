namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>
/// Cleans a value for the unquoted <c>|</c>-delimited CSV format (spec FR-011b).
/// </summary>
public static class CsvText
{
    /// <summary>
    /// Replaces each <c>|</c> with a space and removes each carriage return and line feed. Double
    /// quotes are kept. A null value becomes an empty string.
    /// </summary>
    /// <param name="value">The value to clean.</param>
    /// <returns>The cleaned value.</returns>
    public static string Clean(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace('|', ' ').Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }
}
