# Research: Activity States

Every decision below was checked against the code as it stands after feature 005, and against
probes run on this machine (SDK 11.0.100, macOS, Python 3.15.2). The probes were throwaway projects
in the session scratchpad. They are deleted.

## Facts verified on this machine

| Fact | How it was checked | Used by |
|------|--------------------|---------|
| The real marathon CSV gives 541 StateSets: 161 running, 232 walking, 148 stopped, and no `unknown` rows | `awk` over the cadence column (doubled), not the position columns | SC-004, quickstart |
| In that CSV, measuring each StateSet from its own first row would lose 540 s and 0.651 mi, against a total of 19,881 s and 26.427 mi | `awk` over ElapsedSeconds and DistanceMiles at set boundaries | Clarification Q3, R2 |
| Every trackpoint in that CSV is 1 s after the one before, and no cadence value is fractional | `awk` | R2, quickstart |
| `Enumerable.Sum()` and `Average()` on `double[]` and `List<double>` give exactly the same result as a left-to-right `+=` loop (20,000 values) | console probe | R2 |
| Python 3.15's `sum()` of floats is compensated: `sum([0.1]*10)` is `1.0`, and a `+=` loop gives `0.9999999999999999` | `python3 -c` | R12 |
| APFS here is case-insensitive: after `touch x.states.csv`, `ls X.STATES.CSV` finds it | shell | R4 |
| `System.Text.Json` with `JsonIgnoreCondition.WhenWritingNull` leaves the property out and keeps the 2-space, `\n` layout of the state file | console probe | R5 |
| A non-numeric or non-finite TCX number is already rejected by the parser as `InvalidValue` (fixtures `invalid-number.tcx`, `invalid-number-nan.tcx`). A present RunCadence always has a finite `RecordedNumber.Value` | code reading (`TcxParser.Number`) | R3 |
| ActivityIds contain only `A–Z a–z 0–9 - _` (`ActivityIdName`), so no activity CSV name has a second `.` | code reading | R4 |

## R1. Where the states are calculated

- **Decision**: In the parser library's `Csv/` namespace.
  - `ActivityCsvWriter` gains a `Rows(activity, activityId)` step that builds the 19-field
    `ActivityCsvRow`s, including State and StateSeconds, and a `Write(rows, output)` step that
    writes them.
  - The existing `Write(activity, activityId, output)` stays as the two steps in sequence, so its
    callers and tests keep working.
  - A new `StatesCsvWriter` takes those same rows, groups them into StateSets, and builds and
    writes `StatesCsvRow`s.
  - `TcxCsvConverter` builds the rows once and renders both files from them.
- **Rationale**:
  - FR-010 says the States CSV is calculated from the values written to the activity CSV. Taking
    the very rows that are written makes that true by construction, with no re-read.
  - Principle III: the calculation needs no web host and is tested with `dotnet test` alone.
- **Alternatives considered**:
  - *Re-read the written CSV with `ActivityCsvReader`*: the same result with an extra parse and a
    second code path to keep in step.
  - *Calculate in the Web project*: breaks Principle III, and the converter, which lives in the
    parser library, must write the file.

## R2. Exact arithmetic

- **Decision**:
  - **StateSeconds** is calculated from `TimeSpan`s, like ElapsedSeconds: the row's elapsed
    `TimeSpan` minus the StateSet's start `TimeSpan`, written with `ElapsedTime.FormatSeconds`. The
    start is the elapsed `TimeSpan` of the last timed row before the set, or `TimeSpan.Zero` when
    there is none (Clarification Q3).
  - **StateElapsedSeconds, StateEndDate, ActivityElapsedTime** are copied as text from the set's
    last row that has a value. No number is re-formatted.
  - **DistanceMiles** is `double` arithmetic on the parsed DistanceMiles text: the last value in the
    set minus the last value before it (0 when there is none), written with `DerivedNumber.Format`.
  - **Averages** are a left-to-right `+=` sum of the parsed values divided by their count, written
    with `DerivedNumber.Format`. The code uses an explicit loop, so the order is fixed by the
    source, not by a library.
  - Values are parsed with `double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture)`,
    and only finite values count.
