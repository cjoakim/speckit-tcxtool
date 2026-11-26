namespace JoakimSoftware.TcxTool.Parser.Tcx;

/// <summary>One recorded sample. Every value is optional because devices record different things.</summary>
/// <param name="Time">The sample time.</param>
/// <param name="Position">The position, when both coordinates were recorded.</param>
/// <param name="AltitudeMeters">The altitude in meters.</param>
/// <param name="DistanceMeters">The cumulative distance in meters.</param>
/// <param name="HeartRateBpm">The heart rate, from <c>HeartRateBpm/Value</c>.</param>
/// <param name="RunCadence">The run cadence, from the Activity Extension <c>TPX</c> element.</param>
/// <param name="Speed">The speed in meters per second, from the Activity Extension <c>TPX</c> element.</param>
/// <param name="Watts">The power in watts, from the Activity Extension <c>TPX</c> element.</param>
public sealed record TcxTrackpoint(
    RecordedTime? Time,
    TcxPosition? Position,
    RecordedNumber? AltitudeMeters,
    RecordedNumber? DistanceMeters,
    RecordedNumber? HeartRateBpm,
    RecordedNumber? RunCadence,
    RecordedNumber? Speed,
    RecordedNumber? Watts);
