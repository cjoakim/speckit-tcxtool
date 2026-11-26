using System.Net;
using System.Text.RegularExpressions;

using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies that every page links the vendored Bootstrap CSS and JS, and that the app serves them
/// itself, so the UI needs no network or CDN.
/// </summary>
public sealed partial class BootstrapAssetsTests : IDisposable
{
    private readonly TcxToolWebFactory _factory = new();

    [Theory]
    [InlineData("/")]
    [InlineData("/convert")]
    public async Task PageLinksTheLocalBootstrapStylesheetAsync(string route)
    {
        using var client = _factory.CreateClient();

        var body = await client.GetStringAsync(new Uri(route, UriKind.Relative));

        StylesheetHref().Match(body).Success.Should().BeTrue("the page should link a local bootstrap.min.css");
    }

    [Fact]
    public async Task StylesheetIsServedLocallyAsync()
    {
        await AssetIsServedAsync(StylesheetHref(), "text/css");
    }

    [Fact]
    public async Task ScriptBundleIsServedLocallyAsync()
    {
        await AssetIsServedAsync(ScriptSrc(), "text/javascript");
    }

    public void Dispose() => _factory.Dispose();

    private async Task AssetIsServedAsync(Regex reference, string mediaType)
    {
        using var client = _factory.CreateClient();
        var page = await client.GetStringAsync(new Uri("/", UriKind.Relative));
        var match = reference.Match(page);
        match.Success.Should().BeTrue();

        using var response = await client.GetAsync(new Uri("/" + match.Groups["path"].Value, UriKind.Relative));
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be(mediaType);
        content.Should().Contain("Bootstrap");
    }

    [GeneratedRegex("<link[^>]+href=\"/?(?<path>lib/bootstrap/css/bootstrap\\.min(\\.[a-z0-9]+)?\\.css)\"")]
    private static partial Regex StylesheetHref();

    [GeneratedRegex("<script[^>]+src=\"/?(?<path>lib/bootstrap/js/bootstrap\\.bundle\\.min(\\.[a-z0-9]+)?\\.js)\"")]
    private static partial Regex ScriptSrc();
}
