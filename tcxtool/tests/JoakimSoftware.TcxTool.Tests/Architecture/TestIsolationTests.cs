using System.Text.RegularExpressions;

using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Architecture;

/// <summary>
/// Enforces spec 004 FR-020: HTTP tests get their host from <c>Support/TcxToolWebFactory</c>, which
/// points the CSV directory and the state file at a temp directory. A host built any other way runs
/// in Development against the developer's real <c>tcxtool/</c> paths (research R7).
/// </summary>
public partial class TestIsolationTests
{
    private const string TestProject = "tests/JoakimSoftware.TcxTool.Tests";
    private const string ThisFile = "Architecture/TestIsolationTests.cs";

    [Fact]
    public void NoTestConstructsTheWebHostFactoryDirectly()
    {
        var root = RepositoryRoot.Combine(TestProject);

        var offenders = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(relative => !IsExcluded(relative))
            .Where(relative => ConstructsFactory(File.ReadAllText(Path.Combine(root, relative))))
            .Order(StringComparer.Ordinal)
            .ToList();

        offenders.Should().BeEmpty("HTTP tests must use Support/TcxToolWebFactory (spec 004 FR-020)");
    }

    private static bool IsExcluded(string relative) =>
        relative.StartsWith("bin/", StringComparison.Ordinal)
        || relative.StartsWith("obj/", StringComparison.Ordinal)
        || relative.StartsWith("Support/", StringComparison.Ordinal)
        || relative == ThisFile;

    private static bool ConstructsFactory(string source) =>
        ExplicitConstruction().IsMatch(source) || TargetTypedConstruction().IsMatch(source);

    [GeneratedRegex(@"new\s+WebApplicationFactory\s*<\s*Program\s*>")]
    private static partial Regex ExplicitConstruction();

    [GeneratedRegex(@"WebApplicationFactory\s*<\s*Program\s*>\s+\w+\s*=\s*new\s*\(")]
    private static partial Regex TargetTypedConstruction();
}
