using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Serves the Timeline page for 541 synthetic StateSets, the real marathon's count (spec SC-002, server
/// side). The rows are made up, so no real coordinates are used.
/// </summary>
public sealed partial class TimelinePagePerformanceTests : IDisposable
{
    private const int Rows = 541;
    private const string Name = "2026-11-01_synthetic.csv";
    private const string StatesName = "2026-11-01_synthetic.states.csv";

    private readonly TcxToolWebFactory _factory = new();

    [Fact]
    public async Task FiveHundredFortyOneStateSetsAreServedInUnderFiveSecondsAsync()
    {
        StatesCsvFixture.Write(Path.Combine(_factory.CsvDirectory, StatesName), StatesCsvFixture.Synthetic(Rows));
        ActivityCsvFixture.WriteState(_factory.StateFilePath, Name, StatesName);
        using var client = _factory.CreateClient();

        // Untimed warm-up: the first request starts the host and JIT-compiles the first render.
        using (var warmUp = await client.GetAsync(new Uri("/timeline", UriKind.Relative)))
        {
            await warmUp.Content.ReadAsStringAsync();
        }

        var stopwatch = Stopwatch.StartNew();
        using var response = await client.GetAsync(new Uri("/timeline", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        stopwatch.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var attribute = DataTimeline().Match(body);
        attribute.Success.Should().BeTrue();
        using var json = JsonDocument.Parse(WebUtility.HtmlDecode(attribute.Groups["json"].Value));
        json.RootElement.GetProperty("segments").GetArrayLength().Should().Be(Rows);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    public void Dispose() => _factory.Dispose();

    [GeneratedRegex("<tcx-timeline data-timeline=\"(?<json>[^\"]*)\"")]
    private static partial Regex DataTimeline();
}
