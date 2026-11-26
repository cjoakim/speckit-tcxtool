using Bunit;

using JoakimSoftware.TcxTool.Tests.Support;
using JoakimSoftware.TcxTool.Web.Components.Pages;
using JoakimSoftware.TcxTool.Web.Conversion;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies the home page content contract (specs/001-project-bootstrap/contracts/home-page.md) and
/// the Select CSV File card (specs/004-about-and-csv-state/contracts/pages.md "Home").
/// </summary>
public class HomePageTests : BunitContext
{
    private const string ApplicationName = "JoakimSoftware.TcxTool";

    private readonly TempDirectory _root = new();
    private readonly TcxToolOptions _options;

    public HomePageTests()
    {
        _options = BunitTcxToolServices.Add(Services, _root);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _root.Dispose();
    }

    [Fact]
    public void HomePageHasExactlyOneHeadingWithTheApplicationName()
    {
        var page = Render<Home>();

        var headings = page.FindAll("h1");

        headings.Should().ContainSingle();
        headings[0].TextContent.Trim().Should().Be(ApplicationName);
    }

    [Fact]
    public void HomePageDescribesTheToolDirectlyAfterTheHeading()
    {
        var page = Render<Home>();

        page.Find("h1 + p").TextContent.Trim().Should().NotBeEmpty();
    }

    [Fact]
    public void HomePageLinksToTheConvertPage()
    {
        var page = Render<Home>();

        page.FindAll("a").Select(link => link.GetAttribute("href"))
            .Should().Contain(href => href == "convert" || href == "/convert");
    }

    [Fact]
    public void ConvertLinkIsAPrimaryButtonOnACard()
    {
        var page = Render<Home>();

        var link = page.Find(".card a[href=convert]");
        link.ClassList.Should().Contain(["btn", "btn-primary"]);
    }

    // ---- Select CSV File card (spec 004 US2) ----

    [Fact]
    public void SelectCardSitsBesideTheConvertCardAtTheSameSize()
    {
        var page = RenderHome();

        var columns = page.FindAll(".row.g-3.mt-2 > .col-sm-6.col-lg-4");
        columns.Should().HaveCount(2);
        var body = columns[1].QuerySelector(".card.h-100 > .card-body#select-csv");
        body.Should().NotBeNull();
        body!.QuerySelector("h2.card-title.h5")!.TextContent.Trim().Should().Be("Select CSV File");
        body.QuerySelector("p.card-text.text-muted")!.TextContent.Trim().Should().Be("Choose a CSV file converted earlier.");
    }

    [Fact]
    public void WithCsvsAndNoStateTheFormListsThemNewestFirst()
    {
        AddCsv("2025-03-02_a.csv");
        AddCsv("2026-11-01_b.csv");

        var page = RenderHome();

        page.Find("#select-csv form").GetAttribute("method").Should().Be("post");
        page.Find("#select-csv label.form-label[for=csv-file]").TextContent.Trim().Should().Be("CSV file");
        var select = page.Find("#select-csv select.form-select#csv-file");
        select.GetAttribute("name").Should().Be("CsvFilename");
        var options = select.QuerySelectorAll("option");
        options.Select(option => option.GetAttribute("value")).Should().Equal("", "2026-11-01_b.csv", "2025-03-02_a.csv");
        options.Select(option => option.TextContent.Trim()).Should().Equal("Choose a CSV file", "2026-11-01_b.csv", "2025-03-02_a.csv");
        options[0].HasAttribute("disabled").Should().BeTrue();
        options[0].HasAttribute("selected").Should().BeTrue();
        options.Skip(1).Should().OnlyContain(option => !option.HasAttribute("selected"));
        var button = page.Find("#select-csv button[type=submit]");
        button.ClassList.Should().Contain(["btn", "btn-primary"]);
        button.TextContent.Trim().Should().Be("Select");
        page.Find("#current-csv").TextContent.Trim().Should().Be("No CSV selected yet.");
    }

    [Fact]
    public void ThePresentCurrentCsvIsShownAndPreselected()
    {
        AddCsv("2025-03-02_a.csv");
        AddCsv("2026-11-01_b.csv");
        WriteState("{\"csvFilename\": \"2025-03-02_a.csv\"}");

        var page = RenderHome();

        var current = page.Find("#current-csv");
        current.TextContent.Trim().Should().Be("Current CSV: 2025-03-02_a.csv");
        current.QuerySelector("strong")!.TextContent.Should().Be("2025-03-02_a.csv");
        page.FindAll("#csv-file option[selected]").Select(option => option.GetAttribute("value"))
            .Should().Equal("2025-03-02_a.csv");
    }

    [Fact]
    public void AMissingCurrentCsvIsShownAsMissing()
    {
        AddCsv("2026-11-01_b.csv");
        WriteState("{\"csvFilename\": \"2020-01-01_gone.csv\"}");

        var page = RenderHome();

        page.Find("#current-csv").TextContent.Trim().Should().Be("Current CSV: 2020-01-01_gone.csv (file missing)");
        page.Find("#csv-file option").HasAttribute("selected").Should().BeTrue();
    }

    [Fact]
    public void AMalformedStateFileShowsNoSelection()
    {
        AddCsv("2026-11-01_b.csv");
        WriteState("nope");

        var page = RenderHome();

        page.Find("#current-csv").TextContent.Trim().Should().Be("No CSV selected yet.");
    }

    [Fact]
    public void WithNoCsvsThereAreNoFormControlsAndALinkToConvert()
    {
        var page = RenderHome();

        // An empty form element stays so a late POST still renders the page (T037); it shows nothing.
        page.FindAll("#select-csv select, #select-csv button, #select-csv input:not([type=hidden])").Should().BeEmpty();
        page.Find("#select-csv").TextContent.Should().Contain("No CSV files yet.");
        var link = page.Find("#select-csv a[href=convert]");
        link.TextContent.Trim().Should().Be("Convert a TCX file");
    }

    private IRenderedComponent<Home> RenderHome()
    {
        var page = Render<Home>();
        page.WaitForElement("#current-csv");
        return page;
    }

    private void AddCsv(string name) => File.WriteAllText(Path.Combine(_options.CsvOutputDirectory, name), "header\n");

    private void WriteState(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_options.StateFilePath)!);
        File.WriteAllText(_options.StateFilePath, content);
    }
}
