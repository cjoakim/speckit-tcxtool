namespace JoakimSoftware.TcxTool.Parser.Tcx;

/// <summary>
/// A number read from a TCX element: its text exactly as written, and its parsed value.
/// </summary>
/// <param name="Text">The element text, trimmed of XML whitespace. Copied CSV fields write this.</param>
/// <param name="Value">The finite value parsed with the invariant culture. Derived CSV fields use this.</param>
public sealed record RecordedNumber(string Text, double Value);
