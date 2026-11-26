using JoakimSoftware.TcxTool.Parser.Conversion;

namespace JoakimSoftware.TcxTool.Parser.Tcx;

/// <summary>The outcome of parsing a TCX file: exactly one of a document or an error.</summary>
public sealed record TcxParseResult
{
    private TcxParseResult(TcxDocument? document, TcxError? error)
    {
        Document = document;
        Error = error;
    }

    /// <summary>The parsed document, or null on failure.</summary>
    public TcxDocument? Document { get; }

    /// <summary>The error, or null on success.</summary>
    public TcxError? Error { get; }

    /// <summary>True exactly when <see cref="Error"/> is null and <see cref="Document"/> is set.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>Creates a successful result.</summary>
    /// <param name="document">The parsed document.</param>
    /// <returns>The result.</returns>
    public static TcxParseResult Success(TcxDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new(document, null);
    }

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The error.</param>
    /// <returns>The result.</returns>
    public static TcxParseResult Failure(TcxError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(null, error);
    }
}
