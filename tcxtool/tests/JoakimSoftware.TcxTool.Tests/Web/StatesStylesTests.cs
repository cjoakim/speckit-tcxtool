using System.Text.RegularExpressions;

using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies the States page rules in <c>wwwroot/app.css</c> (specs/006-activity-states/contracts/pages.md
/// "Styles"). The expected values are written here, because <c>specs/</c> is not in the Compose build
/// context.
/// </summary>
public sealed partial class StatesStylesTests
{
    // Comments are removed first, so a comment above a rule is not read as part of its selector.
    private static readonly string Css = Comment().Replace(
        File.ReadAllText(RepositoryRoot.Combine("src/JoakimSoftware.TcxTool.Web/wwwroot/app.css")), string.Empty);

    [Fact]
    public void TheScrollBoxIsCappedAndScrollsVertically()
    {
        Declarations(".states-table-scroll").Should().Equal(new Dictionary<string, string>
        {
            ["max-height"] = "75vh",
            ["overflow-y"] = "auto",
        });
    }

    [Fact]
    public void TheHeadingCellsStickToTheTopOfTheScrollBox()
    {
        Declarations(".states-table-scroll thead th").Should().Equal(new Dictionary<string, string>
        {
            ["position"] = "sticky",
            ["top"] = "0",
            ["z-index"] = "1",
        });
    }

    /// <summary>The declarations of the one rule whose selector is exactly <paramref name="selector"/>.</summary>
    private static Dictionary<string, string> Declarations(string selector)
    {
        var rules = RuleBlock().Matches(Css).Where(match => match.Groups["selector"].Value.Trim() == selector).ToList();
        rules.Should().ContainSingle($"app.css has exactly one rule for {selector}");
        return rules[0].Groups["body"].Value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(declaration => declaration.Split(':', 2, StringSplitOptions.TrimEntries))
            .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.Ordinal);
    }

    [GeneratedRegex(@"(?<selector>[^{}]+)\{(?<body>[^{}]*)\}")]
    private static partial Regex RuleBlock();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex Comment();
}
