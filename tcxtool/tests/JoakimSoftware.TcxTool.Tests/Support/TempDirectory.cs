namespace JoakimSoftware.TcxTool.Tests.Support;

/// <summary>
/// A unique directory under the system temp path, deleted with everything in it on dispose.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    /// <summary>Creates the directory.</summary>
    public TempDirectory()
    {
        FullPath = Path.Combine(Path.GetTempPath(), $"tcxtool-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(FullPath);
    }

    /// <summary>Absolute path of the directory.</summary>
    public string FullPath { get; }

    /// <summary>Names of the files directly in the directory, sorted.</summary>
    public IReadOnlyList<string> FileNames() =>
        [.. Directory.EnumerateFiles(FullPath).Select(file => Path.GetFileName(file)).Order(StringComparer.Ordinal)];

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(FullPath))
        {
            Directory.Delete(FullPath, recursive: true);
        }
    }
}
