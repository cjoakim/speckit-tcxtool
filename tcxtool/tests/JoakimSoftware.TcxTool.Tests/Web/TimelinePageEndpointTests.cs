using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

using JoakimSoftware.TcxTool.Tests.Support;
using JoakimSoftware.TcxTool.Web.Analysis;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies <c>GET /timeline</c> through the real HTTP pipeline (specs/007-timeline-page/contracts/pages.md
/// "Timeline page"), against a host whose CSV directory and state file are temp paths (FR-024).
/// </summary>
public sealed partial class TimelinePageEndpointTests : IDisposable
{
    private const string Name = "2026-11-01_fixture.csv";
    private const string StatesName = "2026-11-01_fixture.states.csv";

    private readonly TcxToolWebFactory _factory = new();

    [Fact]
    public async Task ThePageIsServedInsideTheLayoutWithTheTimelineLinkActiveAsync()
    {
        WriteLoaded();

        var (status, body) = await GetTimelineAsync();

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("<title>Timeline</title>").And.Contain("<h1 class=\"h3 mb-3\">Timeline</h1>");
        body.Should().Contain("href=\"timeline\" class=\"nav-link active\"");
    }

    [Fact]
    public async Task ThePageIsStaticallyRenderedAsync()
    {
        WriteLoaded();

        var (_, body) = await GetTimelineAsync();

        // Only an interactive component writes <!--Blazor:{ (CLAUDE.md, lessons from feature 005).
        body.Should().NotContain("<!--Blazor:{");
    }

    [Fact]
    public async Task ServingThePageChangesNoFileAsync()
    {
        WriteLoaded();
        var before = Snapshot();

        await GetTimelineAsync();
        await GetTimelineAsync();

        Snapshot().Should().BeEquivalentTo(before, options => options.WithStrictOrdering());
    }

    // research.md R7: the attribute is HTML-encoded, and the JSON parses back to the exact text.
    [Fact]
    public async Task ValuesThatLookLikeMarkupAreEncodedAndReadBackExactlyAsync()
    {
        const string tricky = "a\"b<c>&d</script>";
        StatesCsvFixture.Write(Path.Combine(_factory.CsvDirectory, StatesName), [StatesCsvFixture.Line(activityId: tricky)]);
        ActivityCsvFixture.WriteState(_factory.StateFilePath, Name, StatesName);

        var (_, body) = await GetTimelineAsync();

        var attribute = DataTimeline().Match(body);
        attribute.Success.Should().BeTrue();
        attribute.Groups["json"].Value.Should().NotContain("<").And.NotContain(">");
        using var json = JsonDocument.Parse(WebUtility.HtmlDecode(attribute.Groups["json"].Value));
        json.RootElement.GetProperty("segments")[0].GetProperty("values")[0].GetString().Should().Be(tricky);
    }

    [Fact]
    public async Task AFeature006StatesCsvShowsTheWrongFormatNoticeAsync()
    {
        ActivityCsvFixture.WriteRaw(Path.Combine(_factory.CsvDirectory, StatesName),
            "ActivityId|State|StateEndDate|StateEndTrackpointNum|StateElapsedSeconds|ActivityElapsedTime|"
            + "AverageLatitude|AverageLongitude|AverageHeartRateBpm|AverageRunCadence|DistanceMiles|"
            + "AverageAltitudeMeters|AverageAltitudeFeet\n"
            + "fixture-run|walking|2026-11-01T06:00:09.000Z|10|9|00:00:09|1.5|2.5|120|140|0.01|10|32.8\n");
        ActivityCsvFixture.WriteState(_factory.StateFilePath, Name, StatesName);

        var (status, body) = await GetTimelineAsync();

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("id=\"timeline-status\"")
            .And.Contain($"{CurrentStatesMessages.For(CurrentStatesStatus.WrongFormat)} <strong>{StatesName}</strong>. <a href=\"convert\">Convert its TCX file again</a>.")
            .And.NotContain("<tcx-timeline");
    }

    // FR-007: the page reads only the States CSV, so a missing activity CSV doesn't stop it.
    [Fact]
    public async Task ThePageNeedsOnlyTheStatesCsvAsync()
    {
        StatesCsvFixture.Write(Path.Combine(_factory.CsvDirectory, StatesName),
            [StatesCsvFixture.Line(), StatesCsvFixture.Line(state: "running"), StatesCsvFixture.Line()]);
        ActivityCsvFixture.WriteState(_factory.StateFilePath, Name, StatesName);
        File.Exists(Path.Combine(_factory.CsvDirectory, Name)).Should().BeFalse();

        var (status, body) = await GetTimelineAsync();

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("id=\"timeline-summary\"");
        using var json = JsonDocument.Parse(WebUtility.HtmlDecode(DataTimeline().Match(body).Groups["json"].Value));
        json.RootElement.GetProperty("segments").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task EveryPageLoadsTheTimelineModuleAsync()
    {
        var (_, body) = await GetTimelineAsync();

        ModuleScript().IsMatch(body).Should().BeTrue("App.razor should load js/timeline.js as a module");
    }

    public void Dispose() => _factory.Dispose();

    private void WriteLoaded()
    {
        ActivityCsvFixture.Write(Path.Combine(_factory.CsvDirectory, Name), [ActivityCsvFixture.Line()]);
        StatesCsvFixture.Write(Path.Combine(_factory.CsvDirectory, StatesName),
        [
            StatesCsvFixture.Line(stateEndTrackpointNum: "3"),
            StatesCsvFixture.Line(state: "running", stateEndTrackpointNum: "7"),
            StatesCsvFixture.Line(stateEndTrackpointNum: "9"),
        ]);
        ActivityCsvFixture.WriteState(_factory.StateFilePath, Name, StatesName);
    }

    private async Task<(HttpStatusCode Status, string Body)> GetTimelineAsync()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync(new Uri("/timeline", UriKind.Relative));
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private List<(string Path, byte[] Bytes, DateTime Written)> Snapshot()
    {
        var root = Path.GetDirectoryName(_factory.CsvDirectory)!;
        return [.. Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(path => (path, File.ReadAllBytes(path), File.GetLastWriteTimeUtc(path)))];
    }

    [GeneratedRegex("<tcx-timeline data-timeline=\"(?<json>[^\"]*)\"")]
    private static partial Regex DataTimeline();

    [GeneratedRegex("<script type=\"module\" src=\"/?js/timeline(\\.[a-z0-9]+)?\\.js\"></script>")]
    private static partial Regex ModuleScript();
}
