using System.Diagnostics;
using System.Globalization;
using System.Text;

using JoakimSoftware.TcxTool.Parser.Conversion;
using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Converts a sample-sized synthetic activity (spec SC-001, SC-002). The TCX is built in memory, so
/// no large file is checked in and no real coordinates are used.
/// </summary>
public sealed class ConversionPerformanceTests : IDisposable
{
    private const int Trackpoints = 20_000;

    private readonly TempDirectory _output = new();

    [Fact]
    public async Task ASampleSizedActivityConvertsInUnderTenSecondsAsync()
    {
        await using var tcx = new MemoryStream(Encoding.UTF8.GetBytes(BuildTcx()));

        var stopwatch = Stopwatch.StartNew();
        var result = await TcxCsvConverter.ConvertAsync(tcx, "synthetic.tcx", _output.FullPath, CancellationToken.None);
        stopwatch.Stop();

        result.Files.Should().ContainSingle().Which.RowCount.Should().Be(Trackpoints);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
    }

    public void Dispose() => _output.Dispose();

    private static string BuildTcx()
    {
        var start = new DateTimeOffset(2026, 11, 1, 6, 0, 0, TimeSpan.Zero);
        var text = new StringBuilder();
        text.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<TrainingCenterDatabase ")
            .Append("xmlns=\"http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2\" ")
            .Append("xmlns:ns3=\"http://www.garmin.com/xmlschemas/ActivityExtension/v2\">\n<Activities>\n")
            .Append("<Activity Sport=\"Running\"><Id>2026-11-01T06:00:00.000Z</Id>")
            .Append("<Lap StartTime=\"2026-11-01T06:00:00.000Z\"><Track>\n");
        for (var i = 0; i < Trackpoints; i++)
        {
            text.Append(CultureInfo.InvariantCulture, $"""
                <Trackpoint><Time>{start.AddSeconds(i):yyyy-MM-ddTHH:mm:ss.fffZ}</Time>
                <Position><LatitudeDegrees>{i * 1e-6:R}</LatitudeDegrees><LongitudeDegrees>{-i * 1e-6:R}</LongitudeDegrees></Position>
                <AltitudeMeters>{100 + (i % 50) * 0.2:R}</AltitudeMeters><DistanceMeters>{i * 2.7:R}</DistanceMeters>
                <HeartRateBpm><Value>{120 + (i % 40)}</Value></HeartRateBpm>
                <Extensions><ns3:TPX><ns3:Speed>2.7</ns3:Speed><ns3:RunCadence>{80 + (i % 10)}</ns3:RunCadence></ns3:TPX></Extensions></Trackpoint>

                """);
        }

        return text.Append("</Track></Lap></Activity>\n</Activities>\n</TrainingCenterDatabase>\n").ToString();
    }
}
