# Data Model: Table Page for the Current CSV

**Feature**: `005-table-page` | **Date**: 2026-11-25 | **Spec**: [spec.md](./spec.md)

Every type is one public type per file, in the namespace of its folder. Every public parser type
has XML doc comments (Principle II, `GenerateDocumentationFile`).

## Parser library: `src/JoakimSoftware.TcxTool.Parser/Csv/`

### `ActivityCsvRow` (sealed record)

One CSV data row, with the values exactly as written (FR-013). It has the 17 contract fields in
contract order, each a non-null `string`. An empty field is `""`.

| Property | Contract field # | Shown on the Table page |
|----------|------------------|-------------------------|
| `ActivityId` | 1 | header box |
| `Date` | 2 | header box |
| `DeviceName` | 3 | header box |
| `DeviceId` | 4 | header box |
| `Sport` | 5 | header box |
| `TrackpointNum` | 6 | column 1 |
| `Time` | 7 | column 2 |
| `ElapsedSeconds` | 8 | column 3 |
| `ElapsedTime` | 9 | column 4 |
| `Latitude` | 10 | column 8 |
| `Longitude` | 11 | column 9 |
| `HeartRateBpm` | 12 | column 5, banded |
| `RunCadence` | 13 | column 6, banded |
| `DistanceMeters` | 14 | no |
| `DistanceMiles` | 15 | column 7 |
| `AltitudeMeters` | 16 | no |
| `AltitudeFeet` | 17 | column 10 |

The table's column order (FR-012) differs from the file order. The `TrackpointTable` component
does that mapping, not this record.

### `ActivityCsvErrorKind` (enum)

| Value | Meaning |
|-------|---------|
| `HeaderMismatch` | The input is empty, or line 1 is not exactly `ActivityCsvWriter.Header` |
| `FieldCount` | A data line does not split into exactly 17 fields on `\|` |

### `ActivityCsvError` (sealed record)

| Property | Type | Rule |
|----------|------|------|
| `Kind` | `ActivityCsvErrorKind` | |
| `LineNumber` | `int` | 1-based. Always 1 for `HeaderMismatch`; ≥ 2 for `FieldCount` |

### `ActivityCsvReadResult` (sealed record)

Exactly one of the two is meaningful:

| Property | Type | Rule |
|----------|------|------|
| `Rows` | `IReadOnlyList<ActivityCsvRow>` | File order. Empty on error, and empty for a header-only file. |
| `Error` | `ActivityCsvError?` | Null on success |
| `IsSuccess` | `bool` | `Error is null` |

It is created only through `Success(rows)` and `Failure(error)`.

### `ActivityCsvReader` (static class)

| Member | Signature | Behavior |
|--------|-----------|----------|
| `FieldCount` | `const int = 17` | |
| `ReadAsync` | `Task<ActivityCsvReadResult> ReadAsync(TextReader input, CancellationToken cancellationToken)` | Applies research R3. Never throws for content. Throws `ArgumentNullException` for a null input, and `OperationCanceledException` when cancelled. I/O exceptions from `input` propagate to the caller. |

## Web project: `src/JoakimSoftware.TcxTool.Web/Analysis/` (new)

### `CurrentActivityStatus` (enum)

| Value | When |
|-------|------|
| `NoCurrentCsv` | `CurrentCsvStore.ReadAsync` returned `None` |
| `Missing` | The store returned `Missing`, or opening the file threw `FileNotFoundException` or `DirectoryNotFoundException` |
| `Unreadable` | Opening or reading threw another `IOException`, or an `UnauthorizedAccessException` |
| `WrongFormat` | `ActivityCsvReader` returned an error |
| `NoTrackpoints` | The read succeeded with 0 rows |
| `Loaded` | The read succeeded with 1 or more rows |

### `ActivitySummary` (sealed record)

The header box values (FR-009). Every value is a non-null string, and an empty one shows as a dash
(FR-010, handled by the component).

| Property | Source |
|----------|--------|
| `FileName` | the current CSV's file name |
| `ActivityId`, `Date`, `DeviceName`, `DeviceId`, `Sport` | the first row; `""` when there are no rows |

It is created through `ActivitySummary.From(string fileName, ActivityCsvRow? firstRow)`.

### `CurrentActivity` (sealed record)

What the loader returns. Pages bind it. They never see a stream or a path.

| Property | Type | Rule |
|----------|------|------|
| `Status` | `CurrentActivityStatus` | |
| `FileName` | `string?` | Null exactly when `Status` is `NoCurrentCsv` |
| `Summary` | `ActivitySummary?` | Set only for `NoTrackpoints` and `Loaded` |
| `Rows` | `IReadOnlyList<ActivityCsvRow>` | Non-empty only for `Loaded`; empty otherwise |

