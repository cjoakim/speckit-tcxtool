using System.Text;

namespace JoakimSoftware.TcxTool.Web.Analysis;

/// <summary>
/// Opens a CSV in the CSV folder for reading and classifies what went wrong, for both analysis loaders
/// (specs/006-activity-states/data-model.md <c>AppCsvFile</c>, research R9). It writes nothing.
/// </summary>
internal static class AppCsvFile
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>How an open-and-read ended.</summary>
    internal enum Outcome
    {
        /// <summary>The read function ran to the end.</summary>
        Read,

        /// <summary>The file or its folder is gone.</summary>
        Missing,

        /// <summary>Any other I/O failure, or access was denied.</summary>
        Unreadable,
    }

    /// <summary>
    /// Opens <paramref name="path"/> (shared read, UTF-8 with byte order mark detection) and passes it to
    /// <paramref name="read"/>. The open and the read share one <c>try</c>, because the CSV readers let
    /// I/O exceptions reach their caller.
    /// </summary>
    /// <typeparam name="T">The read function's result.</typeparam>
    /// <param name="path">The full path of the CSV.</param>
    /// <param name="read">Reads the open file.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>
    /// The outcome; the read function's value for <see cref="Outcome.Read"/>; and the exception type name
    /// for <see cref="Outcome.Unreadable"/>, for the log.
    /// </returns>
    internal static async Task<(Outcome Outcome, T? Value, string? ExceptionType)> ReadAsync<T>(
        string path,
        Func<TextReader, CancellationToken, Task<T>> read,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 4096, useAsync: true);
            using var input = new StreamReader(stream, Utf8, detectEncodingFromByteOrderMarks: true);
            return (Outcome.Read, await read(input, cancellationToken), null);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            // Deleted after the store saw it, or never there.
            return (Outcome.Missing, default, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return (Outcome.Unreadable, default, exception.GetType().Name);
        }
    }
}
