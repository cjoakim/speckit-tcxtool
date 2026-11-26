namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>
/// One data row of an activity CSV, with the 19 fields of specs/006-activity-states/contracts/csv-format.md
/// in contract order, each exactly as written. An empty field is <c>""</c>, never null.
/// </summary>
/// <param name="ActivityId">Field 1, the sanitized upload name.</param>
/// <param name="Date">Field 2, <c>yyyy-MM-dd</c> of the activity.</param>
/// <param name="DeviceName">Field 3, the device name.</param>
/// <param name="DeviceId">Field 4, the device unit id.</param>
/// <param name="Sport">Field 5, the sport.</param>
/// <param name="TrackpointNum">Field 6, the 1-based trackpoint number.</param>
/// <param name="Time">Field 7, the trackpoint time as written.</param>
/// <param name="ElapsedSeconds">Field 8, seconds since the first timed trackpoint.</param>
/// <param name="ElapsedTime">Field 9, <c>HH:MM:SS</c> since the first timed trackpoint.</param>
/// <param name="Latitude">Field 10, latitude in degrees.</param>
/// <param name="Longitude">Field 11, longitude in degrees.</param>
/// <param name="HeartRateBpm">Field 12, heart rate in beats per minute.</param>
/// <param name="RunCadence">Field 13, run cadence in steps per minute (twice Garmin's per-foot value).</param>
/// <param name="State">Field 14, one of the <see cref="ActivityState"/> names.</param>
/// <param name="StateSeconds">Field 15, seconds since the previous StateSet ended.</param>
/// <param name="DistanceMeters">Field 16, distance in meters.</param>
/// <param name="DistanceMiles">Field 17, distance in miles.</param>
/// <param name="AltitudeMeters">Field 18, altitude in meters.</param>
/// <param name="AltitudeFeet">Field 19, altitude in feet.</param>
public sealed record ActivityCsvRow(
    string ActivityId,
    string Date,
    string DeviceName,
    string DeviceId,
    string Sport,
    string TrackpointNum,
    string Time,
    string ElapsedSeconds,
    string ElapsedTime,
    string Latitude,
    string Longitude,
    string HeartRateBpm,
    string RunCadence,
    string State,
    string StateSeconds,
    string DistanceMeters,
    string DistanceMiles,
    string AltitudeMeters,
    string AltitudeFeet);
