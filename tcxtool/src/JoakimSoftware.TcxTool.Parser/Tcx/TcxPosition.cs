namespace JoakimSoftware.TcxTool.Parser.Tcx;

/// <summary>A trackpoint position. A TCX position missing either coordinate is treated as absent.</summary>
/// <param name="LatitudeDegrees">The latitude.</param>
/// <param name="LongitudeDegrees">The longitude.</param>
public sealed record TcxPosition(RecordedNumber LatitudeDegrees, RecordedNumber LongitudeDegrees);
