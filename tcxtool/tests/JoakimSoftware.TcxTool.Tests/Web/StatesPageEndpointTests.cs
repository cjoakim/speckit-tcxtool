using System.Net;
using System.Text.RegularExpressions;

using JoakimSoftware.TcxTool.Tests.Support;
using JoakimSoftware.TcxTool.Web.Analysis;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies <c>GET /states</c> through the real HTTP pipeline (specs/006-activity-states/contracts/pages.md
/// "States page"), against a host whose CSV directory and state file are temp paths (FR-026).
/// </summary>
public sealed class StatesPageEndpointTests : IDisposable
{
    private const string Name = "2026-11-01_fixture.csv";
    private const string StatesName = "2026-11-01_fixture.states.csv";

    private readonly TcxToolWebFactory _factory = new();

    [Fact]
    public async Task WithNoStateFileThePageSaysNoCsvIsSelectedAsync()
    {
        var (status, body) = await GetStatesAsync();

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("<title>States</title>")
            .And.Contain("<h1 class=\"h3 mb-3\">States</h1>")
            .And.Contain(CurrentStatesMessages.For(CurrentStatesStatus.NoCurrentCsv));
    }

    [Fact]
    public async Task ALoadedStatesCsvShowsTheHeadingsAndRowsAsync()
    {
        WriteLoaded();

        var (status, body) = await GetStatesAsync();

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("id=\"states\"").And.Contain($"States CSV: <strong>{StatesName}</strong>");
        var headings = Regex.Matches(body, "<th scope=\"col\">([^<]*)</th>").Select(match => match.Groups[1].Value);
        headings.Should().Equal(
            "ActivityId", "ActivityDistanceMiles", "ActivityElapsedTime", "ActivityElapsedSeconds", "State",
            "StateEndDate", "StateEndTrackpointNum", "StateTrackpointCount", "StateElapsedSeconds", "StateDistanceMiles",
            "AverageHeartRateBpm", "AverageRunCadence", "AverageLatitude", "AverageLongitude", "AverageAltitudeMeters",
            "AverageAltitudeFeet");
        var tbody = body[body.IndexOf("<tbody>", StringComparison.Ordinal)..body.IndexOf("</tbody>", StringComparison.Ordinal)];
        Regex.Count(tbody, "<tr>").Should().Be(3);
    }

    [Fact]
    public async Task TheBandClassesReachTheBrowserAsync()
    {
        StatesCsvFixture.Write(Path.Combine(_factory.CsvDirectory, StatesName),
            [StatesCsvFixture.Line(averageHeartRateBpm: "165.4", averageRunCadence: "172")]);
        ActivityCsvFixture.WriteState(_factory.StateFilePath, Name, StatesName);

        var (_, body) = await GetStatesAsync();

        body.Should().Contain("<td class=\"hr-band-5\">165.4</td>").And.Contain("<td class=\"cadence-band-3\">172</td>");
    }

    [Fact]
    public async Task EveryNoticeCaseIsAPageNotAnErrorAsync()
    {
        ActivityCsvFixture.WriteState(_factory.StateFilePath, Name);
        (await GetStatesAsync()).Body.Should().Contain(CurrentStatesMessages.For(CurrentStatesStatus.NoStatesCsv));

        ActivityCsvFixture.WriteState(_factory.StateFilePath, Name, StatesName);
        (await GetStatesAsync()).Body.Should().Contain(CurrentStatesMessages.For(CurrentStatesStatus.Missing));

        ActivityCsvFixture.WriteRaw(Path.Combine(_factory.CsvDirectory, StatesName), "hello\n");
        var (status, body) = await GetStatesAsync();
        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(CurrentStatesMessages.For(CurrentStatesStatus.WrongFormat)).And.NotContain("id=\"states\"");
    }

    // A States CSV written by feature 006 (13 fields) is the wrong format, with the Convert link
    // (feature 007 FR-006). The values are made up.
    [Fact]
    public async Task AFeature006StatesCsvShowsTheWrongFormatNoticeAsync()
    {
        ActivityCsvFixture.WriteRaw(Path.Combine(_factory.CsvDirectory, StatesName),
            "ActivityId|State|StateEndDate|StateEndTrackpointNum|StateElapsedSeconds|ActivityElapsedTime|"
            + "AverageLatitude|AverageLongitude|AverageHeartRateBpm|AverageRunCadence|DistanceMiles|"
            + "AverageAltitudeMeters|AverageAltitudeFeet\n"
            + "fixture-run|walking|2026-11-01T06:00:09.000Z|10|9|00:00:09|1.5|2.5|120|140|0.01|10|32.8\n");
        ActivityCsvFixture.WriteState(_factory.StateFilePath, Name, StatesName);

        var (status, body) = await GetStatesAsync();

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain($"{CurrentStatesMessages.For(CurrentStatesStatus.WrongFormat)} <strong>{StatesName}</strong>. <a href=\"convert\">Convert its TCX file again</a>.")
            .And.NotContain("id=\"states\"");
    }

    [Fact]
    public async Task ServingThePageChangesNoFileAsync()
    {
        WriteLoaded();
        var before = Snapshot();

        await GetStatesAsync();
        await GetStatesAsync();

        Snapshot().Should().BeEquivalentTo(before, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ThePageIsStaticallyRenderedAsync()
    {
        WriteLoaded();

        var (_, body) = await GetStatesAsync();

        // Only an interactive component writes <!--Blazor:{ (CLAUDE.md, lessons from feature 005).
        body.Should().NotContain("<!--Blazor:{");
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

    private async Task<(HttpStatusCode Status, string Body)> GetStatesAsync()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync(new Uri("/states", UriKind.Relative));
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private List<(string Path, byte[] Bytes, DateTime Written)> Snapshot()
    {
        var root = Path.GetDirectoryName(_factory.CsvDirectory)!;
        return [.. Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(path => (path, File.ReadAllBytes(path), File.GetLastWriteTimeUtc(path)))];
    }
}