- **Rationale**:
  - `TimeSpan` subtraction is exact in ticks, so StateSeconds never shows float noise such as
    `0.30000000000000004` when ElapsedSeconds is fractional. `ticks / 1e7` is one correctly rounded
    division, the same rule ElapsedSeconds already uses.
  - Copying text keeps the two files byte-consistent (FR-010).
  - On SDK 11, LINQ's sum gives the same result as the loop (facts table). An explicit loop states
    that order in the code, which the golden files rely on (R12).
- **Alternatives considered**:
  - *StateSeconds from the parsed ElapsedSeconds doubles*: float noise on fractional times.
  - *DistanceMiles from meters divided by 1609.344*: a different last digit from the activity CSV's
    own miles column, so the two files would disagree.
  - *Compensated or decimal sums*: no requirement asks for more precision than a plain mean, and
    the independent calculation would need the same algorithm.

## R3. State and doubled RunCadence

- **Decision**:
  - RunCadence is `DerivedNumber.Format(trackpoint.RunCadence.Value * 2)`, or empty when the
    trackpoint has none. Multiplying by 2 is exact in binary floating point.
  - `ActivityState.For(double? stepsPerMinute)` returns `stopped` for 0, `walking` for greater than 0
    and less than 150, `running` for 150 or more, and `unknown` for null.
  - A **negative** value also gives `unknown`. The parser accepts any finite number, so `-5` can
    reach the writer. The spec's ranges cover 0 and above, and `unknown` is its default.
  - The four state names are public constants on `ActivityState`. The Web project uses them and never
    repeats the strings.
- **Rationale**: FR-002 and FR-003. A non-numeric value cannot reach the writer (facts table), so
  "not a number" only matters for the readers.
- **Alternatives considered**: *Negative as `stopped`*: it claims a measurement the device never
  makes.

## R4. File names, and hiding States CSVs

- **Decision**:
  - A new parser type, `StatesCsvFileName`, holds the `.states.csv` suffix.
    - `For("2026-11-01_x.csv")` returns `"2026-11-01_x.states.csv"`.
    - `IsStatesCsvName(name)` is true when the name ends in `.states.csv`, ignoring case.
  - `CsvFileNameRule.IsPlainCsvName` also rejects every States CSV name. That one change hides
    States CSVs from the home page list, the selection form, and `csvFilename` (FR-015), because
    all three already go through that rule.
- **Rationale**:
  - The converter (parser) and the store (Web) need the same name, so it lives in the parser library.
  - The check ignores case because the file system does (facts table). Without that,
    `x.STATES.csv` would pass the rule and open the States CSV as the current CSV.
- **Alternatives considered**: *A separate filter in `ListCsvFiles`*: the form and the state file
  would still accept the name.

## R5. The state file

- **Decision**:
  - `TcxToolState` gains `statesCsvFilename`, omitted when null (`WhenWritingNull`).
  - `CurrentCsvStore.WriteAsync` takes both names.
  - `SelectAsync` writes `statesCsvFilename = StatesCsvFileName.For(csv)` when that file exists in
    the CSV directory, and leaves it out otherwise (FR-014).
  - `RecordConversionAsync` calls `SelectAsync`, as it does now. The converter has just written the
    States CSV, so it is recorded (FR-013).
  - `CurrentCsv` gains `StatesFileName`. On read it is set only when `statesCsvFilename` is a string
    equal (ordinal) to `StatesCsvFileName.For(csvFilename)`; otherwise it is null (FR-016).
    - A missing property is normal and not logged.
    - A present but non-matching or non-string value logs the warning
      `statesCsvFilename does not match csvFilename`, with no file content.
  - Reading `csvFilename` is unchanged.
- **Rationale**:
  - The pair is written together, so FR-014's "never a mismatched pair" holds by construction.
  - Requiring an exact match also rejects any path or other name in the property, so the
    containment check `csvFilename` needs is not repeated.
- **Alternatives considered**: *Derive the States CSV name from `csvFilename` and ignore the
  property*: the draft asks for the property, and a hand-edited mismatch would go unnoticed.

## R6. Writing both files atomically

- **Decision**: The converter's existing two-phase write grows from 1 to 2 files per activity.
  - All temps are written first, then all are moved into place.
  - Any I/O failure deletes every temp and every file already moved, as it does now, and returns
    `OutputFailed`.
