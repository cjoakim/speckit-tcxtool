namespace JoakimSoftware.TcxTool.Web.Analysis;

/// <summary>
/// The only place the States page's status text is written (specs/006-activity-states/data-model.md
/// <c>CurrentStatesMessages</c>). No message includes file content.
/// </summary>
public static class CurrentStatesMessages
{
    /// <summary>The message for a status.</summary>
    /// <param name="status">The status.</param>
    /// <returns>The message, or an empty string for <see cref="CurrentStatesStatus.Loaded"/>.</returns>
    public static string For(CurrentStatesStatus status) => status switch
    {
        CurrentStatesStatus.NoCurrentCsv => "No CSV is selected yet.",
        CurrentStatesStatus.NoStatesCsv => "The current CSV has no States CSV:",
        CurrentStatesStatus.Missing => "The States CSV is no longer in the CSV folder:",
        CurrentStatesStatus.Unreadable => "The States CSV could not be read:",
        CurrentStatesStatus.WrongFormat => "The States CSV is not in the format this app writes:",
        CurrentStatesStatus.NoStateSets => "This activity has no state changes.",
        CurrentStatesStatus.Loaded => string.Empty,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
