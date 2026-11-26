namespace JoakimSoftware.TcxTool.Parser.Tcx;

/// <summary>A parsed TCX file.</summary>
/// <param name="Activities">The <c>Activities/Activity</c> elements, in document order.</param>
public sealed record TcxDocument(IReadOnlyList<TcxActivity> Activities);
