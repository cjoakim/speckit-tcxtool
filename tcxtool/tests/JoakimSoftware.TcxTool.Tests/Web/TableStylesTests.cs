using System.Text.RegularExpressions;

using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies the Table page rules in <c>wwwroot/app.css</c> (specs/005-table-page/contracts/pages.md
/// "Styles"). The expected values are written here, because <c>specs/</c> is not in the Compose build
/// context.
/// </summary>
public sealed partial class TableStylesTests
{
    // Comments are removed first, so a comment above a rule is not read as part of its selector.
    private static readonly string Css = Comment().Replace(
        File.ReadAllText(RepositoryRoot.Combine("src/JoakimSoftware.TcxTool.Web/wwwroot/app.css")), string.Empty);

    [Fact]
    public void TheScrollBoxIsCappedAndScrollsVertically()
    {
        var declarations = Declarations(".trackpoint-table-scroll");

        declarations.Should().Contain("max-height", "75vh").And.Contain("overflow-y", "auto");
    }

    [Fact]
    public void TheHeadingCellsStickToTheTopOfTheScrollBox()
    {
        var declarations = Declarations(".trackpoint-table-scroll thead th");

        declarations.Should().Contain("position", "sticky").And.Contain("top", "0").And.Contain("z-index", "1");
    }

    public static TheoryData<string, string, string> Bands => new()
    {
        { ".hr-band-1", "#cff4fc", "#000" },
        { ".hr-band-2", "#fff3cd", "#000" },
        { ".hr-band-3", "#ffe5d0", "#000" },
        { ".hr-band-4", "#fd7e14", "#000" },
        { ".hr-band-5", "#f8d7da", "#000" },
        { ".hr-band-6", "#b02a37", "#fff" },
        { ".cadence-band-1", "#cff4fc", "#000" },
        { ".cadence-band-2", "#fff3cd", "#000" },
        { ".cadence-band-3", "#d1e7dd", "#000" },
        { ".cadence-band-4", "#146c43", "#fff" },
    };

    [Theory]
    [MemberData(nameof(Bands))]
    public void EachBandSetsExactlyTheTwoTableVariables(string selector, string background, string text)
    {
        var declarations = Declarations(selector);

        declarations.Should().Equal(new Dictionary<string, string>
        {
            ["--bs-table-bg"] = background,
            ["--bs-table-color"] = text,
        });
    }

    // WCAG 2.x contrast; 4.5:1 is the AA minimum for normal text (spec FR-017).
    [Theory]
    [MemberData(nameof(Bands))]
    public void EachBandsTextIsReadableOnItsBackground(string selector, string background, string text)
    {
        var declarations = Declarations(selector);

        var lighter = Math.Max(Luminance(declarations["--bs-table-bg"]), Luminance(declarations["--bs-table-color"]));
        var darker = Math.Min(Luminance(declarations["--bs-table-bg"]), Luminance(declarations["--bs-table-color"]));
        ((lighter + 0.05) / (darker + 0.05)).Should().BeGreaterThanOrEqualTo(4.5, $"{selector} pairs {text} on {background}");
    }

    private static double Luminance(string hex)
    {
        var digits = hex.TrimStart('#');
        if (digits.Length == 3)
        {
            digits = string.Concat(digits.Select(digit => $"{digit}{digit}"));
        }

        var channels = Enumerable.Range(0, 3)
            .Select(i => Convert.ToInt32(digits.Substring(i * 2, 2), 16) / 255.0)
            .Select(c => c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4))
            .ToArray();
        return (0.2126 * channels[0]) + (0.7152 * channels[1]) + (0.0722 * channels[2]);
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
