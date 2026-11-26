# Data Model: Activity States

Types are listed by project and namespace. All parser types are public, immutable, and carry XML
doc comments (Principle II). "As written" means the exact text of the CSV field, never re-formatted.
The file formats themselves are in [contracts/csv-format.md](contracts/csv-format.md) and
[contracts/states-csv-format.md](contracts/states-csv-format.md).

## Parser library: `JoakimSoftware.TcxTool.Parser.Csv`

### `ActivityState` (static class, new)

| Member | Value |
|--------|-------|
| `Stopped` | `"stopped"` |
| `Walking` | `"walking"` |
| `Running` | `"running"` |
| `Unknown` | `"unknown"` |
| `RunningStepsPerMinute` | `150` |
| `For(double? stepsPerMinute)` | `Stopped` for 0; `Walking` for > 0 and < 150; `Running` for ≥ 150; `Unknown` for null or < 0 (spec FR-003, research R3) |

### `ActivityCsvRow` (positional sealed record, changed: 17 → 19 fields)

Every property is a `string`, as written, in contract order. `State` and `StateSeconds` are inserted
after `RunCadence`:

`ActivityId, Date, DeviceName, DeviceId, Sport, TrackpointNum, Time, ElapsedSeconds, ElapsedTime,
Latitude, Longitude, HeartRateBpm, RunCadence, State, StateSeconds, DistanceMeters, DistanceMiles,
AltitudeMeters, AltitudeFeet`

| Field | Rule |
|-------|------|
| `ActivityCsvRow.RunCadence` | twice the TCX value, shortest decimal; empty when the trackpoint has none (FR-002) |
| `ActivityCsvRow.State` | one of the four `ActivityState` names, never empty (FR-003) |
| `ActivityCsvRow.StateSeconds` | elapsed time minus the StateSet's start, shortest decimal; empty when `ElapsedSeconds` is empty (FR-005, research R2) |
| every other field | unchanged from feature 002 |

### `ActivityCsvWriter` (static class, changed)

| Member | Change |
|--------|--------|
| `Header` | the 19-field header (contracts/csv-format.md) |
| `FieldCount` | new constant, `19` |
| `Rows(TcxActivity activity, string activityId)` | new: builds the `ActivityCsvRow`s, assigning State and StateSeconds (research R1, R2) |
| `Write(IReadOnlyList<ActivityCsvRow> rows, TextWriter output)` | new: writes the header and rows, applying `CsvText.Clean` to every field |
| `Write(TcxActivity, string, TextWriter)` | kept; now `Write(Rows(...), output)`, returning the row count |

**StateSet start**, inside `Rows`: a running value that starts at `TimeSpan.Zero`. At every row whose
State differs from the previous row's, it becomes the elapsed `TimeSpan` of the last *timed* row seen
so far, or stays `TimeSpan.Zero` when there is none.

### `StatesCsvRow` (positional sealed record, new)

13 `string` properties, as written:

| Field | Rule (FR-009) |
|-------|---------------|
| `StatesCsvRow.ActivityId` | the set's ActivityId |
| `StatesCsvRow.State` | the set's State |
| `StatesCsvRow.StateEndDate` | `Time` of the set's last row with a non-empty Time; empty when none |
| `StatesCsvRow.StateEndTrackpointNum` | `TrackpointNum` of the set's last row; never empty |
| `StatesCsvRow.StateElapsedSeconds` | `StateSeconds` of the set's last row with a non-empty StateSeconds; empty when none |
| `StatesCsvRow.ActivityElapsedTime` | `ElapsedTime` of the set's last row with a non-empty ElapsedTime; empty when none |
| `StatesCsvRow.AverageLatitude` | mean of the set's finite `Latitude` values; empty when none |
| `StatesCsvRow.AverageLongitude` | mean of the set's finite `Longitude` values; empty when none |
| `StatesCsvRow.AverageHeartRateBpm` | mean of the set's finite `HeartRateBpm` values; empty when none |
| `StatesCsvRow.AverageRunCadence` | mean of the set's finite (doubled) `RunCadence` values; empty when none |
| `StatesCsvRow.DistanceMiles` | last finite `DistanceMiles` in the set minus the last finite `DistanceMiles` before the set (0 when none); empty when the set has none |
| `StatesCsvRow.AverageAltitudeMeters` | mean of the set's finite `AltitudeMeters` values; empty when none |
| `StatesCsvRow.AverageAltitudeFeet` | mean of the set's finite `AltitudeFeet` values; empty when none |

Means are a left-to-right sum divided by the count, and differences are one subtraction. Both are
written with `DerivedNumber.Format` (research R2).

### `StatesCsvWriter` (static class, new)

| Member | Meaning |
|--------|---------|
| `Header` | the 13-field header (contracts/states-csv-format.md) |
| `FieldCount` | `13` |
| `Rows(IReadOnlyList<ActivityCsvRow> rows)` | groups consecutive rows by `State` into StateSets, in order, and returns one `StatesCsvRow` per set; empty input gives an empty list |
| `Write(IReadOnlyList<StatesCsvRow> rows, TextWriter output)` | writes the header and rows, applying `CsvText.Clean` |

### `StatesCsvFileName` (static class, new)

| Member | Meaning |
|--------|---------|
| `Suffix` | `".states.csv"` |
| `For(string csvFileName)` | replaces the final `.csv` with `.states.csv`; throws `ArgumentException` when the name does not end in `.csv` |
| `IsStatesCsvName(string? name)` | true when the name ends in `.states.csv`, ignoring case (research R4) |

### Readers

