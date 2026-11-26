using System.ComponentModel.DataAnnotations;

using JoakimSoftware.TcxTool.Web.Conversion;

using Microsoft.Extensions.Hosting.Internal;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies <c>TcxToolOptions.StateFilePath</c> and <see cref="TcxToolPaths"/>
/// (spec 004 FR-005; data-model.md TcxToolOptions.StateFilePath and TcxToolPaths).
/// </summary>
public class TcxToolPathsTests
{
    private static readonly string ContentRoot = Path.Combine(Path.GetTempPath(), "tcxtool-content-root");

    [Fact]
    public void StateFilePathDefaultsToTheFileNameInTheContentRoot()
    {
        new TcxToolOptions().StateFilePath.Should().Be("tcxtool-state.json");
    }

    [Fact]
    public void StateFilePathIsRequired()
    {
        var options = new TcxToolOptions { StateFilePath = string.Empty };

        Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true)
            .Should().BeFalse();
    }

    [Fact]
    public void RelativeStateFileResolvesAgainstTheContentRoot()
    {
        var options = new TcxToolOptions { StateFilePath = "../state/tcxtool-state.json" };

        TcxToolPaths.StateFile(options, Environment())
            .Should().Be(Path.GetFullPath(Path.Combine(ContentRoot, "../state/tcxtool-state.json")));
    }

    [Fact]
    public void AbsoluteStateFileIsKept()
    {
        var absolute = Path.Combine(Path.GetTempPath(), "elsewhere", "tcxtool-state.json");

        TcxToolPaths.StateFile(new TcxToolOptions { StateFilePath = absolute }, Environment()).Should().Be(absolute);
    }

    [Fact]
    public void RelativeCsvDirectoryResolvesAgainstTheContentRoot()
    {
        TcxToolPaths.CsvDirectory(new TcxToolOptions { CsvOutputDirectory = "data/csv" }, Environment())
            .Should().Be(Path.GetFullPath(Path.Combine(ContentRoot, "data/csv")));
    }

    [Fact]
    public void AbsoluteCsvDirectoryIsKept()
    {
        var absolute = Path.Combine(Path.GetTempPath(), "csv-out");

        TcxToolPaths.CsvDirectory(new TcxToolOptions { CsvOutputDirectory = absolute }, Environment()).Should().Be(absolute);
    }

    private static HostingEnvironment Environment() => new() { ContentRootPath = ContentRoot };
}
