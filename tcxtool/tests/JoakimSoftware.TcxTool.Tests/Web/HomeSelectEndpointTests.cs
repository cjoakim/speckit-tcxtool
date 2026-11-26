using System.Net;

using JoakimSoftware.TcxTool.Tests.Support;
using JoakimSoftware.TcxTool.Web.State;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Exercises the home page's Select CSV File form through the real HTTP pipeline: form binding,
/// antiforgery, and the state file on disk (specs/004-about-and-csv-state/contracts/pages.md "POST /").
/// </summary>
public sealed class HomeSelectEndpointTests : IDisposable
{
    private const string Older = "2025-03-02_a.csv";
    private const string Newer = "2026-11-01_b.csv";
    private const string SeedState = "{\n  \"csvFilename\": \"2025-03-02_a.csv\"\n}\n";

    private readonly TcxToolWebFactory _host = new();

    public HomeSelectEndpointTests()
    {
        File.WriteAllText(Path.Combine(_host.CsvDirectory, Older), "header\n");
        File.WriteAllText(Path.Combine(_host.CsvDirectory, Newer), "header\n");
    }

    [Fact]
    public async Task GetListsTheCsvFilesAsync()
    {
        using var client = _host.CreateClient();

        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain($"<option value=\"{Newer}\"").And.Contain($"<option value=\"{Older}\"");
        body.IndexOf(Newer, StringComparison.Ordinal).Should().BeLessThan(body.IndexOf(Older, StringComparison.Ordinal));
    }

    // Feature 006 FR-015: a States CSV is never offered as a CSV.
    [Fact]
    public async Task GetDoesNotListStatesCsvFilesAsync()
    {
        File.WriteAllText(Path.Combine(_host.CsvDirectory, "2026-11-01_b.states.csv"), "header\n");
        using var client = _host.CreateClient();

        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain($"<option value=\"{Newer}\"").And.NotContain(".states.csv");
    }

    [Fact]
    public async Task SelectingACsvWithAStatesCsvRecordsBothAsync()
    {
        File.WriteAllText(Path.Combine(_host.CsvDirectory, "2026-11-01_b.states.csv"), "header\n");

        var (status, _) = await PostAsync(Newer, crossSite: false);

        status.Should().Be(HttpStatusCode.OK);
        File.ReadAllText(_host.StateFilePath).Should().Be(
            $"{{\n  \"csvFilename\": \"{Newer}\",\n  \"statesCsvFilename\": \"2026-11-01_b.states.csv\"\n}}\n");
    }

    [Fact]
    public async Task SelectingAListedCsvRecordsItAsync()
    {
        var (status, body) = await PostAsync(Newer, crossSite: false);

        status.Should().Be(HttpStatusCode.OK);
        File.ReadAllText(_host.StateFilePath).Should().Be($"{{\n  \"csvFilename\": \"{Newer}\"\n}}\n");
        body.Should().Contain($"Current CSV: <strong>{Newer}</strong>").And.Contain("role=\"status\"");
    }

    [Fact]
    public async Task SubmittingNothingChangesNothingAsync()
    {
        SeedTheState();

        var (status, body) = await PostAsync(string.Empty, crossSite: false);

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(CsvSelectionMessages.For(CsvSelectionOutcome.NoneChosen));
        File.ReadAllText(_host.StateFilePath).Should().Be(SeedState);
    }

    [Fact]
    public async Task APathEscapeIsRejectedAsync()
    {
        SeedTheState();

        var (status, body) = await PostAsync("../secrets.csv", crossSite: false);

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(CsvSelectionMessages.For(CsvSelectionOutcome.InvalidName));
        File.ReadAllText(_host.StateFilePath).Should().Be(SeedState);
    }

    [Fact]
    public async Task ANameThatIsNotOnDiskIsNotFoundAsync()
    {
        var (status, body) = await PostAsync("2020-01-01_gone.csv", crossSite: false);

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(CsvSelectionMessages.For(CsvSelectionOutcome.NotFound));
        File.Exists(_host.StateFilePath).Should().BeFalse();
    }

    [Fact]
    public async Task ANameThatIsGoneFromAnEmptyFolderIsNotFoundAsync()
    {
        // The last CSV was deleted after the page loaded, so the page now lists no files (T037).
        File.Delete(Path.Combine(_host.CsvDirectory, Older));
        File.Delete(Path.Combine(_host.CsvDirectory, Newer));

        var (status, body) = await PostAsync("2026-11-01_gone.csv", crossSite: false);

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(CsvSelectionMessages.For(CsvSelectionOutcome.NotFound)).And.Contain("No CSV files yet.");
        File.Exists(_host.StateFilePath).Should().BeFalse();
    }

    [Fact]
    public async Task ACrossSitePostIsRejectedAndChangesNothingAsync()
    {
        // .NET 11 antiforgery checks Sec-Fetch-Site and Origin rather than a hidden form token.
        var (status, _) = await PostAsync(Newer, crossSite: true);

        status.Should().Be(HttpStatusCode.BadRequest);
        File.Exists(_host.StateFilePath).Should().BeFalse();
    }

    public void Dispose() => _host.Dispose();

    private void SeedTheState()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_host.StateFilePath)!);
        File.WriteAllText(_host.StateFilePath, SeedState);
    }

    private async Task<(HttpStatusCode Status, string Body)> PostAsync(string csvFilename, bool crossSite)
    {
        using var client = _host.CreateClient();
        if (crossSite)
        {
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
        }

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "csv-select",
            ["CsvFilename"] = csvFilename,
        });
        using var response = await client.PostAsync(new Uri("/", UriKind.Relative), form);
        return (response.StatusCode, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }
}
