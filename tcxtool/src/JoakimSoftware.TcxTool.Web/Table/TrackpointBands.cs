using System.Globalization;

namespace JoakimSoftware.TcxTool.Web.Table;

/// <summary>
/// Maps a HeartRateBpm or RunCadence value to its band's CSS class (specs/005-table-page/data-model.md
/// <c>TrackpointBands</c>, research R6; cadence edges from specs/006-activity-states/data-model.md). A value is in a band when it is at least the band's lower edge
/// and below the next band's. An empty, non-numeric, or non-finite value has no band (spec FR-016).
/// </summary>
public static class TrackpointBands
{
    // Heart rate edges in beats per minute (spec FR-014).
    private const double HeartRateBand2 = 90;
    private const double HeartRateBand3 = 120;
    private const double HeartRateBand4 = 140;
    private const double HeartRateBand5 = 160;
    private const double HeartRateBand6 = 180;

    // Cadence edges in steps per minute, compared with the CSV value as written (feature 006 FR-018).
    private const double CadenceBand2StepsPerMinute = 20;
    private const double CadenceBand3StepsPerMinute = 150;
    private const double CadenceBand4StepsPerMinute = 180;

    private const string HeartRate1 = "hr-band-1";
    private const string HeartRate2 = "hr-band-2";
    private const string HeartRate3 = "hr-band-3";
    private const string HeartRate4 = "hr-band-4";
    private const string HeartRate5 = "hr-band-5";
    private const string HeartRate6 = "hr-band-6";
    private const string Cadence1 = "cadence-band-1";
    private const string Cadence2 = "cadence-band-2";
    private const string Cadence3 = "cadence-band-3";
    private const string Cadence4 = "cadence-band-4";

    /// <summary>The heart-rate band class for a HeartRateBpm value.</summary>
    /// <param name="value">The CSV text.</param>
    /// <returns><c>hr-band-1</c> to <c>hr-band-6</c>, or null when the value has no band.</returns>
    public static string? HeartRateClass(string value) => Parse(value) switch
    {
        null => null,
        < HeartRateBand2 => HeartRate1,
        < HeartRateBand3 => HeartRate2,
        < HeartRateBand4 => HeartRate3,
        < HeartRateBand5 => HeartRate4,
        < HeartRateBand6 => HeartRate5,
        _ => HeartRate6,
    };

    /// <summary>The cadence band class for a RunCadence value.</summary>
    /// <param name="value">The CSV text: the cadence in steps per minute, compared as written.</param>
    /// <returns><c>cadence-band-1</c> to <c>cadence-band-4</c>, or null when the value has no band.</returns>
    public static string? RunCadenceClass(string value) => Parse(value) switch
    {
        null => null,
        < CadenceBand2StepsPerMinute => Cadence1,
        < CadenceBand3StepsPerMinute => Cadence2,
        < CadenceBand4StepsPerMinute => Cadence3,
        _ => Cadence4,
    };

    private static double? Parse(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number)
            ? number
            : null;
}
