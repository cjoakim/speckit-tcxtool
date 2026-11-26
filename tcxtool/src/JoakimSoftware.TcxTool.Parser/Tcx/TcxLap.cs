namespace JoakimSoftware.TcxTool.Parser.Tcx;

/// <summary>
/// A segment of an activity. Summary values are parsed for later features; this feature's CSV
/// writes only the trackpoints.
/// </summary>
/// <param name="StartTime">The <c>StartTime</c> attribute.</param>
/// <param name="TotalTimeSeconds">The lap duration in seconds.</param>
/// <param name="DistanceMeters">The lap distance in meters.</param>
/// <param name="MaximumSpeed">The maximum speed in meters per second.</param>
/// <param name="Calories">The calories.</param>
/// <param name="AverageHeartRateBpm">The average heart rate.</param>
/// <param name="MaximumHeartRateBpm">The maximum heart rate.</param>
/// <param name="Intensity">The intensity text, such as <c>Active</c>.</param>
/// <param name="TriggerMethod">The trigger method text, such as <c>Manual</c>.</param>
/// <param name="Trackpoints">The trackpoints of every track in the lap, in document order.</param>
public sealed record TcxLap(
    RecordedTime? StartTime,
    RecordedNumber? TotalTimeSeconds,
    RecordedNumber? DistanceMeters,
    RecordedNumber? MaximumSpeed,
    RecordedNumber? Calories,
    RecordedNumber? AverageHeartRateBpm,
    RecordedNumber? MaximumHeartRateBpm,
    string? Intensity,
    string? TriggerMethod,
    IReadOnlyList<TcxTrackpoint> Trackpoints);
