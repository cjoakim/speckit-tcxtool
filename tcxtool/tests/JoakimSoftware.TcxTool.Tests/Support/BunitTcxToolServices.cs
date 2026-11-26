using JoakimSoftware.TcxTool.Web.Analysis;
using JoakimSoftware.TcxTool.Web.Conversion;
using JoakimSoftware.TcxTool.Web.State;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace JoakimSoftware.TcxTool.Tests.Support;

/// <summary>
/// Registers the services pages need in a bUnit context, with the CSV directory and the state file
/// inside a test-owned <see cref="TempDirectory"/>, never the developer's real paths (spec 004 FR-020).
/// </summary>
public static class BunitTcxToolServices
{
    /// <summary>
    /// Adds logging, the options, the host environment, <see cref="CurrentCsvStore"/>, and
    /// <see cref="CurrentActivityLoader"/> (spec 005), and <see cref="CurrentStatesLoader"/> (spec 006).
    /// </summary>
    /// <param name="services">The bUnit service collection.</param>
    /// <param name="root">The temp directory that holds <c>csv/</c> and <c>state/</c>.</param>
    /// <returns>The registered options, whose paths the test can write fixtures into.</returns>
    public static TcxToolOptions Add(IServiceCollection services, TempDirectory root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var options = new TcxToolOptions
        {
            CsvOutputDirectory = Path.Combine(root.FullPath, "csv"),
            StateFilePath = Path.Combine(root.FullPath, "state", "tcxtool-state.json"),
        };
        Directory.CreateDirectory(options.CsvOutputDirectory);

        services.AddLogging();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { ContentRootPath = root.FullPath });
        services.AddSingleton<CurrentCsvStore>();
        services.AddSingleton<CurrentActivityLoader>();
        services.AddSingleton<CurrentStatesLoader>();
        return options;
    }
}