- **Rationale**: A conversion never leaves an activity CSV without its States CSV, and an existing
  pair is never half-replaced unless a move itself fails part-way (as today).
- **Alternatives considered**: *Write the States CSV after the activity CSV has been moved*: a
  failure would leave a new CSV with no States CSV and an old States CSV beside it.

## R7. The conversion result

- **Decision**:
  - `SavedCsv(FileName, RowCount)` becomes
    `SavedCsv(FileName, RowCount, StatesFileName, StateSetCount)`.
  - `ConversionResultView` lists two items per activity: the existing line for the activity CSV,
    unchanged, then `<states name>: 1 StateSet`, `<states name>: N StateSets` for 2 or more, or
    `<states name>: contains no StateSets` for 0 (FR-011).
- **Rationale**: The page already lists one line per saved file. Keeping the activity line's text
  unchanged keeps the feature 002 tests and contract.
- **Alternatives considered**: *A separate `SavedStatesCsv` list on `ConversionResult`*: the pairing
  of the two files would have to be rebuilt by index.

## R8. Reading the States CSV

- **Decision**:
  - A new `StatesCsvReader.ReadAsync(TextReader, CancellationToken)` returns a `StatesCsvReadResult`:
    either `StatesCsvRow`s with every field as written, or an `ActivityCsvError` (`HeaderMismatch`
    or `FieldCount`, with a line number).
  - Both readers share an internal line reader. It checks the exact header, splits on `|`, checks
    the field count, and reports the line number.
  - `ActivityCsvReader` checks the 19-field header, so a 17-field CSV is `HeaderMismatch` at line 1
    with no extra code (FR-017).
- **Rationale**:
  - One rule for "a CSV this app writes" (BOM, `\r\n`, `\r`, blank lines as field-count errors).
    Feature 005 already tests it for the activity CSV.
  - `ActivityCsvError` already describes exactly these two failures. A copy under another name would
    add two types and the same tests.
- **Alternatives considered**:
  - *Rename the error types to a neutral name*: churn in feature 005's code and log messages for no
    change in behavior.
  - *A second hand-written reader*: duplicated edge-case handling.

## R9. The States page's data

- **Decision**: `Analysis/CurrentStatesLoader` (singleton) returns a `CurrentStates` with one of
  these statuses:
  - `NoCurrentCsv` when `CurrentCsvStore` returns `None`.
  - `NoStatesCsv` (with the CSV name) when `StatesFileName` is null.
  - `Missing`, `Unreadable`, or `WrongFormat` (with the States CSV name), on the same exceptions and
    with the same warning logs as `CurrentActivityLoader`.
  - `NoStateSets` for a header-only file.
  - `Loaded` with the rows otherwise.

  The open-and-classify code is shared:
  - It moves from `CurrentActivityLoader` into one internal helper, `Analysis/AppCsvFile`, which
    both loaders call: open with `FileShare.ReadWrite | FileShare.Delete`, decode UTF-8 with BOM
    detection, and map `FileNotFound` / `DirectoryNotFound` to missing and other `IOException` /
    `UnauthorizedAccess` to unreadable.
  - A current CSV whose own file is `Missing` still has its States CSV shown, because the States
    page reads only the States CSV.
- **Rationale**:
  - The same shape as feature 005, so the pages behave alike (FR-022, FR-024).
  - One copy of the I/O classification that feature 005 verified with the lock and dangling-link
    tests.
- **Alternatives considered**: *Extend `CurrentActivityLoader` to load both files*: the Table page
  would pay for a file it does not show, and the statuses would mix.

## R10. Table page changes

- **Decision**:
  - `TrackpointBands.RunCadenceClass` drops the doubling. Its edges become 20, 150, and 180 on the
    value as written (FR-018). The heart-rate bands are unchanged.
  - The Table page's `WrongFormat` notice gains `<a href="convert">Convert its TCX file again</a>.`
    The `Unreadable` notice is unchanged (FR-017).
  - The Table page keeps its 10 columns. The `ActivityCsvRow` it receives simply has two more
    properties.
