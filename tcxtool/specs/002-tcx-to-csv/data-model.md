# Data Model: TCX to CSV Conversion

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Date**: 2026-11-22

All parsed TCX data is immutable `record` types in the parser library (Principle II), one public
type per file, each with an XML doc comment. Namespaces sit under `JoakimSoftware.TcxTool.Parser`:

- `.Tcx`: the parsed model
- `.Csv`: row formatting
- `.Conversion`: files and results

Web types sit under `JoakimSoftware.TcxTool.Web`.

## Value types (`Parser.Tcx`)

| Type | Fields | Rules |
|------|--------|-------|
| `RecordedNumber` | `string Text`, `double Value` | `Text` is the element text, trimmed of XML whitespace. `Value` is parsed with `NumberStyles.Float` and the invariant culture. It must be finite, otherwise `InvalidValue` (research R5). |
| `RecordedTime` | `string Text`, `DateTimeOffset Value` | Parsed with `DateTimeStyles.RoundtripKind` and the invariant culture, otherwise `InvalidValue` (R6). |

The `Text` fields keep each value exactly as written (FR-011). `Value` feeds the derived fields.

## Parsed TCX model (`Parser.Tcx`)

```text
TcxDocument 1 ── * TcxActivity 1 ── * TcxLap 1 ── * TcxTrackpoint
                        └── 0..1 TcxDevice            └── 0..1 TcxPosition
```

| Record | Fields | Source (TCX v2 namespace unless noted) |
|--------|--------|------------------------------------------|
| `TcxDocument` | `IReadOnlyList<TcxActivity> Activities` | `TrainingCenterDatabase/Activities/Activity`, in document order |
| `TcxActivity` | `RecordedTime Id`, `string? Sport`, `TcxDevice? Creator`, `IReadOnlyList<TcxLap> Laps` | `Id` (required), `@Sport`, `Creator` (anywhere in the activity, normally after the laps) |
| `TcxDevice` | `string? Name`, `string? UnitId` | `Creator/Name`, `Creator/UnitId` |
| `TcxLap` | `RecordedTime? StartTime`, `RecordedNumber? TotalTimeSeconds`, `RecordedNumber? DistanceMeters`, `RecordedNumber? MaximumSpeed`, `RecordedNumber? Calories`, `RecordedNumber? AverageHeartRateBpm`, `RecordedNumber? MaximumHeartRateBpm`, `string? Intensity`, `string? TriggerMethod`, `IReadOnlyList<TcxTrackpoint> Trackpoints` | `@StartTime` and the child elements. Trackpoints from every `Track` in the lap, in order. Parsed for later features; not written to CSV here (spec Assumptions). |
| `TcxTrackpoint` | `RecordedTime? Time`, `TcxPosition? Position`, `RecordedNumber? AltitudeMeters`, `RecordedNumber? DistanceMeters`, `RecordedNumber? HeartRateBpm`, `RecordedNumber? RunCadence`, `RecordedNumber? Speed`, `RecordedNumber? Watts` | `Time`, `Position`, `AltitudeMeters`, `DistanceMeters`, `HeartRateBpm/Value`. `RunCadence`, `Speed`, and `Watts` come from `Extensions/TPX` in the Activity Extension v2 namespace (FR-003). |
| `TcxPosition` | `RecordedNumber LatitudeDegrees`, `RecordedNumber LongitudeDegrees` | `Position/*`. A `Position` missing either child is treated as absent. |

**Validation while mapping**:
- Elements from unknown namespaces and unknown extensions are ignored.
- Every optional element may be missing.
- An element that is present but empty counts as missing.
- A present element whose text is not a finite number or a valid time fails the whole parse with
  `InvalidValue`. The error carries the element's local name (for `HeartRateBpm/Value`, the parent
  `HeartRateBpm`) and the 1-based trackpoint number
  within its activity.

## Errors (`Parser.Conversion`)

| Type | Fields |
|------|--------|
| `TcxErrorKind` (enum) | `Malformed`, `NotTcx`, `ProhibitedContent`, `InvalidValue`, `InvalidActivityId`, `DuplicateActivityStart`, `EmptyFileName`, `NotTcxFileName`, `TooLarge`, `OutputFailed`, `NoFile` |
| `TcxErrorMessages` (static) | one fixed message per kind; `TooLarge` takes the limit in MB, `InvalidValue` takes the element and trackpoint number |
| `TcxError` | `TcxErrorKind Kind`, `string Message`, `string? Element`, `int? TrackpointNumber` |

