namespace JoakimSoftware.TcxTool.Tests.Support;

/// <summary>
/// Locates the solution root (the directory holding <see cref="SolutionFileName"/>) by walking up
/// from the test output directory. Works on the host and inside the Compose test container.
/// </summary>
public static class RepositoryRoot
{
    /// <summary>The solution file that marks the repository root.</summary>
    public const string SolutionFileName = "JoakimSoftware.TcxTool.slnx";

    /// <summary>Absolute path of the directory that holds the solution file.</summary>
    public static string FullPath { get; } = Locate();

    /// <summary>Combines <see cref="FullPath"/> with a solution-relative path such as <c>src/x/x.csproj</c>.</summary>
    public static string Combine(string relativePath) => Path.Combine(FullPath, relativePath);

    private static string Locate()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not find {SolutionFileName} in {AppContext.BaseDirectory} or any parent directory.");
    }
}
