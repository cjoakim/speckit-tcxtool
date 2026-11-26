using JoakimSoftware.TcxTool.Web.Table;

namespace JoakimSoftware.TcxTool.Tests.Web.Table;

/// <summary>
/// Verifies <see cref="TrackpointBands"/> against specs/005-table-page/data-model.md <c>TrackpointBands</c>
/// and research R6: every band edge (FR-014, FR-015) and the values that get no band (FR-016).
/// </summary>
public class TrackpointBandsTests
{
    public static TheoryData<string> NoBand => new() { "", "abc", "NaN", "Infinity", "-Infinity", "1,5" };

    [Theory]
    [InlineData("-5", "hr-band-1")]
    [InlineData("0", "hr-band-1")]
    [InlineData("89", "hr-band-1")]
    [InlineData("89.999", "hr-band-1")]
    [InlineData("90", "hr-band-2")]
    [InlineData("119", "hr-band-2")]
    [InlineData("119.5", "hr-band-2")]
    [InlineData("120", "hr-band-3")]
    [InlineData("139", "hr-band-3")]
    [InlineData("140", "hr-band-4")]
    [InlineData("159", "hr-band-4")]
    [InlineData("160", "hr-band-5")]
    [InlineData("179", "hr-band-5")]
    [InlineData("179.5", "hr-band-5")]
    [InlineData("180", "hr-band-6")]
    [InlineData("250", "hr-band-6")]
    public void HeartRateFallsInTheBandWhoseRangeContainsIt(string value, string band)
    {
        TrackpointBands.HeartRateClass(value).Should().Be(band);
    }

    // CSV values are already steps per minute and are compared as written (feature 006 FR-018).
    [Theory]
    [InlineData("0", "cadence-band-1")]
    [InlineData("19", "cadence-band-1")]
    [InlineData("19.99", "cadence-band-1")]
    [InlineData("20", "cadence-band-2")]
    [InlineData("100", "cadence-band-2")]
    [InlineData("149.5", "cadence-band-2")]
    [InlineData("150", "cadence-band-3")]
    [InlineData("176", "cadence-band-3")]
    [InlineData("179.5", "cadence-band-3")]
    [InlineData("180", "cadence-band-4")]
    [InlineData("250", "cadence-band-4")]
    public void RunCadenceAsWrittenFallsInTheBandWhoseRangeContainsIt(string value, string band)
    {
        TrackpointBands.RunCadenceClass(value).Should().Be(band);
    }

    [Theory]
    [MemberData(nameof(NoBand))]
    public void EmptyNonNumericOrNonFiniteValuesGetNoBand(string value)
    {
        TrackpointBands.HeartRateClass(value).Should().BeNull();
        TrackpointBands.RunCadenceClass(value).Should().BeNull();
    }
}
