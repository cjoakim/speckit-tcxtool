namespace JoakimSoftware.TcxTool.Web.State;

/// <summary>What happened when a CSV was offered as the current one (data-model.md).</summary>
public enum CsvSelectionOutcome
{
    /// <summary>The name passed every check and the state file was written.</summary>
    Recorded,

    /// <summary>The name was null, empty, or whitespace.</summary>
    NoneChosen,

    /// <summary>The name failed the name rule, or resolved outside the CSV directory.</summary>
    InvalidName,

    /// <summary>The name was valid, but no such file is in the CSV directory.</summary>
    NotFound,

    /// <summary>Writing the state file failed; the previous state is untouched.</summary>
    WriteFailed,
}
