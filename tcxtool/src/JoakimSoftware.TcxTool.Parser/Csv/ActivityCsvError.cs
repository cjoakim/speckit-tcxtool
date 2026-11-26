namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>The first problem found in an input that is not an activity CSV. It never holds file content.</summary>
/// <param name="Kind">The kind of problem.</param>
/// <param name="LineNumber">The 1-based line: always 1 for <see cref="ActivityCsvErrorKind.HeaderMismatch"/>, 2 or more for <see cref="ActivityCsvErrorKind.FieldCount"/>.</param>
public sealed record ActivityCsvError(ActivityCsvErrorKind Kind, int LineNumber);