`Message` is user-readable English. It never contains file content beyond an element name. The
`TooLarge` message states the limit in MB. The parser produces the first six kinds. The converter
adds `EmptyFileName`, `NotTcxFileName`, and `OutputFailed`. The web service adds `TooLarge` and
`NoFile` (a null or empty upload).

Messages are created where the error is detected: the parser, the converter, or
`CsvConversionService`. Each kind's text is fixed in one place, `TcxErrorMessages`
(`Parser.Conversion`), including the kinds the web project raises. Components render
`TcxError.Message` and never compose their own message.

## Results

| Type | Fields | Notes |
|------|--------|-------|
| `TcxParseResult` | `TcxDocument? Document`, `TcxError? Error`, `bool IsSuccess` | Exactly one of `Document` / `Error` is set. Created through `Success(...)` / `Failure(...)`. |
| `SavedCsv` | `string FileName`, `int RowCount` | `FileName` is the name only, for example `2026-11-01_24932322230.csv`. `RowCount` 0 means the activity had no trackpoints (FR-017). |
| `ConversionResult` | `IReadOnlyList<SavedCsv> Files`, `TcxError? Error`, `bool IsSuccess` | On failure `Files` is empty, and nothing from this upload is on disk (FR-015). |

## CSV row (`Parser.Csv`)

There is no stored entity. `ActivityCsvWriter` maps one `TcxTrackpoint` and its `TcxActivity` to the
17 fields in [contracts/csv-format.md](./contracts/csv-format.md). The calculated fields:

| Field | Rule |
|-------|------|
| `ActivityId` | `ActivityIdName.FromFileName(uploadedName)`: take the file name without its directory and without `.tcx` (any letter case), and map each character outside `[A-Za-z0-9_-]` to `_`. Empty → `EmptyFileName`; no `.tcx` → `NotTcxFileName`. |
| `Date` | `Id.Value.ToString("yyyy-MM-dd", Invariant)`, in the Id's own offset |
| `TrackpointNum` | 1-based, per activity, across laps |
| `ElapsedSeconds` / `ElapsedTime` | Measured from the first trackpoint with a time (research R6) |
| `DistanceMiles` / `AltitudeFeet` | `Value / 1609.344`, `Value / 0.3048`, formatted by `DerivedNumber.Format` (R5) |

## Output file names (`Parser.Csv.CsvFileName`)

- One activity: `<Date>_<ActivityId>.csv`
- More than one activity: `<Date>_<HHmmss>_<ActivityId>.csv`, where `HHmmss` comes from each
  activity's `Id`
- Two activities with the same `<Date>_<HHmmss>` → `DuplicateActivityStart`

## Conversion lifecycle

```text
upload ─► file present? ─► size check ─► file-name check ─► parse ─► render all CSVs in memory ─► write temp files ─► rename all
   │            │              │               │              │                │                        │
   └ TooLarge   └ NoFile       └ TooLarge      └ Empty/NotTcx └ parse errors   └ DuplicateActivityStart └ OutputFailed (temp and renamed files removed)
     (guard)      (service)      (service)       FileName
```

Every failure branch leaves `data/csv/` as it was before the upload.

## Web types (`JoakimSoftware.TcxTool.Web`)

| Type | Fields / members | Notes |
|------|------------------|-------|
| `TcxToolOptions` | `long MaxUploadBytes` = 52,428,800; `string CsvOutputDirectory` = `data/csv` | Section `TcxTool`, validated at startup (`MaxUploadBytes > 0`, directory not blank) |
| `CsvConversionService` | `Task<ConversionResult> ConvertAsync(IFormFile file, CancellationToken)` | Returns `NoFile` for a null or empty file, checks the size, resolves the output directory against the content root, calls `TcxCsvConverter`, and logs failures with the file name and `Kind` only (FR-018) |
| `UploadSizeGuard` | middleware | Redirects oversize `POST /convert` to `/convert?error=too-large` (R2) |
