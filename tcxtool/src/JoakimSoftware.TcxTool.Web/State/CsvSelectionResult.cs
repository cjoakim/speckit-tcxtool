namespace JoakimSoftware.TcxTool.Web.State;

/// <summary>The result of offering a CSV as the current one (data-model.md CsvSelectionResult).</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="FileName">The requested name as given, or null for <see cref="CsvSelectionOutcome.NoneChosen"/>.</param>
public sealed record CsvSelectionResult(CsvSelectionOutcome Outcome, string? FileName)
{
    /// <summary>True when the state file now names <see cref="FileName"/>.</summary>
    public bool IsRecorded => Outcome == CsvSelectionOutcome.Recorded;
}
