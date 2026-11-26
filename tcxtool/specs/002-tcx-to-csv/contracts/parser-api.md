# Contract: Parser Library Public API

`JoakimSoftware.TcxTool.Parser` is BCL-only, with no ASP.NET Core reference (Principle III).
Every public member has an XML doc comment (`GenerateDocumentationFile`). The record shapes are
in [data-model.md](../data-model.md).

## Parsing (`JoakimSoftware.TcxTool.Parser.Tcx`)

```csharp
public static class TcxParser
{
    /// Parses a TCX document through the secure XmlReader (Principle IV).
    public static Task<TcxParseResult> ParseAsync(Stream tcx, CancellationToken cancellationToken);
}
```

- Never throws for bad input. Malformed XML, a wrong root, a DTD, an invalid value, or a bad
  activity Id are all returned as `TcxParseResult.Failure`.
- Throws only for programming errors (`ArgumentNullException`) and for cancellation.
- Root must be `{http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2}TrainingCenterDatabase`,
  otherwise `NotTcx`.
- A file with no `Activities/Activity` is a success with zero activities.

## CSV (`JoakimSoftware.TcxTool.Parser.Csv`)

```csharp
public static class ActivityIdName
{
    public static bool TryFromFileName(string uploadedFileName, out string activityId, out TcxError? error);
}

public static class CsvFileName
{
    public static bool TryFor(IReadOnlyList<TcxActivity> activities, string activityId,
        out IReadOnlyList<string> fileNames, out TcxError? error);
}

public static class ActivityCsvWriter
{
    public const string Header = "ActivityId|Date|...|AltitudeFeet"; // exact text in csv-format.md
    public static int Write(TcxActivity activity, string activityId, TextWriter output); // returns data rows written
}

public static class DerivedNumber
{
    public static string Format(double value); // shortest round-trip, no exponent (research R5)
}
```

`CsvFileName.TryFor` returns the names in activity order, or `false` with a `DuplicateActivityStart`
error when two names collide.

## Conversion (`JoakimSoftware.TcxTool.Parser.Conversion`)

```csharp
public static class TcxCsvConverter
{
    public static Task<ConversionResult> ConvertAsync(
        Stream tcx, string uploadedFileName, string outputDirectory, CancellationToken cancellationToken);
}
```

- Runs these steps in order:
  1. check the file name
  2. parse
  3. work out the output names
  4. render every CSV in memory
  5. write temp files
  6. rename them all
- On any failure it returns `ConversionResult` with an `Error`, and leaves `outputDirectory` as it
  was (research R8).
- Creates `outputDirectory` if it does not exist.
- Does no logging and reads no configuration. The web layer does both.
