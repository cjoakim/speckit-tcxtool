namespace JoakimSoftware.TcxTool.Parser.Tcx;

/// <summary>
/// A time read from a TCX element or attribute: its text exactly as written, and its parsed value.
/// </summary>
/// <param name="Text">The text, trimmed of XML whitespace. The CSV Time field writes this.</param>
/// <param name="Value">The time, in the offset written in the text.</param>
public sealed record RecordedTime(string Text, DateTimeOffset Value);
