namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>
/// One data row of a States CSV, summarizing one StateSet, with the 16 fields of
/// specs/007-timeline-page/contracts/states-csv-format.md in contract order, each exactly as
/// written. An empty field is <c>""</c>, never null.
/// </summary>
/// <param name="ActivityId">Field 1, the set's ActivityId.</param>
/// <param name="ActivityDistanceMiles">Field 2, the DistanceMiles of the set's last row that has one.</param>
/// <param name="ActivityElapsedTime">Field 3, the ElapsedTime of the set's last row that has one.</param>
/// <param name="ActivityElapsedSeconds">Field 4, the ElapsedSeconds of the set's last row that has one.</param>
/// <param name="State">Field 5, the set's <see cref="ActivityState"/> name.</param>
/// <param name="StateEndDate">Field 6, the Time of the set's last row that has one.</param>
/// <param name="StateEndTrackpointNum">Field 7, the TrackpointNum of the set's last row.</param>
/// <param name="StateTrackpointCount">Field 8, the number of rows in the set.</param>
/// <param name="StateElapsedSeconds">Field 9, the StateSeconds of the set's last row that has one.</param>
/// <param name="StateDistanceMiles">Field 10, the miles covered during the set (feature 006's DistanceMiles).</param>
/// <param name="AverageHeartRateBpm">Field 11, the mean HeartRateBpm.</param>
/// <param name="AverageRunCadence">Field 12, the mean RunCadence, in steps per minute.</param>
/// <param name="AverageLatitude">Field 13, the mean Latitude.</param>
/// <param name="AverageLongitude">Field 14, the mean Longitude.</param>
/// <param name="AverageAltitudeMeters">Field 15, the mean AltitudeMeters.</param>
/// <param name="AverageAltitudeFeet">Field 16, the mean AltitudeFeet.</param>
public sealed record StatesCsvRow(
    string ActivityId,
    string ActivityDistanceMiles,
    string ActivityElapsedTime,
    string ActivityElapsedSeconds,
    string State,
    string StateEndDate,
    string StateEndTrackpointNum,
    string StateTrackpointCount,
    string StateElapsedSeconds,
    string StateDistanceMiles,
    string AverageHeartRateBpm,
    string AverageRunCadence,
    string AverageLatitude,
    string AverageLongitude,
    string AverageAltitudeMeters,
    string AverageAltitudeFeet);
