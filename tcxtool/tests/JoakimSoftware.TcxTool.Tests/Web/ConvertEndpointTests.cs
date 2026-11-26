using System.Net;
using System.Net.Http.Headers;

using JoakimSoftware.TcxTool.Parser.Conversion;
using JoakimSoftware.TcxTool.Tests.Support;
using JoakimSoftware.TcxTool.Web.State;

using Microsoft.AspNetCore.Mvc.Testing;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Exercises <c>/convert</c> through the real HTTP pipeline: form binding, antiforgery, middleware
/// order, and files on disk (contracts/convert-page.md, research R10).
/// </summary>
public sealed class ConvertEndpointTests : IDisposable
{
    private readonly TcxToolWebFactory _host = new();

    [Fact]
    public async Task GetShowsTheUploadFormAsync()
    {
        using var client = _host.CreateClient();

        var response = await client.GetAsync(new Uri("/convert", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("enctype=\"multipart/form-data\"").And.Contain("name=\"TcxFile\"");
    }

    [Fact]
    public async Task CrossSitePostIsRejectedAndWritesNothingAsync()
    {
        // .NET 11 antiforgery checks Sec-Fetch-Site and Origin rather than a hidden form token.
        var (status, _) = await UploadAsync(
            _host, "run-two-laps.tcx", File.ReadAllBytes(Fixture.Path("run-two-laps.tcx")), crossSite: true, CancellationToken.None);

        status.Should().Be(HttpStatusCode.BadRequest);
        _host.CsvFileNames().Should().BeEmpty();
    }

    [Fact]
    public async Task UploadWritesTheCsvAndListsItAsync()
    {
        var (status, body) = await UploadAsync(_host, "run-two-laps.tcx", File.ReadAllBytes(Fixture.Path("run-two-laps.tcx")), crossSite: false, CancellationToken.None);

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("2026-11-01_run-two-laps.csv: 10 rows")
            .And.Contain("<li>2026-11-01_run-two-laps.states.csv: 1 StateSet</li>");
        File.ReadAllBytes(Path.Combine(_host.CsvDirectory, "2026-11-01_run-two-laps.csv"))
            .Should().Equal(File.ReadAllBytes(Fixture.ExpectedPath("run-two-laps.csv")));
        File.ReadAllBytes(Path.Combine(_host.CsvDirectory, "2026-11-01_run-two-laps.states.csv"))
            .Should().Equal(File.ReadAllBytes(Fixture.ExpectedPath("run-two-laps.states.csv")));
    }

    [Fact]
    public async Task UploadLeavesOnlyTheCsvAndItsStatesCsvInTheOutputDirectoryAsync()
    {
        await UploadAsync(_host, "run-two-laps.tcx", File.ReadAllBytes(Fixture.Path("run-two-laps.tcx")), crossSite: false, CancellationToken.None);

        _host.CsvFileNames().Should().Equal("2026-11-01_run-two-laps.csv", "2026-11-01_run-two-laps.states.csv");
    }

    [Fact]
    public async Task UploadWithoutTrackpointsSaysSoAndWritesTheHeaderAsync()
    {
        var (status, body) = await UploadAsync(_host, "no-trackpoints.tcx", File.ReadAllBytes(Fixture.Path("no-trackpoints.tcx")), crossSite: false, CancellationToken.None);

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("contains no trackpoints");
        File.Exists(Path.Combine(_host.CsvDirectory, "2026-11-01_no-trackpoints.csv")).Should().BeTrue();
    }

    [Fact]
    public async Task PostingWithoutAFileAsksForOneAsync()
    {
        var (status, body) = await UploadAsync(_host, null, null, crossSite: false, CancellationToken.None);

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("Choose a .tcx file to convert.");
        _host.CsvFileNames().Should().BeEmpty();
    }

    [Theory]
    [InlineData("malformed.tcx", TcxErrorKind.Malformed)]
    [InlineData("not-tcx.tcx", TcxErrorKind.NotTcx)]
    [InlineData("dtd.tcx", TcxErrorKind.ProhibitedContent)]
    [InlineData("invalid-number-nan.tcx", TcxErrorKind.InvalidValue)]
    [InlineData("missing-id.tcx", TcxErrorKind.InvalidActivityId)]
    [InlineData("duplicate-start.tcx", TcxErrorKind.DuplicateActivityStart)]
    public async Task AnInvalidFileShowsItsMessageAndWritesNothingAsync(string fixture, TcxErrorKind kind)
    {
        var (status, body) = await UploadAsync(_host, fixture, File.ReadAllBytes(Fixture.Path(fixture)), crossSite: false, CancellationToken.None);

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(kind == TcxErrorKind.InvalidValue
            ? TcxErrorMessages.For(kind, element: "AltitudeMeters", trackpointNumber: 2)
            : TcxErrorMessages.For(kind));
        _host.CsvFileNames().Should().BeEmpty();
    }

    [Fact]
    public async Task AFileOverTheLimitShowsTooLargeAsync()
    {
        using var host = CreateSmallLimitHost();

        var (status, body) = await UploadAsync(host, "padded.tcx", new byte[1500], crossSite: false, CancellationToken.None);

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain(TcxErrorMessages.For(TcxErrorKind.TooLarge, SmallLimit));
        host.CsvFileNames().Should().BeEmpty();
    }

    [Fact]
    public async Task ARequestOverTheCeilingIsRedirectedBeforeTheFormIsReadAsync()
    {
        using var host = CreateSmallLimitHost();
        using var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var form = new MultipartFormDataContent
        {
            { new StringContent("tcx-upload"), "_handler" },
            { new ByteArrayContent(new byte[SmallLimit + (1024 * 1024) + 1]), "TcxFile", "huge.tcx" },
        };

        using var response = await client.PostAsync(new Uri("/convert", UriKind.Relative), form);

        response.StatusCode.Should().Be(HttpStatusCode.SeeOther);
        response.Headers.Location!.OriginalString.Should().Be("/convert?error=too-large");
        var page = WebUtility.HtmlDecode(await client.GetStringAsync(response.Headers.Location));
        page.Should().Contain(TcxErrorMessages.For(TcxErrorKind.TooLarge, SmallLimit));
        host.CsvFileNames().Should().BeEmpty();
    }

    [Fact]
    public async Task TheTooLargeQueryShowsTheMessageAsync()
    {
        using var client = _host.CreateClient();

        var page = WebUtility.HtmlDecode(await client.GetStringAsync(new Uri("/convert?error=too-large", UriKind.Relative)));

        page.Should().Contain(TcxErrorMessages.For(TcxErrorKind.TooLarge, 52_428_800)).And.Contain("role=\"alert\"");
    }

    // ---- Current CSV (spec 004 US1, contracts/pages.md "Convert") ----

    private const string SeedState = "{\n  \"csvFilename\": \"seed.csv\"\n}\n";

    [Fact]
    public async Task AConversionRecordsItsCsvAsTheCurrentOneAsync()
    {
        var (status, body) = await UploadAsync(_host, "run-two-laps.tcx", File.ReadAllBytes(Fixture.Path("run-two-laps.tcx")), crossSite: false, CancellationToken.None);

        status.Should().Be(HttpStatusCode.OK);
        File.ReadAllText(_host.StateFilePath).Should().Be(
            "{\n  \"csvFilename\": \"2026-11-01_run-two-laps.csv\",\n  \"statesCsvFilename\": \"2026-11-01_run-two-laps.states.csv\"\n}\n");
        body.Should().Contain("role=\"status\"").And.Contain("Current CSV: <strong>2026-11-01_run-two-laps.csv</strong>");
    }

    [Fact]
    public async Task ASeveralActivityConversionRecordsTheFirstCsvAsync()
    {
        const string first = "2026-11-01_071500_multi-activity.csv";
        File.Exists(Fixture.ExpectedPath(first)).Should().BeTrue();

        await UploadAsync(_host, "multi-activity.tcx", File.ReadAllBytes(Fixture.Path("multi-activity.tcx")), crossSite: false, CancellationToken.None);

        File.ReadAllText(_host.StateFilePath).Should().Contain($"\"csvFilename\": \"{first}\"")
            .And.Contain("\"statesCsvFilename\": \"2026-11-01_071500_multi-activity.states.csv\"");
    }

    [Fact]
    public async Task ALaterConversionReplacesTheCurrentCsvAsync()
    {
        await UploadAsync(_host, "run-two-laps.tcx", File.ReadAllBytes(Fixture.Path("run-two-laps.tcx")), crossSite: false, CancellationToken.None);
        var (_, body) = await UploadAsync(_host, "ride-positions.tcx", File.ReadAllBytes(Fixture.Path("ride-positions.tcx")), crossSite: false, CancellationToken.None);

        File.ReadAllText(_host.StateFilePath).Should().Be(
            "{\n  \"csvFilename\": \"2026-10-15_ride-positions.csv\",\n  \"statesCsvFilename\": \"2026-10-15_ride-positions.states.csv\"\n}\n");
        body.Should().Contain("Current CSV: <strong>2026-10-15_ride-positions.csv</strong>");
    }

    [Theory]
    [InlineData("malformed.tcx")]
    [InlineData(null)]
    public async Task AFailedConversionLeavesTheCurrentCsvAloneAsync(string? fixture)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_host.StateFilePath)!);
        File.WriteAllText(_host.StateFilePath, SeedState);

