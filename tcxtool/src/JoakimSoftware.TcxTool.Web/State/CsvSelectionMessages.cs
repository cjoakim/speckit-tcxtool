namespace JoakimSoftware.TcxTool.Web.State;

/// <summary>
/// The only place this feature's user-facing selection text is written
/// (specs/004-about-and-csv-state/contracts/pages.md "Messages").
/// </summary>
public static class CsvSelectionMessages
{
    /// <summary>The message for an outcome that did not record a CSV.</summary>
    /// <param name="outcome">The outcome.</param>
    /// <returns>The message, or an empty string for <see cref="CsvSelectionOutcome.Recorded"/>.</returns>
    public static string For(CsvSelectionOutcome outcome) => outcome switch
    {
        CsvSelectionOutcome.NoneChosen => "Choose a CSV file to select.",
        CsvSelectionOutcome.InvalidName => "That is not a CSV file this app can use.",
        CsvSelectionOutcome.NotFound => "That CSV file is no longer in the CSV folder.",
        CsvSelectionOutcome.WriteFailed => "The current CSV could not be recorded.",
        CsvSelectionOutcome.Recorded => string.Empty,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
    };
}
