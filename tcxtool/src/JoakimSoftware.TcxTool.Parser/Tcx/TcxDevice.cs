namespace JoakimSoftware.TcxTool.Parser.Tcx;

/// <summary>The device that recorded an activity, from the activity's <c>Creator</c> element.</summary>
/// <param name="Name">The device name, such as <c>Forerunner 265S</c>.</param>
/// <param name="UnitId">The device unit id text.</param>
public sealed record TcxDevice(string? Name, string? UnitId);
