using JoakimSoftware.TcxTool.Parser.Conversion;

using Microsoft.Extensions.Options;

namespace JoakimSoftware.TcxTool.Web.Conversion;

/// <summary>
/// Converts an uploaded TCX file to CSVs in the configured output directory. The page calls this and
/// only renders the returned <see cref="ConversionResult"/> (constitution Principle III).
/// </summary>
public sealed partial class CsvConversionService(
    IOptions<TcxToolOptions> options,
    IHostEnvironment environment,
    ILogger<CsvConversionService> logger)
{
    private readonly TcxToolOptions _options = options.Value;
    private readonly IHostEnvironment _environment = environment;
    private readonly ILogger<CsvConversionService> _logger = logger;

    /// <summary>The <see cref="TcxErrorKind.TooLarge"/> failure, stating the configured limit.</summary>
    internal ConversionResult TooLarge() => ConversionResult.Failure(new TcxError(
        TcxErrorKind.TooLarge, TcxErrorMessages.For(TcxErrorKind.TooLarge, _options.MaxUploadBytes), null, null));

    /// <summary>
    /// Converts <paramref name="file"/>. The size and empty-file checks run before the upload is read.
    /// </summary>
    /// <param name="file">The uploaded file, or null when none was chosen.</param>
    /// <param name="cancellationToken">Cancels the conversion.</param>
    /// <returns>The saved CSVs, or the reason nothing was saved.</returns>
    public async Task<ConversionResult> ConvertAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return ConversionResult.Failure(TcxError.Of(TcxErrorKind.NoFile));
        }

        var result = file.Length > _options.MaxUploadBytes ? TooLarge() : await ConvertFileAsync(file, cancellationToken);
        if (result.Error is { } error)
        {
            // FR-018: the file name and the kind only, never the file's content.
            LogFailure(file.FileName, error.Kind);
        }

        return result;
    }

    private async Task<ConversionResult> ConvertFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        return await TcxCsvConverter.ConvertAsync(stream, file.FileName, OutputDirectory(), cancellationToken);
    }

    private string OutputDirectory() => TcxToolPaths.CsvDirectory(_options, _environment);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Conversion of {FileName} failed: {Kind}")]
    private partial void LogFailure(string fileName, TcxErrorKind kind);
}
