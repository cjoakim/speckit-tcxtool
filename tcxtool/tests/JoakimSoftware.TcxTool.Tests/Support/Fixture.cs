using System.Text;

namespace JoakimSoftware.TcxTool.Tests.Support;

/// <summary>
/// Locates the checked-in <c>.tcx</c> fixtures and their golden CSV files under
/// <c>tests/JoakimSoftware.TcxTool.Tests/Fixtures/</c>. Works on the host and in the test container.
/// </summary>
public static class Fixture
{
    private const string FixturesDirectory = "tests/JoakimSoftware.TcxTool.Tests/Fixtures";

    /// <summary>Absolute path of the fixture <paramref name="name"/>, such as <c>run-two-laps.tcx</c>.</summary>
    public static string Path(string name) => RepositoryRoot.Combine($"{FixturesDirectory}/{name}");

    /// <summary>Absolute path of the golden file <paramref name="name"/> in <c>Fixtures/Expected/</c>.</summary>
    public static string ExpectedPath(string name) => RepositoryRoot.Combine($"{FixturesDirectory}/Expected/{name}");

    /// <summary>Opens the fixture <paramref name="name"/> for reading.</summary>
    public static FileStream OpenRead(string name) => File.OpenRead(Path(name));

    /// <summary>Reads the golden file <paramref name="name"/> as UTF-8 text.</summary>
    public static string ReadExpected(string name) => File.ReadAllText(ExpectedPath(name), Encoding.UTF8);
}
