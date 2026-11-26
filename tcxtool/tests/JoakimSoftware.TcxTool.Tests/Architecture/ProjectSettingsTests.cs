using System.Xml.Linq;

using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Architecture;

/// <summary>
/// Enforces constitution Principle II: the mandatory settings are declared in every project file,
/// and the solution holds exactly the three projects allowed by Technology Constraints.
/// </summary>
public class ProjectSettingsTests
{
    private static readonly string[] ExpectedProjects =
    [
        "src/JoakimSoftware.TcxTool.Parser/JoakimSoftware.TcxTool.Parser.csproj",
        "src/JoakimSoftware.TcxTool.Web/JoakimSoftware.TcxTool.Web.csproj",
        "tests/JoakimSoftware.TcxTool.Tests/JoakimSoftware.TcxTool.Tests.csproj",
    ];

    public static TheoryData<string> SolutionProjects => [.. ReadSolutionProjects()];

    [Fact]
    public void SolutionListsExactlyTheThreeAllowedProjects()
    {
        ReadSolutionProjects().Should().BeEquivalentTo(ExpectedProjects);
    }

    [Theory]
    [MemberData(nameof(SolutionProjects))]
    public void ProjectDeclaresMandatorySettings(string projectPath)
    {
        var project = SecureXml.Load(RepositoryRoot.Combine(projectPath));

        ReadProperty(project, "Nullable").Should().Be("enable", "Principle II requires it in {0}", projectPath);
        ReadProperty(project, "ImplicitUsings").Should().Be("enable", "Principle II requires it in {0}", projectPath);
        ReadProperty(project, "TreatWarningsAsErrors").Should().Be("true", "Principle II requires it in {0}", projectPath);
        ReadProperty(project, "AnalysisLevel").Should().Be("latest", "Principle II requires it in {0}", projectPath);
    }

    private static List<string> ReadSolutionProjects()
    {
        var solution = SecureXml.Load(RepositoryRoot.Combine(RepositoryRoot.SolutionFileName));
        return solution
            .Descendants("Project")
            .Select(element => (string?)element.Attribute("Path") ?? string.Empty)
            .ToList();
    }

    private static string? ReadProperty(XDocument project, string name) =>
        project.Descendants(name).Select(element => element.Value.Trim()).LastOrDefault();
}
