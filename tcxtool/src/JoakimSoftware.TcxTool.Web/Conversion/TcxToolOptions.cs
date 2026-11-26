using System.ComponentModel.DataAnnotations;

namespace JoakimSoftware.TcxTool.Web.Conversion;

/// <summary>
/// The <c>TcxTool</c> configuration section: the upload limit, where CSVs are written, and where the
/// application state file lives.
/// </summary>
public sealed class TcxToolOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "TcxTool";

    /// <summary>The largest accepted upload in bytes; a file of exactly this size is accepted. Default 50 MB.</summary>
    [Range(1, long.MaxValue)]
    public long MaxUploadBytes { get; set; } = 52_428_800;

    /// <summary>The CSV output directory. A relative path resolves against the content root.</summary>
    [Required(AllowEmptyStrings = false)]
    public string CsvOutputDirectory { get; set; } = "data/csv";

    /// <summary>
    /// The application state file (spec 004 FR-005). A relative path resolves against the content root.
    /// Default <c>tcxtool-state.json</c>.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string StateFilePath { get; set; } = "tcxtool-state.json";
}
