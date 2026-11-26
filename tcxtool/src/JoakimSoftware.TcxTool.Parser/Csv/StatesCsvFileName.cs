namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>
/// Names the States CSV that sits beside an activity CSV
/// (specs/006-activity-states/contracts/states-csv-format.md "File").
/// </summary>
public static class StatesCsvFileName
{
    /// <summary>The ending of every States CSV name.</summary>
    public const string Suffix = ".states.csv";

    private const string CsvExtension = ".csv";

    /// <summary>Returns the States CSV name for an activity CSV name.</summary>
    /// <param name="csvFileName">The activity CSV name, ending in <c>.csv</c>.</param>
    /// <returns>The name with its final <c>.csv</c> replaced by <see cref="Suffix"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="csvFileName"/> does not end in <c>.csv</c>.</exception>
    public static string For(string csvFileName)
    {
        ArgumentNullException.ThrowIfNull(csvFileName);
        if (csvFileName.Length <= CsvExtension.Length || !csvFileName.EndsWith(CsvExtension, StringComparison.Ordinal))
        {
            throw new ArgumentException("The name must end in .csv.", nameof(csvFileName));
        }

        return string.Concat(csvFileName.AsSpan(0, csvFileName.Length - CsvExtension.Length), Suffix);
    }

    /// <summary>True when <paramref name="name"/> ends in <see cref="Suffix"/>, ignoring case.</summary>
    /// <param name="name">The candidate name.</param>
    /// <returns>Whether the name is a States CSV name.</returns>
    public static bool IsStatesCsvName(string? name) =>
        name is not null && name.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase);
}