It is created only through static members, one per status, so that a status can't come with the
wrong properties: the static property `NoCurrentCsv { get; }` (following `CurrentCsv.None`), and the
static factory methods `Missing(name)`, `Unreadable(name)`, `WrongFormat(name)`, and
`Read(name, rows)`. `Read` returns `NoTrackpoints` or `Loaded`, depending on whether there are
rows.

### `CurrentActivityLoader` (sealed partial class, singleton)

| Member | Behavior |
|--------|----------|
| constructor | `(CurrentCsvStore store, IOptions<TcxToolOptions> options, IHostEnvironment environment, ILogger<CurrentActivityLoader> logger)`. Resolves the CSV directory once, through `TcxToolPaths.CsvDirectory`. |
| `LoadAsync(CancellationToken)` | Applies research R2. Writes nothing (FR-007). Logs as in research R11. Never throws for a missing, unreadable, or malformed file. |

### `CurrentActivityMessages` (static class)

The only place this feature's user-facing text is written. Its exact strings are in
[contracts/pages.md](./contracts/pages.md#messages).

`For(CurrentActivityStatus)` returns the message for each status other than `Loaded`, which gets
`""`. An unknown value throws `ArgumentOutOfRangeException`.

## Web project: `src/JoakimSoftware.TcxTool.Web/Table/` (new)

### `TrackpointBands` (static class)

| Member | Returns |
|--------|---------|
| `HeartRateClass(string value)` | `hr-band-1` … `hr-band-6`, or null |
| `RunCadenceClass(string value)` | `cadence-band-1` … `cadence-band-4`, or null |

**Heart rate** (FR-014), with `v` the parsed value:

| Range of `v` | Class |
|--------------|-------|
| `v < 90` | `hr-band-1` |
| `90 <= v < 120` | `hr-band-2` |
| `120 <= v < 140` | `hr-band-3` |
| `140 <= v < 160` | `hr-band-4` |
| `160 <= v < 180` | `hr-band-5` |
| `v >= 180` | `hr-band-6` |

**Run cadence** (FR-015), with `spm = 2 × v`:

| Range of `spm` | CSV `v` | Class |
|----------------|---------|-------|
| `spm < 20` | `v < 10` | `cadence-band-1` |
| `20 <= spm < 150` | `10 <= v < 75` | `cadence-band-2` |
| `150 <= spm < 180` | `75 <= v < 90` | `cadence-band-3` |
| `spm >= 180` | `v >= 90` | `cadence-band-4` |

Parsing and the "no band" rule are in research R6.

## Components: `src/JoakimSoftware.TcxTool.Web/Components/`

| Component | Parameters | Job |
|-----------|-----------|-----|
| `Pages/TablePage.razor` + `TablePage.razor.cs` | none; injects `CurrentActivityLoader`, cascades `HttpContext` for `RequestAborted` | Loads once in `OnInitializedAsync`, then renders the `h1` and the state from research R8 |
| `Table/ActivitySummaryCard.razor` (new) | `[Parameter, EditorRequired] ActivitySummary Summary` | The header box (FR-008 to FR-010) |
| `Table/TrackpointTable.razor` (new) | `[Parameter, EditorRequired] IReadOnlyList<ActivityCsvRow> Rows` | The scroll container and the table (FR-012, FR-013, FR-014 to FR-017, FR-019). One loop, with no child component per row (research R4). |

## Lifecycle

There is no state to change. A GET of `/table` runs the following steps once, and keeps nothing
between requests:

```text
GET /table
  └─ TablePage.OnInitializedAsync
       └─ CurrentActivityLoader.LoadAsync
            ├─ CurrentCsvStore.ReadAsync ── None ───────────────► NoCurrentCsv
            │                            ── Missing ────────────► Missing(name)
            │                            ── Present(name)
            ├─ open <csv dir>/<name> ──── FileNotFound/DirNotFound ► Missing(name)
            │                         ──── other IO / access ───► Unreadable(name)   (logged)
            └─ ActivityCsvReader.ReadAsync ── Error ─────────────► WrongFormat(name)  (logged)
                                           ── 0 rows ───────────► NoTrackpoints
                                           ── n rows ───────────► Loaded
```

## Registration (`Program.cs`)

- `builder.Services.AddSingleton<CurrentActivityLoader>();`
- The loader holds no state between requests. It is a singleton, like the `CurrentCsvStore` it
  depends on.
