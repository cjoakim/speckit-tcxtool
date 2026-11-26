using Bunit;

using JoakimSoftware.TcxTool.Parser.Conversion;
using JoakimSoftware.TcxTool.Web.Components.Conversion;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies the result region of the convert page (feature 002 contracts/convert-page.md "Result region";
/// feature 006 contracts/pages.md "Convert page result").
/// </summary>
public class ConversionResultViewTests : BunitContext
{
    [Fact]
    public void RendersNothingWithoutAResult()
    {
        var view = Render<ConversionResultView>(parameters => parameters.Add(p => p.Result, null));

        view.Markup.Trim().Should().BeEmpty();
    }

    [Fact]
    public void ListsEachSavedFileWithItsRowCountAndThenItsStatesCsv()
    {
        var result = ConversionResult.Success(
        [
            new SavedCsv("2026-11-01_071500_run.csv", 3, "2026-11-01_071500_run.states.csv", 3),
            new SavedCsv("2026-11-01_183005_run.csv", 19914, "2026-11-01_183005_run.states.csv", 541),
        ]);

        var view = Render<ConversionResultView>(parameters => parameters.Add(p => p.Result, result));

        var items = view.FindAll("section[role=status] li");
        items.Select(item => item.TextContent.Trim())
            .Should().Equal(
                "2026-11-01_071500_run.csv: 3 rows",
                "2026-11-01_071500_run.states.csv: 3 StateSets",
                "2026-11-01_183005_run.csv: 19914 rows",
                "2026-11-01_183005_run.states.csv: 541 StateSets");
        view.FindAll("[role=alert]").Should().BeEmpty();
    }

    [Theory]
    [InlineData(TcxErrorKind.Malformed, null, null)]
    [InlineData(TcxErrorKind.InvalidValue, "HeartRateBpm", 3)]
    [InlineData(TcxErrorKind.NoFile, null, null)]
    public void AFailureRendersOnlyTheErrorMessage(TcxErrorKind kind, string? element, int? trackpointNumber)
    {
        var error = new TcxError(kind, TcxErrorMessages.For(kind, element: element, trackpointNumber: trackpointNumber), element, trackpointNumber);

        var view = Render<ConversionResultView>(parameters => parameters.Add(p => p.Result, ConversionResult.Failure(error)));

        view.Find("section[role=alert]").TextContent.Trim().Should().Be(error.Message);
        view.FindAll("li").Should().BeEmpty();
        view.FindAll("[role=status]").Should().BeEmpty();
    }

    [Fact]
    public void SaysWhenAFileHasNoTrackpoints()
    {
        var result = ConversionResult.Success([new SavedCsv("2026-11-01_empty.csv", 0, "2026-11-01_empty.states.csv", 0)]);

        var view = Render<ConversionResultView>(parameters => parameters.Add(p => p.Result, result));

        view.FindAll("section[role=status] li").Select(item => item.TextContent.Trim())
            .Should().Equal("2026-11-01_empty.csv: contains no trackpoints", "2026-11-01_empty.states.csv: contains no StateSets");
    }

    [Fact]
    public void OneStateSetIsSingular()
    {
        var result = ConversionResult.Success([new SavedCsv("2026-11-01_run-two-laps.csv", 10, "2026-11-01_run-two-laps.states.csv", 1)]);

        var view = Render<ConversionResultView>(parameters => parameters.Add(p => p.Result, result));

        view.FindAll("section[role=status] li")[1].TextContent.Trim().Should().Be("2026-11-01_run-two-laps.states.csv: 1 StateSet");
    }

    [Fact]
    public void ASuccessIsABootstrapSuccessAlert()
    {
        var result = ConversionResult.Success([new SavedCsv("2026-11-01_run.csv", 3, "2026-11-01_run.states.csv", 1)]);

        var view = Render<ConversionResultView>(parameters => parameters.Add(p => p.Result, result));

        view.Find("section[role=status]").ClassList.Should().Contain(["alert", "alert-success"]);
    }

    [Fact]
    public void AFailureIsABootstrapDangerAlert()
    {
        var error = new TcxError(TcxErrorKind.NoFile, TcxErrorMessages.For(TcxErrorKind.NoFile), null, null);

        var view = Render<ConversionResultView>(parameters => parameters.Add(p => p.Result, ConversionResult.Failure(error)));

        view.Find("section[role=alert]").ClassList.Should().Contain(["alert", "alert-danger"]);
    }
}