        var (_, body) = await UploadAsync(
            _host, fixture, fixture is null ? null : File.ReadAllBytes(Fixture.Path(fixture)), crossSite: false, CancellationToken.None);

        File.ReadAllText(_host.StateFilePath).Should().Be(SeedState);
        body.Should().NotContain("Current CSV:");
    }

    [Fact]
    public async Task AStateWriteFailureStillSavesAndListsTheCsvAsync()
    {
        // The factory fixes its settings when it is constructed, so the blocking directory comes
        // from a second temp directory owned by the test.
        using var stateDirectory = new TempDirectory();
        using var host = new TcxToolWebFactory(new Dictionary<string, string?> { ["TcxTool:StateFilePath"] = stateDirectory.FullPath });

        var (status, body) = await UploadAsync(host, "run-two-laps.tcx", File.ReadAllBytes(Fixture.Path("run-two-laps.tcx")), crossSite: false, CancellationToken.None);

        status.Should().Be(HttpStatusCode.OK);
        host.CsvFileNames().Should().Equal("2026-11-01_run-two-laps.csv", "2026-11-01_run-two-laps.states.csv");
        body.Should().Contain("2026-11-01_run-two-laps.csv: 10 rows")
            .And.Contain(CsvSelectionMessages.For(CsvSelectionOutcome.WriteFailed));
    }

    public void Dispose()
    {
        _host.Dispose();
    }

    private const long SmallLimit = 1000;

    private static TcxToolWebFactory CreateSmallLimitHost() => new(new Dictionary<string, string?>
    {
        ["TcxTool:MaxUploadBytes"] = SmallLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
    });

    internal static async Task<(HttpStatusCode Status, string Body)> UploadAsync(
        TcxToolWebFactory host, string? fileName, byte[]? content, bool crossSite, CancellationToken cancellationToken)
    {
        using var client = host.CreateClient();
        if (crossSite)
        {
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
        }

        using var form = new MultipartFormDataContent
        {
            { new StringContent("tcx-upload"), "_handler" },
        };
        if (fileName is not null && content is not null)
        {
            var file = new ByteArrayContent(content);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(file, "TcxFile", fileName);
        }

        using var response = await client.PostAsync(new Uri("/convert", UriKind.Relative), form, cancellationToken);
        return (response.StatusCode, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(cancellationToken)));
    }
}
