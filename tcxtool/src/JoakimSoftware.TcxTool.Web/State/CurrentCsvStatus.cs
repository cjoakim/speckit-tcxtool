namespace JoakimSoftware.TcxTool.Web.State;

/// <summary>Whether there is a current CSV, and whether its file still exists (data-model.md).</summary>
public enum CurrentCsvStatus
{
    /// <summary>No state file, or the file is unreadable or invalid.</summary>
    None,

    /// <summary>The state names a file that exists in the CSV directory.</summary>
    Present,

    /// <summary>The state names a valid name, but that file is not in the CSV directory.</summary>
    Missing,
}
