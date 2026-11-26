using System.Reflection;
using System.Xml.Linq;

using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Architecture;

/// <summary>
/// Enforces constitution Principle III: the parser library has no reference to ASP.NET Core,
/// Blazor, or the web project. Checks both the project file (catches unused references) and the
/// compiled assembly.
/// </summary>
public class ParserIndependenceTests
{
    private const string ParserProjectPath =
        "src/JoakimSoftware.TcxTool.Parser/JoakimSoftware.TcxTool.Parser.csproj";

    private const string ParserAssemblyName = "JoakimSoftware.TcxTool.Parser";
    private const string WebAssemblyName = "JoakimSoftware.TcxTool.Web";
    private const string AspNetCorePrefix = "Microsoft.AspNetCore";

    [Fact]
    public void ParserProjectUsesThePlainSdk()
    {
        ((string?)LoadParserProject().Root?.Attribute("Sdk")).Should().Be("Microsoft.NET.Sdk");
    }

    [Fact]
    public void ParserProjectHasNoFrameworkReference()
    {
        LoadParserProject().Descendants("FrameworkReference").Should().BeEmpty();
    }

    [Fact]
    public void ParserProjectDoesNotReferenceTheWebProject()
    {
        IncludesOf(LoadParserProject(), "ProjectReference")
            .Should().NotContain(include => include.Contains(WebAssemblyName, StringComparison.Ordinal));
    }

    [Fact]
    public void ParserProjectHasNoAspNetCorePackages()
    {
        IncludesOf(LoadParserProject(), "PackageReference")
            .Should().NotContain(include => include.StartsWith(AspNetCorePrefix, StringComparison.Ordinal));
    }

    [Fact]
    public void ParserAssemblyReferencesNoWebAssemblies()
    {
        var referenced = Assembly.Load(ParserAssemblyName)
            .GetReferencedAssemblies()
            .Select(name => name.Name ?? string.Empty);

        referenced.Should().NotContain(name =>
            name.StartsWith(AspNetCorePrefix, StringComparison.Ordinal) || name == WebAssemblyName);
    }

    private static XDocument LoadParserProject() => SecureXml.Load(RepositoryRoot.Combine(ParserProjectPath));

    private static IEnumerable<string> IncludesOf(XDocument project, string elementName) =>
        project.Descendants(elementName).Select(element => (string?)element.Attribute("Include") ?? string.Empty);
}
