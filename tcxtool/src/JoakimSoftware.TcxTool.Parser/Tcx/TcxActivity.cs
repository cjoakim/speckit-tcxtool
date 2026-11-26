namespace JoakimSoftware.TcxTool.Parser.Tcx;

/// <summary>One recorded workout.</summary>
/// <param name="Id">The activity <c>Id</c>, its start time. Required.</param>
/// <param name="Sport">The <c>Sport</c> attribute, such as <c>Running</c>.</param>
/// <param name="Creator">The recording device, wherever <c>Creator</c> appears in the activity.</param>
/// <param name="Laps">The laps, in document order.</param>
public sealed record TcxActivity(RecordedTime Id, string? Sport, TcxDevice? Creator, IReadOnlyList<TcxLap> Laps);
