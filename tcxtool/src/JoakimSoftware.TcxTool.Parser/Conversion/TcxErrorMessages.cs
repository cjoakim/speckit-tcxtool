using System.Globalization;

namespace JoakimSoftware.TcxTool.Parser.Conversion;

/// <summary>
/// The one place where each <see cref="TcxErrorKind"/>'s user-readable message is defined.
/// Components render these messages and never compose their own.
/// </summary>
public static class TcxErrorMessages
{
    private const double BytesPerMegabyte = 1024 * 1024;

    /// <summary>Returns the message for <paramref name="kind"/>.</summary>
    /// <param name="kind">The kind of failure.</param>
    /// <param name="maxUploadBytes">For <see cref="TcxErrorKind.TooLarge"/>, the configured limit in bytes.</param>
    /// <param name="element">For <see cref="TcxErrorKind.InvalidValue"/>, the element that holds the bad value.</param>
    /// <param name="trackpointNumber">For <see cref="TcxErrorKind.InvalidValue"/>, the 1-based trackpoint number.</param>
    /// <returns>The message.</returns>
    public static string For(TcxErrorKind kind, long? maxUploadBytes = null, string? element = null, int? trackpointNumber = null) =>
        kind switch
        {
            TcxErrorKind.Malformed => "The file is not well-formed XML, so it cannot be read as a TCX file.",
            TcxErrorKind.NotTcx => "The file is not a TCX file: its root element is not a Garmin Training Center Database.",
            TcxErrorKind.ProhibitedContent => "The file contains a document type definition (DOCTYPE), which is not allowed in a TCX file.",
            TcxErrorKind.InvalidValue => InvalidValue(element, trackpointNumber),
            TcxErrorKind.InvalidActivityId => "An activity in the file has a missing or invalid Id, so its date cannot be determined.",
            TcxErrorKind.DuplicateActivityStart => "Two activities in the file start in the same second, so they cannot be told apart.",
            TcxErrorKind.EmptyFileName => "The file needs a name before the .tcx extension.",
            TcxErrorKind.NotTcxFileName => "The file name must end in .tcx.",
            TcxErrorKind.TooLarge => TooLarge(maxUploadBytes),
            TcxErrorKind.OutputFailed => "The CSV could not be saved to the output folder. No files were written.",
            TcxErrorKind.NoFile => "Choose a .tcx file to convert.",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown error kind."),
        };

    /// <summary>Formats a byte count as whole or fractional megabytes, for example <c>50 MB</c>.</summary>
    /// <param name="bytes">The byte count.</param>
    /// <returns>The formatted size.</returns>
    public static string Megabytes(long bytes) =>
        string.Create(CultureInfo.InvariantCulture, $"{Math.Round(bytes / BytesPerMegabyte, 1)} MB");

    private static string InvalidValue(string? element, int? trackpointNumber)
    {
        var where = trackpointNumber is { } number
            ? string.Create(CultureInfo.InvariantCulture, $" at trackpoint {number}")
            : string.Empty;
        return $"The file has an invalid {element ?? "numeric"} value{where}.";
    }

    private static string TooLarge(long? maxUploadBytes) =>
        maxUploadBytes is { } limit
            ? $"The file is larger than the {Megabytes(limit)} limit."
            : "The file is larger than the upload limit.";
}
