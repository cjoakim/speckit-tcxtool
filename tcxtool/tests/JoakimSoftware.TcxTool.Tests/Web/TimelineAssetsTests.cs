using System.Net;
using System.Text.RegularExpressions;

using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies the Timeline's browser assets (specs/007-timeline-page/research.md R8, R9, R12; spec
/// FR-023): the vendored D3 bundle and <c>js/timeline.js</c> are served from the app, and the module holds no
/// network URL and no color. The expected values are written here, because <c>specs/</c> is not in the
/// Compose build context.
/// </summary>
public sealed partial class TimelineAssetsTests : IDisposable
{
    private const string WebRoot = "src/JoakimSoftware.TcxTool.Web/wwwroot/";

    private readonly TcxToolWebFactory _factory = new();

    private static string TimelineScript => File.ReadAllText(RepositoryRoot.Combine(WebRoot + "js/timeline.js"));

    [Fact]
    public async Task TheModuleTheLayoutLoadsIsServedAsJavaScriptAsync()
    {
        using var client = _factory.CreateClient();
        var page = await client.GetStringAsync(new Uri("/", UriKind.Relative));
        var match = ModuleSrc().Match(page);
        match.Success.Should().BeTrue("App.razor should load js/timeline.js as a module");

        using var response = await client.GetAsync(new Uri("/" + match.Groups["path"].Value, UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/javascript");
        (await response.Content.ReadAsStringAsync()).Should().Contain("customElements.define(\"tcx-timeline\"");
    }

    // The package's d3.min.mjs re-exports bare d3-* names that a browser can't resolve, so the
    // module loads the self-contained UMD bundle by its plain path (tasks.md T031 done note).
    [Fact]
    public async Task TheVendoredD3BundleIsServedAtThePathTheModuleLoadsAsync()
    {
        TimelineScript.Should().Contain("new URL(\"../lib/d3/d3.min.js\", import.meta.url)");
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/lib/d3/d3.min.js", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/javascript");
    }

    // FR-022: every page gets one shared script reference, the module. D3 is fetched only by it.
    // The import map <ImportMap /> writes names every JS asset, but loads nothing, so only a
    // <script> or <link> that would fetch D3 counts.
    [Theory]
    [InlineData("/")]
    [InlineData("/convert")]
    [InlineData("/timeline")]
    public async Task NoPageReferencesD3DirectlyAsync(string route)
    {
        using var client = _factory.CreateClient();

        var page = await client.GetStringAsync(new Uri(route, UriKind.Relative));

        D3Fetch().IsMatch(page).Should().BeFalse("only js/timeline.js loads D3");
    }

    [Fact]
    public void D3IsVersion792WithItsLicenceAndProvenance()
    {
        File.ReadAllText(RepositoryRoot.Combine(WebRoot + "lib/d3/d3.min.js")).Should().StartWith("// https://d3js.org v7.9.2 ");
        File.ReadAllText(RepositoryRoot.Combine(WebRoot + "lib/d3/LICENSE")).Should().Contain("Mike Bostock");
        File.ReadAllText(RepositoryRoot.Combine(WebRoot + "lib/d3/README.md")).Should().Contain("7.9.2")
            .And.Contain("d3.min.js").And.Contain("sha512-ZKb/mONPVKzaO5UlcWcwN8bS4ezvQaYu+bsou+XgNABdGC800kwH651ADTbbTxMPHW13UlvbQafdmJQDk1w7Mg==");
    }

    [Fact]
    public void TheModuleDefinesTheElementAndColorsSegmentsByStateClass()
    {
        TimelineScript.Should().Contain("customElements.define(\"tcx-timeline\"").And.Contain("state-");
    }

    // Only checks that the module is wired to contracts/timeline-data.md "Charts"; the drawing is
    // checked by hand (quickstart scenario 5).
    [Fact]
    public void TheModuleDrawsTheChartsWithALeftAxisAndALine()
    {
        TimelineScript.Should().Contain("axisLeft").And.Contain("d3.line(").And.Contain("timeline-line").And.Contain("timeline-chart");
    }

    // Guard (FR-023): the page needs no network, so the module names no URL.
    [Fact]
    public void TheModuleHasNoNetworkUrl()
    {
        TimelineScript.Should().NotContain("http://").And.NotContain("https://");
    }

    // Guard (FR-014, research.md R12): the state colors live in app.css only.
    [Fact]
    public void TheModuleHasNoColorLiteral()
    {
        HexColor().IsMatch(TimelineScript).Should().BeFalse("colors come from the state classes in app.css");
        TimelineScript.Should().NotContain("rgb(");
    }

    public void Dispose() => _factory.Dispose();

    [GeneratedRegex("<script type=\"module\" src=\"/?(?<path>js/timeline(\\.[a-z0-9]+)?\\.js)\"></script>")]
    private static partial Regex ModuleSrc();

    [GeneratedRegex("<(script|link)[^>]+lib/d3/")]
    private static partial Regex D3Fetch();

    [GeneratedRegex(@"#[0-9a-fA-F]{3}(?:[0-9a-fA-F]{3})?\b")]
    private static partial Regex HexColor();
}
