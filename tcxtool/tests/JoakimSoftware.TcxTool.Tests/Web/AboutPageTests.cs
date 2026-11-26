using System.Text.RegularExpressions;

using AngleSharp.Dom;

using Bunit;

using JoakimSoftware.TcxTool.Web.Components.Pages;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies the About page content (spec 004 FR-001, FR-003; contracts/pages.md "About").
/// </summary>
public partial class AboutPageTests : BunitContext
{
    private const string Intro =
        "The TcxTool application was developed by Chris Joakim as an experiment of using the Software Factory "
        + "approach to develop software in an entirely hands-off manner using Claude Code and Spec-Kit.";

    [Fact]
    public void HasOneAboutHeading()
    {
        var heading = Render<AboutPage>().FindAll("h1").Should().ContainSingle().Subject;

        heading.TextContent.Trim().Should().Be("About");
        heading.ClassList.Should().Contain(["h3", "mb-3"]);
    }

    [Fact]
    public void IntroMatchesTheSpecWithItsBoldNames()
    {
        var intro = Render<AboutPage>().Find("h1 + p");

        Collapse(intro.TextContent).Should().Be(Intro);
        intro.QuerySelectorAll("strong").Select(strong => strong.TextContent)
            .Should().Equal("TcxTool", "Chris Joakim", "Software Factory", "Claude Code", "Spec-Kit");
    }

    [Fact]
    public void SubheadingsAreInOrder()
    {
        Render<AboutPage>().FindAll("h2").Select(heading => heading.TextContent.Trim()).Should().Equal(
            "Spec-Kit Process", "Spec-Kit Project Startup", "Spec-Kit Feature/Spec Development Loop", "Links");
    }

    [Fact]
    public void StartupListHasTheConstitutionCommand()
    {
        ListItemsAfter(Render<AboutPage>(), "Spec-Kit Project Startup")
            .Should().Equal("/speckit-constitution - Establish project principles");
    }

    [Fact]
    public void LoopListHasTheFiveCommandsInOrder()
    {
        ListItemsAfter(Render<AboutPage>(), "Spec-Kit Feature/Spec Development Loop").Should().Equal(
            "/speckit-specify - Create baseline specification",
            "/speckit-plan - Create implementation plan",
            "/speckit-tasks - Generate actionable tasks",
            "/speckit-implement - Execute implementation",
            "/speckit-converge - Assess the codebase and append remaining work as tasks");
    }

    [Fact]
    public void LinksListHasTheFiveLinks()
    {
        var links = ListAfter(Render<AboutPage>(), "Links").QuerySelectorAll("li > a")
            .Select(link => (Text: link.TextContent.Trim(), Href: link.GetAttribute("href")));

        links.Should().Equal(
            ("speckit.org", "https://speckit.org"),
            ("spec-kit @ GitHub", "https://github.com/github/spec-kit"),
            ("Spec-Kit Quickstart", "https://github.github.io/spec-kit/quickstart.html"),
            ("specify-cli @ PyPI", "https://pypi.org/project/specify-cli/"),
            ("GitHub repo for this project", "https://github.com/cjoakim/cj-speckit-explore"));
    }

    [Fact]
    public void LoadsNothingFromTheNetwork()
    {
        Render<AboutPage>().FindAll("img, script, link, iframe").Should().BeEmpty();
    }

    private static IElement ListAfter(IRenderedComponent<AboutPage> page, string heading)
    {
        var h2 = page.FindAll("h2").Single(element => element.TextContent.Trim() == heading);
        var list = h2.NextElementSibling;
        list.Should().NotBeNull();
        list!.TagName.Should().Be("UL");
        return list;
    }

    private static IEnumerable<string> ListItemsAfter(IRenderedComponent<AboutPage> page, string heading) =>
        ListAfter(page, heading).QuerySelectorAll("li").Select(item => Collapse(item.TextContent));

    private static string Collapse(string text) => Whitespace().Replace(text, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
