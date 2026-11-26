using System.Text.RegularExpressions;

using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies the Timeline rules in <c>wwwroot/app.css</c> (specs/007-timeline-page/contracts/pages.md
/// "Styles"; spec FR-014, FR-021). The expected values are written here, because <c>specs/</c> is not in
/// the Compose build context.
/// </summary>
public sealed partial class TimelineStylesTests
{
    // Comments are removed first, so a comment above a rule is not read as part of its selector.
    private static readonly string Css = Comment().Replace(
        File.ReadAllText(RepositoryRoot.Combine("src/JoakimSoftware.TcxTool.Web/wwwroot/app.css")), string.Empty);

    public static TheoryData<string> States => ["stopped", "walking", "running", "unknown"];

    [Fact]
    public void TheRootDefinesTheFourStateColors()
    {
        Declarations(":root").Should().Equal(new Dictionary<string, string>
        {
            ["--tcx-state-stopped"] = "#6c757d",
            ["--tcx-state-walking"] = "#ffc107",
            ["--tcx-state-running"] = "#198754",
            ["--tcx-state-unknown"] = "#dee2e6",
        });
    }

    [Theory]
    [InlineData("#6c757d")]
    [InlineData("#ffc107")]
    [InlineData("#198754")]
    [InlineData("#dee2e6")]
    public void EachStateColorIsWrittenOnce(string color)
    {
        Regex.Count(Css, Regex.Escape(color), RegexOptions.IgnoreCase).Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(States))]
    public void EachStateClassFillsAndPaintsWithItsColor(string state)
    {
        Declarations($".state-{state}").Should().Equal(new Dictionary<string, string>
        {
            ["fill"] = $"var(--tcx-state-{state})",
            ["background-color"] = $"var(--tcx-state-{state})",
        });
    }

    [Fact]
    public void TheLinesAreThinAndGray()
    {
        Declarations(".timeline-line").Should().Equal(new Dictionary<string, string>
        {
            ["fill"] = "none",
            ["stroke"] = "#adb5bd",
            ["stroke-width"] = "1px",
        });
    }

    // The chart points take the body text color from Bootstrap, so no new color is written.
    [Fact]
    public void ThePointsUseTheBodyTextColor()
    {
        Declarations(".timeline-point").Should().Equal(new Dictionary<string, string>
        {
            ["fill"] = "var(--bs-body-color)",
        });
    }

    [Fact]
    public void TheSwatchElementAndPopupHaveTheirLayoutRules()
    {
        Declarations(".timeline-swatch").Should().Equal(new Dictionary<string, string>
        {
            ["display"] = "inline-block",
            ["width"] = "0.9em",
            ["height"] = "0.9em",
            ["vertical-align"] = "-0.1em",
        });
        Declarations("tcx-timeline").Should().Equal(new Dictionary<string, string>
        {
            ["display"] = "block",
            ["position"] = "relative",
        });
        Declarations(".timeline-popup").Should().Equal(new Dictionary<string, string>
        {
            ["position"] = "absolute",
            ["z-index"] = "10",
            ["max-width"] = "22rem",
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