- **Rationale**: The value now is steps per minute, so the band rule applies to it directly. A
  wrong-format file is, in practice, a CSV from before this feature, and reconverting it is the fix.
- **Alternatives considered**: *Keep doubling for old files*: rejected by Clarification Q1.

## R11. The States page

- **Decision**:
  - The page is `/states`, with `StatesPage.razor` and a `.razor.cs` code-behind, statically
    rendered. Its table is the `Components/States/StatesTable.razor` component.
  - The table uses the Table page's markup and classes, with 13 headings, inside
    `div.table-responsive.states-table-scroll`.
  - `app.css` gains `.states-table-scroll` and `.states-table-scroll thead th`, with the same values
    as the Table page's rules: `max-height: 75vh`, `overflow-y: auto`, and `position: sticky`,
    `top: 0`, `z-index: 1`.
  - AverageHeartRateBpm cells use `TrackpointBands.HeartRateClass`, and AverageRunCadence cells use
    `TrackpointBands.RunCadenceClass` (FR-021). The band classes in `app.css` are reused unchanged.
  - The navbar gets `<NavLink class="nav-link" href="states">States</NavLink>` straight after Table.
  - The page shows `States CSV: <strong>name</strong>` above the table, and notices like the Table
    page's.
- **Rationale**:
  - About 540 rows is small, but global CSS and the 005 table markup keep the two pages consistent,
    and the 005 lessons apply as they are.
  - A separate class keeps feature 005's style test, which checks for exactly one
    `.trackpoint-table-scroll` rule, unchanged.
- **Alternatives considered**:
  - *Share one renamed scroll class*: it changes a feature 005 contract and test for two rules'
    worth of saving.
  - *Interactive sorting*: not asked for, and it would break static SSR (FR-019).

## R12. Fixtures and golden files

- **Decision**:
  - `Fixtures/generate_success_fixtures.py` is extended. It writes the 19-field activity CSVs and,
    for every success fixture, a `<name>.states.csv` golden, calculated in Python independently of
    the C# code.
  - The generator uses explicit `+=` loops for averages, never `sum()` (facts table).
  - It uses `timedelta` arithmetic for StateSeconds, the same exact-then-round rule as R2, and
    `repr`-based shortest decimals, as it does now.
  - A new success fixture, `run-states.tcx`, covers:
    - cadence 0, 1, 74, 75, 90, and missing (`stopped` / `walking` / `walking` / `running` /
      `running` / `unknown`)
    - a one-row StateSet
    - a timed row followed by an untimed row inside one set
    - a StateSet with no distance
    - a set boundary with a gap of more than 1 s
    - fractional seconds
  - The existing 7 activity goldens are regenerated, and the existing `.tcx` fixtures do not change.
  - Fixture tests that list golden files keep activity CSVs and States CSVs apart.
- **Rationale**: CLAUDE.md, feature 002: golden files come from an independent calculation. The
  generator already reproduces the checked-in files byte for byte. Extending it keeps one source.
- **Alternatives considered**: *Hand-written expected values*: error-prone for averages with
  17-digit results.

## R13. Tests that change because of the format

These tests change with the format; nothing else in the suite depends on it.

- **Parser**: `ActivityCsvWriterTests` (19 fields), `ActivityCsvReaderTests` (19 fields, a 17-field
  file is `HeaderMismatch`), and `TcxCsvConverterTests`.
  - Output directories now hold 2 files per activity.
  - `SavedCsv` gains two values.
  - A failure leaves neither file behind.
- **Support**: `ActivityCsvFixture.Line` gains `state` and `stateSeconds`, with defaults `walking`
  and `0`, and writes 19 fields.
- **Web**:
  - `TrackpointTableTests` (19-argument row), `TrackpointBandsTests` (cadence edges without
    doubling), and `CurrentActivityLoaderTests` (a 19-field marker row).
  - `TablePageTests` and `TablePageEndpointTests`: the cadence band expectations change, and the
    wrong-format notice gains its link.
  - `ConversionResultViewTests`, `ConvertEndpointTests`, and `CurrentCsvStoreTests`: the new
    property, the hidden names, and the result lines.
  - `MainLayoutTests`: the navbar order.
- Feature 005's guard tests (no writes, static rendering, no empty `class`) are reused for the
  States page.
