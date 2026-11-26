using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace JoakimSoftware.TcxTool.Tests.Support;

/// <summary>
/// The only way HTTP tests get a host (spec 004 FR-020, research R7). A bare
/// <c>WebApplicationFactory&lt;Program&gt;</c> runs in Development, where the CSV directory and the
/// state file resolve to the developer's real <c>tcxtool/</c> paths. This factory points both at a
/// temp directory it owns, and deletes it on dispose. <c>Architecture/TestIsolationTests</c> enforces
/// its use.
/// </summary>
public sealed class TcxToolWebFactory : IDisposable
{
    private readonly TempDirectory _root = new();

    /// <summary>Creates the factory.</summary>
    /// <param name="settings">Extra configuration, applied after (and so overriding) the temp paths.</param>
    public TcxToolWebFactory(IDictionary<string, string?>? settings = null)
    {
        CsvDirectory = Path.Combine(_root.FullPath, "csv");
        StateFilePath = Path.Combine(_root.FullPath, "state", "tcxtool-state.json");
        Directory.CreateDirectory(CsvDirectory);

        var configuration = new Dictionary<string, string?>
        {
            ["TcxTool:CsvOutputDirectory"] = CsvDirectory,
            ["TcxTool:StateFilePath"] = StateFilePath,
        };
        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
        {
            configuration[key] = value;
        }

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(configuration)));
    }

    /// <summary>The configured host factory.</summary>
    public WebApplicationFactory<Program> Factory { get; }

    /// <summary>Absolute path of the CSV output directory (created).</summary>
    public string CsvDirectory { get; }

    /// <summary>Absolute path of the state file (not created).</summary>
    public string StateFilePath { get; }

    /// <summary>Names of the files directly in <see cref="CsvDirectory"/>, sorted.</summary>
    public IReadOnlyList<string> CsvFileNames() =>
        [.. Directory.EnumerateFiles(CsvDirectory).Select(file => Path.GetFileName(file)).Order(StringComparer.Ordinal)];

    /// <summary>Creates a client for the host.</summary>
    /// <param name="options">Client options, or null for the defaults.</param>
    /// <returns>The client.</returns>
    public HttpClient CreateClient(WebApplicationFactoryClientOptions? options = null) =>
        options is null ? Factory.CreateClient() : Factory.CreateClient(options);

    /// <inheritdoc />
    public void Dispose()
    {
        Factory.Dispose();
        _root.Dispose();
    }
}
