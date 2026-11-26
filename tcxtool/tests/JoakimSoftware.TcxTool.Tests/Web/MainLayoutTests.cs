using AngleSharp.Html.Parser;

using Bunit;

using JoakimSoftware.TcxTool.Tests.Support;
using JoakimSoftware.TcxTool.Web.Components.Layout;

using Microsoft.AspNetCore.Components;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies the Bootstrap page shell: a dark navbar, a container for the page body, and a footer,
/// matching the PLog look and feel.
/// </summary>
public class MainLayoutTests : BunitContext
{
    private IRenderedComponent<MainLayout> RenderLayout() =>
        Render<MainLayout>(parameters => parameters.Add(
            layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<p id=\"page-body\">body</p>"))));

    [Fact]
    public void HasADarkNavbarWithTheBrandLinkingHome()
    {
        var layout = RenderLayout();

        layout.Find("nav").ClassList.Should().Contain(["navbar", "navbar-dark", "bg-dark"]);
        var brand = layout.Find("nav a.navbar-brand");
        brand.TextContent.Trim().Should().Be("TcxTool");
        brand.GetAttribute("href").Should().BeOneOf("", "/");
    }

    [Fact]
    public void NavbarLinksOnlyToPagesThatExist()
    {
        var layout = RenderLayout();

        layout.FindAll("nav a.nav-link")
            .Select(link => (Text: link.TextContent.Trim(), Href: link.GetAttribute("href")))
            .Should().Equal(
                ("Home", ""), ("Convert", "convert"), ("Table", "table"), ("States", "states"),
                ("Timeline", "timeline"), ("About", "about"));
    }

    // Feature 006 contracts/pages.md "Navbar": the States link is the active one on /states.
    [Fact]
    public async Task OnlyTheStatesLinkIsActiveOnTheStatesPageAsync()
    {
        using var host = new TcxToolWebFactory();
        using var client = host.CreateClient();

        var body = await client.GetStringAsync(new Uri("/states", UriKind.Relative));
        using var document = new HtmlParser().ParseDocument(body);

        document.QuerySelectorAll("nav a.nav-link.active").Select(link => link.GetAttribute("href"))
            .Should().Equal("states");
    }

    [Fact]
    public void RendersThePageBodyInsideAContainer()
    {
        var layout = RenderLayout();

        layout.Find("main.container #page-body").TextContent.Should().Be("body");
    }

    [Fact]
    public void HasAFooter()
    {
        var layout = RenderLayout();

        layout.Find("footer").TextContent.Should().Contain("Chris Joakim");
    }
}
