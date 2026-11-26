using JoakimSoftware.TcxTool.Parser.Conversion;
using JoakimSoftware.TcxTool.Parser.Csv;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Verifies how an uploaded file name becomes the ActivityId (spec FR-007, data-model.md
/// CSV row → ActivityId).
/// </summary>
public class ActivityIdNameTests
{
    [Theory]
    [InlineData("24932322230.tcx", "24932322230")]
    [InlineData("Morning Run (2).tcx", "Morning_Run__2_")]
    [InlineData("run.TCX", "run")]
    [InlineData("/a/b/run.tcx", "run")]
    [InlineData("C:\\a\\b\\run.tcx", "run")]
    [InlineData("run-2026_11.tcx", "run-2026_11")]
    [InlineData("Läufe.tcx", "L_ufe")]
    public void SanitizesTheFileName(string uploaded, string expected)
    {
        ActivityIdName.TryFromFileName(uploaded, out var activityId, out var error).Should().BeTrue();

        activityId.Should().Be(expected);
        error.Should().BeNull();
    }

    [Theory]
    [InlineData(".tcx", TcxErrorKind.EmptyFileName)]
    [InlineData("/a/b/.TCX", TcxErrorKind.EmptyFileName)]
    [InlineData("notes.txt", TcxErrorKind.NotTcxFileName)]
    [InlineData("run.tcx.xml", TcxErrorKind.NotTcxFileName)]
    [InlineData("", TcxErrorKind.NotTcxFileName)]
    public void RejectsNamesThatCannotBeUsed(string uploaded, TcxErrorKind expected)
    {
        ActivityIdName.TryFromFileName(uploaded, out _, out var error).Should().BeFalse();

        error!.Kind.Should().Be(expected);
        error.Message.Should().Be(TcxErrorMessages.For(expected));
    }
}
