using System.Net;
using System.Text.RegularExpressions;

using JoakimSoftware.TcxTool.Tests.Support;
using JoakimSoftware.TcxTool.Web.Analysis;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies <c>GET /table</c> through the real HTTP pipeline (specs/005-table-page/contracts/pages.md
/// "HTTP"), against a host whose CSV directory and state file are temp paths (FR-022).
/// </summary>
public sealed class TablePageEndpointTests : IDisposable
{
    private const string Name = "2026-11-01_fixture.csv";

    private readonly TcxToolWebFactory _factory = new();

    [Fact]
    public async Task WithNoStateFileThePageSaysNoCsvIsSelectedAsync()
    {
        var (status, body) = await GetTableAsync();

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("<title>Table</title>")
            .And.Contain(">Table</h1>")
            .And.Contain(CurrentActivityMessages.For(CurrentActivityStatus.NoCurrentCsv));
    }

    // Moved from PlaceholderPageEndpointTests, with the same assertions (feature 008 research.md R3).
    [Fact]
    public async Task ThePageIsServedInsideTheLayoutAsync()
    {
        var (status, body) = await GetTableAsync();

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("<title>Table</title>").And.Contain(">Table</h1>").And.Contain("navbar");
    }

    [Fact]
    public async Task ALoadedCsvShowsItsActivityCardAsync()
    {
        WriteLoaded();

        var (status, body) = await GetTableAsync();

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("id=\"activity-summary\"").And.Contain(Name);
    }

    [Fact]
    public async Task ALoadedCsvShowsTheTableWithItsHeadingsAndRowsAsync()
    {
        WriteLoaded();

        var (_, body) = await GetTableAsync();

        body.Should().Contain("id=\"trackpoints\"");
        var headings = Regex.Matches(body, "<th scope=\"col\">([^<]*)</th>").Select(match => match.Groups[1].Value);
        headings.Should().Equal(
            "TrackpointNum", "Time", "ElapsedSeconds", "ElapsedTime", "HeartRateBpm",
            "RunCadence", "DistanceMiles", "Latitude", "Longitude", "AltitudeFeet");
        var tbody = body[body.IndexOf("<tbody>", StringComparison.Ordinal)..body.IndexOf("</tbody>", StringComparison.Ordinal)];
        Regex.Count(tbody, "<tr>").Should().Be(3);
    }

    [Fact]
    public async Task TheBandClassesReachTheBrowserAsync()
    {
        ActivityCsvFixture.Write(Path.Combine(_factory.CsvDirectory, Name), [ActivityCsvFixture.Line(heartRateBpm: "185", runCadence: "176")]);
        ActivityCsvFixture.WriteState(_factory.StateFilePath, Name);

        var (_, body) = await GetTableAsync();

        body.Should().Contain("<td class=\"hr-band-6\">185</td>").And.Contain("<td class=\"cadence-band-3\">176</td>");
    }

    // A CSV written before feature 006 has 17 fields (feature 006 FR-017).
    [Fact]
    public async Task AFeature002CsvShowsTheWrongFormatNoticeWithTheConvertLinkAsync()
    {
        const string seventeenFieldHeader =
            "ActivityId|Date|DeviceName|DeviceId|Sport|TrackpointNum|Time|ElapsedSeconds|ElapsedTime|"
            + "Latitude|Longitude|HeartRateBpm|RunCadence|DistanceMeters|DistanceMiles|AltitudeMeters|AltitudeFeet";
        var line = string.Join('|', Enumerable.Repeat("1", 17));
        ActivityCsvFixture.WriteRaw(Path.Combine(_factory.CsvDirectory, Name), seventeenFieldHeader + "\n" + line + "\n");
        ActivityCsvFixture.WriteState(_factory.StateFilePath, Name);

        var (status, body) = await GetTableAsync();

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(CurrentActivityMessages.For(CurrentActivityStatus.WrongFormat))
            .And.Contain("<strong>" + Name + "</strong>. <a href=\"convert\">Convert its TCX file again</a>.")
            .And.NotContain("id=\"trackpoints\"");
    }

    [Fact]
    public async Task ServingThePageChangesNoFileAsync()
    {
        WriteLoaded();
        var before = Snapshot();

        await GetTableAsync();
        await GetTableAsync();

        Snapshot().Should().BeEquivalentTo(before, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ThePageIsStaticallyRenderedAsync()
    {
        WriteLoaded();

        var (_, body) = await GetTableAsync();

        // A static page still carries <!--Blazor-Server-Component-State: and <!--Blazor-Web-Initializers:
        // comments; only an interactive component writes <!--Blazor:{ (tasks.md T014).
        body.Should().NotContain("<!--Blazor:{");
    }

    public void Dispose() => _factory.Dispose();

    private void WriteLoaded()
    {
        ActivityCsvFixture.Write(Path.Combine(_factory.CsvDirectory, Name),
            [ActivityCsvFixture.Line(trackpointNum: "1"), ActivityCsvFixture.Line(trackpointNum: "2"), ActivityCsvFixture.Line(trackpointNum: "3")]);
        ActivityCsvFixture.WriteState(_factory.StateFilePath, Name);
    }

    private async Task<(HttpStatusCode Status, string Body)> GetTableAsync()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync(new Uri("/table", UriKind.Relative));
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
