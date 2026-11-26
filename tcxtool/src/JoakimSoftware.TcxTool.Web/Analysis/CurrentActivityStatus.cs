namespace JoakimSoftware.TcxTool.Web.Analysis;

/// <summary>What loading the current CSV found (specs/005-table-page/data-model.md).</summary>
public enum CurrentActivityStatus
{
    /// <summary>There is no current CSV.</summary>
    NoCurrentCsv,

    /// <summary>The current CSV's file is not in the CSV directory, or was deleted before it could be opened.</summary>
    Missing,

    /// <summary>The file exists but could not be opened or read.</summary>
    Unreadable,

    /// <summary>The file is not in the format this app writes.</summary>
    WrongFormat,

    /// <summary>The file is a valid CSV with a header line and no data rows.</summary>
    NoTrackpoints,

    /// <summary>The file is a valid CSV with one or more data rows.</summary>
    Loaded,
}
