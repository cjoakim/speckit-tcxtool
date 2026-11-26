using Bunit;

using JoakimSoftware.TcxTool.Tests.Support;
using JoakimSoftware.TcxTool.Web.Components.Pages;
using JoakimSoftware.TcxTool.Web.Conversion;

using Microsoft.Extensions.DependencyInjection;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies the convert page markup (contracts/convert-page.md "Markup").
/// </summary>
public class ConvertPageTests : BunitContext
{
    private readonly TempDirectory _root = new();

    public ConvertPageTests()
    {
        BunitTcxToolServices.Add(Services, _root);
        Services.AddScoped<CsvConversionService>();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _root.Dispose();
    }

    [Fact]
    public void HasOneHeading()
    {
        var page = Render<ConvertPage>();

        page.FindAll("h1").Should().ContainSingle().Which.TextContent.Trim().Should().Be("Convert TCX to CSV");
    }

    [Fact]
    public void HasAMultipartPostForm()
    {
        var form = Render<ConvertPage>().Find("form");

        form.GetAttribute("method").Should().Be("post");
        form.GetAttribute("enctype").Should().Be("multipart/form-data");
    }

    [Fact]
    public void HasALabelledTcxFileInput()
    {
        var page = Render<ConvertPage>();

        var input = page.Find("input#tcx-file");
        input.GetAttribute("type").Should().Be("file");
        input.GetAttribute("name").Should().Be("TcxFile");
        input.GetAttribute("accept").Should().Be(".tcx");
        page.Find("label[for=tcx-file]").TextContent.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void StatesTheSizeLimitAndHasAConvertButton()
    {
        var page = Render<ConvertPage>();

        page.Markup.Should().Contain("Maximum file size: 50 MB");
        page.Find("button[type=submit]").TextContent.Trim().Should().Be("Convert");
    }

    [Fact]
    public void UsesBootstrapFormStyles()
    {
        var page = Render<ConvertPage>();

        page.Find("label[for=tcx-file]").ClassList.Should().Contain("form-label");
        page.Find("input#tcx-file").ClassList.Should().Contain("form-control");
        page.Find("button[type=submit]").ClassList.Should().Contain(["btn", "btn-primary"]);
    }
}
