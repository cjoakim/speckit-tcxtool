namespace JoakimSoftware.TcxTool.Parser.Conversion;

/// <summary>
/// The reasons a TCX upload cannot be converted. Each kind has its own message in
/// <see cref="TcxErrorMessages"/>.
/// </summary>
public enum TcxErrorKind
{
    /// <summary>The file is not well-formed XML.</summary>
    Malformed,

    /// <summary>The root element is not a Training Center Database v2 document.</summary>
    NotTcx,

    /// <summary>The file contains a document type definition, which is never processed.</summary>
    ProhibitedContent,

    /// <summary>A numeric or time element holds a value that cannot be read.</summary>
    InvalidValue,

    /// <summary>An activity's <c>Id</c> is missing or is not a valid time.</summary>
    InvalidActivityId,

    /// <summary>Two activities start in the same second, so their CSV names would collide.</summary>
    DuplicateActivityStart,

    /// <summary>The uploaded file name is empty once the <c>.tcx</c> extension is removed.</summary>
    EmptyFileName,

    /// <summary>The uploaded file name does not end in <c>.tcx</c>.</summary>
    NotTcxFileName,

    /// <summary>The upload is larger than the configured limit.</summary>
    TooLarge,

    /// <summary>Writing a CSV to the output directory failed.</summary>
    OutputFailed,

    /// <summary>No file, or an empty file, was uploaded.</summary>
    NoFile,
}
