namespace JoakimSoftware.TcxTool.Web.Conversion;

/// <summary>
/// Resolves the configured <see cref="TcxToolOptions"/> paths to full paths. A relative path resolves
/// against the content root (spec 004 research R2). This is the one place that rule is written.
/// </summary>
public static class TcxToolPaths
{
    /// <summary>The full path of the CSV output directory.</summary>
    /// <param name="options">The configured options.</param>
    /// <param name="environment">The host environment, for the content root.</param>
    /// <returns>The full path.</returns>
    public static string CsvDirectory(TcxToolOptions options, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Resolve(options.CsvOutputDirectory, environment);
    }

    /// <summary>The full path of the application state file.</summary>
    /// <param name="options">The configured options.</param>
    /// <param name="environment">The host environment, for the content root.</param>
    /// <returns>The full path.</returns>
    public static string StateFile(TcxToolOptions options, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Resolve(options.StateFilePath, environment);
    }

    private static string Resolve(string path, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return Path.GetFullPath(Path.Combine(environment.ContentRootPath, path));
    }
}
