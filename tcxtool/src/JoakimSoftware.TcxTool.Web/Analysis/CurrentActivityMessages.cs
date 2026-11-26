namespace JoakimSoftware.TcxTool.Web.Analysis;

/// <summary>
/// The only place the Table page's status text is written (specs/005-table-page/contracts/pages.md
/// "Messages"). No message includes file content (spec FR-006).
/// </summary>
public static class CurrentActivityMessages
{
    /// <summary>The message for a status.</summary>
    /// <param name="status">The status.</param>
    /// <returns>The message, or an empty string for <see cref="CurrentActivityStatus.Loaded"/>.</returns>
    public static string For(CurrentActivityStatus status) => status switch
    {
        CurrentActivityStatus.NoCurrentCsv => "No CSV is selected yet.",
        CurrentActivityStatus.Missing => "The current CSV is no longer in the CSV folder:",
        CurrentActivityStatus.Unreadable => "The current CSV could not be read:",
        CurrentActivityStatus.WrongFormat => "The current CSV is not in the format this app writes:",
        CurrentActivityStatus.NoTrackpoints => "This activity has no trackpoints.",
        CurrentActivityStatus.Loaded => string.Empty,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
