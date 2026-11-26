using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Web.State;

/// <summary>
/// The single gate for CSV names from the form, the state file, and the directory listing
/// (spec 004 FR-010; contracts/state-file.md "Name rule"). A States CSV name never passes it
/// (feature 006 FR-015).
/// </summary>
public static class CsvFileNameRule
{
    private const string Extension = ".csv";

    /// <summary>
    /// True when <paramref name="name"/> is a plain file name ending in <c>.csv</c> that is not a States
    /// CSV name (<see cref="StatesCsvFileName.IsStatesCsvName"/>).
    /// </summary>
    /// <param name="name">The candidate name.</param>
    /// <returns>Whether the name may be listed, selected, or read from the state file.</returns>
    public static bool IsPlainCsvName(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && Path.GetFileName(name) == name
        && !name.Contains('/', StringComparison.Ordinal)
        && !name.Contains('\\', StringComparison.Ordinal)
        && name is not "." and not ".."
        && !Path.IsPathRooted(name)
        && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
        && name.Length > Extension.Length
        && name.EndsWith(Extension, StringComparison.Ordinal)
        && !StatesCsvFileName.IsStatesCsvName(name);
}