- `ActivityCsvReader` (changed): it checks the 19-field header, and `FieldCount` becomes 19. A
  17-field file is `HeaderMismatch` at line 1 (FR-017).
- `StatesCsvReader` (new):
  - `ReadAsync(TextReader input, CancellationToken)` returns a `StatesCsvReadResult`.
  - It throws only for a null input or cancellation.
- `StatesCsvReadResult` (new): the same shape as `ActivityCsvReadResult`: `Rows`, `Error`,
  `IsSuccess`, `Success(rows)`, and `Failure(error)`. The error is an `ActivityCsvError`.
- `PipeDelimitedLines` (internal, new): the line reader both readers share (research R8). It checks
  the exact header, splits each later line on `|`, and fails on the first line whose field count is
  wrong.

## Parser library: `JoakimSoftware.TcxTool.Parser.Conversion`

### `SavedCsv` (positional sealed record, changed)

| Field | Rule |
|-------|------|
| `SavedCsv.FileName` | unchanged |
| `SavedCsv.RowCount` | unchanged |
| `SavedCsv.StatesFileName` | `StatesCsvFileName.For(FileName)` (new) |
| `SavedCsv.StateSetCount` | number of data rows in the States CSV, ≥ 0 (new) |

`TcxCsvConverter` writes 2 files per activity with the existing temp-then-move procedure. A failure
removes every temp and every moved file (research R6).

## Web project: `JoakimSoftware.TcxTool.Web.State`

| Type | Change |
|------|--------|
| `TcxToolState` | adds `StatesCsvFilename` (`statesCsvFilename`, omitted when null) |
| `CurrentCsv` | adds `StatesFileName` (`string?`). It is non-null only when the state file's `statesCsvFilename` equals `StatesCsvFileName.For(FileName)`, ordinal (FR-016). `Present` and `Missing` take it as an optional second argument |
| `CurrentCsvStore` | `ReadAsync` fills `StatesFileName`. `SelectAsync` writes both names (FR-014). `RecordConversionAsync` is unchanged in shape (FR-013) |
| `CsvFileNameRule` | `IsPlainCsvName` is false for any `StatesCsvFileName.IsStatesCsvName` name (FR-015) |

## Web project: `JoakimSoftware.TcxTool.Web.Analysis`

### `AppCsvFile` (internal static class, new)

The open-and-classify code moved out of `CurrentActivityLoader` (research R9). Given a full path and
a read function, it returns one of three outcomes:

- `Missing`: `FileNotFoundException` or `DirectoryNotFoundException`
- `Unreadable`: any other `IOException`, or `UnauthorizedAccessException`, with the exception type
  name for the log
- the read function's result

The open is the existing `FileStream` (Open, Read, `ReadWrite | Delete`, async) and `StreamReader`
(UTF-8, BOM detection).

### `CurrentStatesStatus` (enum, new)

`NoCurrentCsv`, `NoStatesCsv`, `Missing`, `Unreadable`, `WrongFormat`, `NoStateSets`, `Loaded`.

### `CurrentStates` (sealed record, new)

| Property | Meaning |
|----------|---------|
| `Status` | a `CurrentStatesStatus` |
| `CsvFileName` | the current CSV's name; null only for `NoCurrentCsv` |
| `StatesFileName` | the States CSV's name; null for `NoCurrentCsv` and `NoStatesCsv` |
| `Rows` | `IReadOnlyList<StatesCsvRow>`; empty unless `Loaded` |

Invariant: `Rows.Count > 0` exactly when `Status` is `Loaded`.

### `CurrentStatesLoader` (sealed partial class, singleton, new)

`LoadAsync(CancellationToken)` returns a `CurrentStates`. It writes nothing and never throws for bad
input. The rules are in [contracts/pages.md](contracts/pages.md), "States page".

It logs `States CSV {FileName} could not be read: {ExceptionType}` and `States CSV {FileName} is not
in the expected format: {ErrorKind} at line {LineNumber}`, both at warning level.

### `CurrentStatesMessages` (static class, new)

| Status | Text |
|--------|------|
| `NoCurrentCsv` | `No CSV is selected yet.` |
| `NoStatesCsv` | `The current CSV has no States CSV:` |
| `Missing` | `The States CSV is no longer in the CSV folder:` |
| `Unreadable` | `The States CSV could not be read:` |
| `WrongFormat` | `The States CSV is not in the format this app writes:` |
| `NoStateSets` | `This activity has no state changes.` |
| `Loaded` | `""` |

`CurrentActivityMessages` is unchanged. The Table page adds its "Convert its TCX file again" link in
markup (research R10).

## Web project: `JoakimSoftware.TcxTool.Web.Table`

`TrackpointBands.RunCadenceClass(string value)` (changed): `cadence-band-1` below 20, `-2` from 20
to below 150, `-3` from 150 to below 180, and `-4` at 180 or more, on the value as written. It
returns null for an empty or non-finite value. `HeartRateClass` is unchanged (FR-018, FR-021).

## Relationships

```text
TcxActivity ──Rows──▶ ActivityCsvRow[] ──Write──▶ <name>.csv
                           │
                           └──StatesCsvWriter.Rows──▶ StatesCsvRow[] ──Write──▶ <name>.states.csv

tcxtool-state.json { csvFilename, statesCsvFilename? }
      │ CurrentCsvStore.ReadAsync
      ▼
CurrentCsv { FileName, Status, StatesFileName? }
      ├── CurrentActivityLoader ──▶ CurrentActivity ──▶ Table page
      └── CurrentStatesLoader   ──▶ CurrentStates   ──▶ States page
```
