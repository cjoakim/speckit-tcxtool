namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>Why an input is not an activity CSV (specs/005-table-page/contracts/activity-csv-reader.md).</summary>
public enum ActivityCsvErrorKind
{
    /// <summary>The input is empty, or its first line is not exactly <see cref="ActivityCsvWriter.Header"/>.</summary>
    HeaderMismatch,

    /// <summary>A data line does not split into exactly <see cref="ActivityCsvReader.FieldCount"/> fields on <c>|</c>.</summary>
    FieldCount,
}
