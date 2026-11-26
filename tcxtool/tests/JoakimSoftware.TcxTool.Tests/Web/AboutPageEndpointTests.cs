using System.Net;

using AngleSharp.Html.Parser;

using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies <c>/about</c> through the real HTTP pipeline: its title, its heading, and that its navbar
/// link is the only active one (spec 004 FR-001, FR-002).
/// </summary>
public sealed class AboutPageEndpointTests : IDisposable
{
    private readonly TcxToolWebFactory _host = new();

    [Fact]
    public async Task AboutIsServedInsideTheLayoutAsync()
    {
        using var client = _host.CreateClient();

        using var response = await client.GetAsync(new Uri("/about", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("<title>About</title>").And.Contain(">About</h1>");
    }

    [Fact]
    public async Task OnlyTheAboutLinkIsActiveAsync()
    {
        using var client = _host.CreateClient();

        var body = await client.GetStringAsync(new Uri("/about", UriKind.Relative));
        using var document = new HtmlParser().ParseDocument(body);

        document.QuerySelectorAll("nav a.nav-link.active").Select(link => link.GetAttribute("href"))
            .Should().Equal("about");
    }

    public void Dispose() => _host.Dispose();
}
