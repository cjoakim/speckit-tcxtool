using JoakimSoftware.TcxTool.Parser.Conversion;

namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>
/// Turns an uploaded file name into the ActivityId used in every CSV row and in the output file
/// name (spec FR-007).
/// </summary>
public static class ActivityIdName
{
    private const string TcxExtension = ".tcx";

    /// <summary>
    /// Removes any directory part and the <c>.tcx</c> extension (in any letter case) from
    /// <paramref name="uploadedFileName"/>, then replaces every character other than an ASCII letter,
    /// digit, <c>-</c>, or <c>_</c> with <c>_</c>.
    /// </summary>
    /// <param name="uploadedFileName">The file name as uploaded.</param>
    /// <param name="activityId">The ActivityId, or an empty string on failure.</param>
    /// <param name="error"><see cref="TcxErrorKind.NotTcxFileName"/> or <see cref="TcxErrorKind.EmptyFileName"/> on failure.</param>
    /// <returns>True when an ActivityId was produced.</returns>
    public static bool TryFromFileName(string uploadedFileName, out string activityId, out TcxError? error)
    {
        ArgumentNullException.ThrowIfNull(uploadedFileName);

        var name = uploadedFileName[(uploadedFileName.LastIndexOfAny(['/', '\\']) + 1)..];
        activityId = string.Empty;

        if (!name.EndsWith(TcxExtension, StringComparison.OrdinalIgnoreCase))
        {
            error = TcxError.Of(TcxErrorKind.NotTcxFileName);
            return false;
        }

        var stem = name[..^TcxExtension.Length];
        if (stem.Length == 0)
        {
            error = TcxError.Of(TcxErrorKind.EmptyFileName);
            return false;
        }

        activityId = string.Create(stem.Length, stem, static (chars, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                chars[i] = char.IsAsciiLetterOrDigit(c) || c is '-' or '_' ? c : '_';
            }
        });
        error = null;
        return true;
    }
}
