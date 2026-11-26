namespace JoakimSoftware.TcxTool.Web.Analysis;

/// <summary>What loading the current CSV's States CSV found (specs/006-activity-states/data-model.md).</summary>
public enum CurrentStatesStatus
{
    /// <summary>There is no current CSV.</summary>
    NoCurrentCsv,

    /// <summary>The state file records no States CSV for the current CSV, or one that is not its own.</summary>
    NoStatesCsv,

    /// <summary>The States CSV is not in the CSV directory, or was deleted before it could be opened.</summary>
    Missing,

    /// <summary>The States CSV exists but could not be opened or read.</summary>
    Unreadable,

    /// <summary>The States CSV is not in the format this app writes.</summary>
    WrongFormat,

    /// <summary>The States CSV has a header line and no data rows.</summary>
    NoStateSets,

    /// <summary>The States CSV has one or more data rows.</summary>
    Loaded,
}
