namespace JoakimSoftware.TcxTool.Parser.Conversion;

/// <summary>
/// The outcome of converting one upload: either the CSVs saved (one per activity) or the error
/// that stopped the conversion. On failure nothing from the upload is left on disk.
/// </summary>
public sealed record ConversionResult
{
    private ConversionResult(IReadOnlyList<SavedCsv> files, TcxError? error)
    {
        Files = files;
        Error = error;
    }

    /// <summary>The saved CSVs, in activity order; empty on failure.</summary>
    public IReadOnlyList<SavedCsv> Files { get; }

    /// <summary>The error, or null on success.</summary>
    public TcxError? Error { get; }

    /// <summary>True exactly when <see cref="Error"/> is null.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>Creates a successful result.</summary>
    /// <param name="files">The saved CSVs.</param>
    /// <returns>The result.</returns>
    public static ConversionResult Success(IReadOnlyList<SavedCsv> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        return new(files, null);
    }

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The error.</param>
    /// <returns>The result.</returns>
    public static ConversionResult Failure(TcxError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new([], error);
    }
}
