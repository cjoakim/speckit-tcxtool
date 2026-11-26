---

description: "Task list for feature 006-activity-states"
---

# Tasks: Activity States

**Input**: Design documents from `specs/006-activity-states/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included. The constitution requires test-first (Principle V, NON-NEGOTIABLE).

- Every test task comes before the code it covers, and must be run and seen failing first.
- A test that fails to compile because its subject type or member does not exist yet counts as
  failing. Add the subject in a later task of the same phase.
- If a test passes before its code is written (a guard), show it failing with a temporary break in
  that code. Revert the break in the same task, with an edit rather than by restoring a backup
  (`CLAUDE.md`, lessons from 001, 002, and 005).
- Expected files come only from `Fixtures/generate_success_fixtures.py`, never from running the
  converter (research.md R12).

**Organization**:
- Setup checks the baseline.
- There is no separate foundational phase. US1 changes the activity CSV format, which US2 and US3
  both build on, so the stories are done in order:
  - US1: the 19-field activity CSV, plus the Table page's cadence bands and wrong-format notice
  - US2: the States CSV, the conversion result, and the state file
  - US3: the States page

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task belongs to (US1, US2, US3)
- All paths are relative to `tcxtool/`. Run every command from `tcxtool/`. The git root is the
  parent directory.

## Path Conventions

These come from plan.md → Project Structure:

- Parser: `src/JoakimSoftware.TcxTool.Parser/{Csv,Conversion}/`
- Web: `src/JoakimSoftware.TcxTool.Web/{State,Analysis,Table,Components/{Pages,States,Conversion,Layout},wwwroot}/`
- Tests: `tests/JoakimSoftware.TcxTool.Tests/{Fixtures,Parser,Support,Web,Web/State,Web/Analysis,Web/Table,Web/States}/`

**Conventions for every task**:

- **Code rules**:
  - one public type per file, file-scoped namespaces
  - `_camelCase` only for instance fields; static fields PascalCase; local constants camelCase
  - `Async` methods take a `CancellationToken`, and every `async Task` test name ends in `Async`
    (IDE1006)
  - every public parser type and member has an XML doc comment (`GenerateDocumentationFile`)
  - no relaxed settings or suppressed rules
- **Namespace traps**: no Web namespace or folder named `Activity` or `Convert` (CLAUDE.md lessons
  from 002 and 005). The new folder is `Components/States/`, and the page class is `StatesPage`.
- **Test isolation (FR-026)**:
  - HTTP tests get their host only from `Support/TcxToolWebFactory`.
  - bUnit tests register services only through `Support/BunitTcxToolServices`.
  - Store and loader tests build `TcxToolOptions` whose paths are inside a `TempDirectory`.
  - No test reads `data/`, `specs/`, `.gitignore`, or the real `tcxtool-state.json`.
- **I/O failures in tests**: the Compose test container runs as root, so never use `chmod`. Use
  CLAUDE.md's techniques:
  - **Unreadable**: hold the file with `FileShare.None`.
  - **Deleted after the check**: make the file a dangling symbolic link.
  - **Write failure**: make the target path a directory.
- **Real data**: never copy rows from `data/csv/` or `data/tcx/` into a test, fixture, or log. They
  hold real GPS positions. All test values are made up.
- **Messages**: user-facing text comes only from `CurrentActivityMessages`, `CurrentStatesMessages`,
  and the markup in contracts/pages.md. Tests compare against those.
- **bUnit and async loads**: pages load in `OnInitializedAsync` with async file I/O. Wait with
  `WaitForElement` or `WaitForAssertion` before asserting, as `TablePageTests` does.
- **Gates**: check `dotnet format`'s exit code directly, never through a pipe.

---

## Phase 1: Setup

**Purpose**: a green baseline, so every later failure is one this feature caused.

- [X] T001 Run quality gates 1–3: `dotnet build JoakimSoftware.TcxTool.slnx`,
  `dotnet format JoakimSoftware.TcxTool.slnx --verify-no-changes`, and
  `dotnet test JoakimSoftware.TcxTool.slnx`. All must pass before any change. Record the test count.

**Checkpoint**: the suite is green.

---

## Phase 2: User Story 1 - The activity CSV records the runner's state on every trackpoint (Priority: P1) 🎯 MVP

**Goal**: every activity CSV has 19 fields, with RunCadence doubled and State and StateSeconds after
it. The Table page colors cadence without doubling, and calls a pre-006 CSV the wrong format with a
link to convert again (spec FR-001 to FR-006, FR-017, FR-018; contracts/csv-format.md;
contracts/pages.md "Table page").

**Independent Test**: convert `run-states.tcx`. Its CSV matches `Expected/run-states.csv` byte for
byte. `/table` colors a `180` cadence cell `cadence-band-4`, and shows the wrong-format notice with
its link for a 17-field CSV.

### Tests for User Story 1 ⚠️

- [X] T002 [US1] Extend `tests/JoakimSoftware.TcxTool.Tests/Fixtures/generate_success_fixtures.py`
  for the 19-field activity CSV (contracts/csv-format.md; research.md R2, R3, R12). Do not add
  States goldens yet (T018).
  - **Header**: `HEADER` becomes the 19-field header.
  - **RunCadence**: `fmt(float(cad) * 2)`, or empty when there is no cadence.
  - **State**: `stopped` for 0, `walking` for greater than 0 and less than 150, `running` for 150
    or more, and `unknown` for empty or negative.
  - **StateSeconds**: `timedelta` arithmetic. Keep the elapsed `timedelta` of the last timed row.
    When a row's State differs from the previous row's, the set start becomes that last timed
    elapsed, or `timedelta(0)` when there is none. A timed row's StateSeconds is
    `fmt((elapsed - start).total_seconds())`; an untimed row's is empty.
  - **New fixture**: add `run_states()` and its `run-states` entry in the main fixture list. It
    must cover:
    - TCX cadence 0, 1, 74, 75, 90, and missing
    - a one-row StateSet
    - a timed row then an untimed row inside one set
    - a StateSet in which no row has a DistanceMeters
    - a set boundary with a gap of more than 1 s
    - fractional-second times
    - HeartRateBpm, position, and altitude on most rows, with some missing

    All values are made up.
  - **Regenerate** with the README command, with `run-states` added to its list.
  - **Check** with `git status`/`git diff --stat` that the existing `.tcx` fixtures are
    byte-identical and only `Expected/*.csv`, `run-states.tcx`, and `Expected/run-states.csv`
    changed.
  - **README**: update `tests/JoakimSoftware.TcxTool.Tests/Fixtures/README.md` (19 fields, the
    `run-states.tcx` row, and the new command).
  - **Expected failures**: the golden comparisons in `ActivityCsvWriterTests`,
    `TcxCsvConverterTests`, `ConvertEndpointTests`, and `ActivityCsvReaderTests` now fail.
- [X] T003 [P] [US1] Create `tests/JoakimSoftware.TcxTool.Tests/Parser/ActivityStateTests.cs` for
  `ActivityState` (data-model.md `ActivityState.For`, research.md R3).
  - The four name constants and `RunningStepsPerMinute` = 150.
  - `For(0)` is `stopped`; `For(2)`, `For(148)`, and `For(149.999)` are `walking`; `For(150)` and
    `For(198)` are `running`; `For(null)` and `For(-2)` are `unknown`.
- [X] T004 [P] [US1] Update `tests/JoakimSoftware.TcxTool.Tests/Parser/ActivityCsvWriterTests.cs`
  (contracts/csv-format.md):
  - The header equals the exact 19-field line, and every line of every golden case has 19 fields.
  - `run-states.csv` joins the golden cases.
  - **Inline activities**:
    - cadence 59 → `118` and 59.5 → `119`
    - the worked example (70, 70, 80, 80, missing at 1 s steps) gives States
      `walking, walking, running, running, unknown` and StateSeconds `0, 1, 1, 2, 1`
    - an untimed row has an empty StateSeconds, and the next set starts at the last timed row
      before it
    - times at `.3` s and `3725.6` s in one set give StateSeconds `3725.3`, with no float noise
  - `Write(Rows(activity, id), writer)` produces exactly the text of `Write(activity, id, writer)`,
    and `Rows` returns one `ActivityCsvRow` per trackpoint.
- [X] T005 [P] [US1] Update `tests/JoakimSoftware.TcxTool.Tests/Parser/ActivityCsvReaderTests.cs`
  (contracts/csv-format.md "Older files"):
  - Add `run-states.csv` to `GoldenFiles`.
  - The field-mapping test covers `State` and `StateSeconds`.
  - The field-count cases become 18, 20, and a blank line.
  - Feature 002's exact 17-field header line is `HeaderMismatch` at line 1.
- [X] T006 [P] [US1] Update `tests/JoakimSoftware.TcxTool.Tests/Parser/TcxCsvConverterTests.cs`:
  converting `run-states.tcx` writes bytes equal to `Expected/run-states.csv`. US2 changes the file
  list and `SavedCsv` (T021).
- [X] T007 [US1] Update the 19-field test inputs (data-model.md `ActivityCsvRow`):
  - **`tests/JoakimSoftware.TcxTool.Tests/Support/ActivityCsvFixture.cs`**: `Line(...)` gains
    `state` (default `walking`) and `stateSeconds` (default `0`), placed after `runCadence`, and
    writes 19 fields. `Synthetic(count)` gives each row a State and StateSeconds. Update the XML doc
    comments.
  - **`Web/Table/TrackpointTableTests.cs`**:
    - `Row(...)` builds 19 arguments.
    - `EachCellHoldsItsColumnsFieldExactly` passes 19 `f`-marked values, with `f-state` and
      `f-seconds` for the two new fields, and still expects the same 10 cells.
    - The band test's cadences become `19, 20, 150, 180`, with the same expected classes (FR-018).
  - **`Web/Analysis/CurrentActivityLoaderTests.cs`**:
    - The short marker row becomes 18 fields.
    - Every hand-built line is 19 fields.
- [X] T008 [P] [US1] Update the cadence band expectations (contracts/pages.md "Table page", FR-018)
  in `tests/JoakimSoftware.TcxTool.Tests/Web/Table/TrackpointBandsTests.cs`: band 1 for 0, 19, and
  19.99; band 2 for 20, 100, and 149.5; band 3 for 150, 176, and 179.5; band 4 for 180 and 250.
  `NoBand` is unchanged.
- [X] T009 [P] [US1] Update `tests/JoakimSoftware.TcxTool.Tests/Web/TablePageTests.cs` and
  `Web/TablePageEndpointTests.cs` for the wrong-format notice (contracts/pages.md "Table page",
  FR-017):
  - The `WrongHeader` setup's notice contains `<a href="convert">Convert its TCX file again</a>`.
  - The `LockedFile` (Unreadable) notice has no `a` element.
  - A new HTTP test writes, with `ActivityCsvFixture.WriteRaw`, feature 002's 17-field header and
    one 17-field line. `/table` shows the wrong-format notice with the link and no
    `id="trackpoints"`.
  - `TheBandClassesReachTheBrowserAsync` uses `runCadence: "176"` and expects
    `<td class="cadence-band-3">176</td>` (FR-018).
- [X] T010 [US1] Run `dotnet build JoakimSoftware.TcxTool.slnx`. **Expected**: the test project fails
  to compile, because `ActivityState`, `ActivityCsvWriter.Rows`, and the 19-argument `ActivityCsvRow`
  do not exist yet. That counts as failing for T003–T008. The individual failures of T002, T004–T006,
  T008, and T009 are confirmed after T012.

### Implementation for User Story 1

- [X] T011 [US1] Create `src/JoakimSoftware.TcxTool.Parser/Csv/ActivityState.cs` with the members in
  data-model.md `ActivityState` (`ActivityState.For`; research.md R3).
- [X] T012 [US1] Change `src/JoakimSoftware.TcxTool.Parser/Csv/ActivityCsvRow.cs` to the 19 fields
  of data-model.md `ActivityCsvRow`, adding `ActivityCsvRow.State` and `ActivityCsvRow.StateSeconds`
  after `ActivityCsvRow.RunCadence`, with XML docs. Then:
  1. Change `ActivityCsvReader.cs` to map the two new fields and to check 19 fields (its own
     constant for now; T014 switches it to the writer's).
  2. Build and run the suite.
  3. Record which tests fail. **Expected**: T003 passes; T002, T004–T006, T008, and T009 fail for
     their own reasons. T005 fails on the header, which is still the 17-field one until T013.
     **Done note**: the test project could not compile until `ActivityCsvWriter.Rows` existed
     (T013), so the first suite run after this task was after T013. There, every parser test passed,
     and the 11 failures were the band and Table page tests that T015 and T016 cover.
- [X] T013 [US1] Change `src/JoakimSoftware.TcxTool.Parser/Csv/ActivityCsvWriter.cs` (data-model.md
  `ActivityCsvWriter`; research.md R1–R3):
  - `Header` is the 19-field line, and add `FieldCount` = 19.
  - `Rows(activity, activityId)` builds the rows:
    - `ActivityCsvRow.RunCadence` is `DerivedNumber.Format(Value * 2)`.
    - `ActivityCsvRow.State` is `ActivityState.For`.
    - `ActivityCsvRow.StateSeconds` is calculated from `TimeSpan`s with the "StateSet start" rule
      in data-model.md, through `ElapsedTime.FormatSeconds`.
  - `Write(rows, output)` cleans and writes the rows.
  - `Write(activity, activityId, output)` becomes `Write(Rows(...), output)` and returns the count.
  - T004, T005, and T006 pass, and `ConvertEndpointTests`' golden comparison passes.
- [X] T014 [US1] Change `src/JoakimSoftware.TcxTool.Parser/Csv/ActivityCsvReader.cs` to use
  `ActivityCsvWriter.FieldCount` instead of its own constant, and update its XML docs. This is a
  refactor: T005 stays green.
- [X] T015 [US1] Change `src/JoakimSoftware.TcxTool.Web/Table/TrackpointBands.cs` per data-model.md
  `TrackpointBands.RunCadenceClass`: no doubling, with edges 20, 150, and 180. Rename the cadence
  constants and update the XML docs, which no longer mention per-foot cadence. T008 passes, and so
  do the band checks added in T007 and T009.
- [X] T016 [US1] Change `src/JoakimSoftware.TcxTool.Web/Components/Pages/TablePage.razor`: split the
  `Unreadable` and `WrongFormat` cases, and give `WrongFormat` the exact notice in
  contracts/pages.md "Table page". T009 passes.
- [X] T017 [US1] Run gates 1–3. All pass, and the test count is at least T001's.

**Checkpoint**: converting writes 19-field CSVs, and the Table page handles new and old CSVs.

---

## Phase 3: User Story 2 - Each state change is summarized in a States CSV (Priority: P2)

**Goal**: each conversion also writes `<name>.states.csv`, lists it in its result, and records it in
the state file. The home page never offers it as a CSV (spec FR-007 to FR-016;
contracts/states-csv-format.md, state-file.md, pages.md "Convert page result").

**Independent Test**: convert `run-states.tcx`. The folder holds both files, the States CSV equals
`Expected/run-states.states.csv`, the result lists both, `tcxtool-state.json` names both, and the
home page lists only the activity CSV.

### Tests for User Story 2 ⚠️

- [X] T018 [US2] Extend `generate_success_fixtures.py` to write, for every activity golden
  `Expected/<stem>.csv`, a States golden `Expected/<stem>.states.csv`
  (contracts/states-csv-format.md; research.md R2, R12).
  - Build it from the activity rows the script already computes, as strings, using the 13-field
    header.
  - Group consecutive rows by State.
  - Copy the last non-empty `Time`, `StateSeconds`, and `ElapsedTime`.
  - Means are an explicit `+=` loop over the non-empty values parsed with `float`, divided by the
    count. **Never `sum()`**, which is compensated in Python 3.12 and later.
  - DistanceMiles is the last non-empty value in the set minus the last non-empty value before it
    (0.0 when none), through `fmt`.
  - Regenerate. **Check** that `git diff --stat` shows the activity goldens unchanged since T002,
    and that `Expected/` gains 8 `*.states.csv` files. The no-trackpoints one is header-only.
  - Update the README.
- [X] T019 [P] [US2] Create `tests/JoakimSoftware.TcxTool.Tests/Parser/StatesCsvFileNameTests.cs`
  (data-model.md `StatesCsvFileName.For`, `StatesCsvFileName.IsStatesCsvName`):
  - `For` maps the single- and multi-activity names in contracts/states-csv-format.md, and throws
    `ArgumentException` for `x.txt`.
  - `IsStatesCsvName` is true for `a.states.csv` and `A.STATES.CSV`, and false for `a.csv`, `null`,
    and `a.states.csv.bak`.
- [X] T020 [P] [US2] Create `tests/JoakimSoftware.TcxTool.Tests/Parser/StatesCsvWriterTests.cs`
  (contracts/states-csv-format.md; data-model.md `StatesCsvRow`):
  - **Header**: equals the exact 13-field line.
  - **Golden theory** over the 8 activity goldens: read each with `ActivityCsvReader`, pass its rows
    to `StatesCsvWriter.Rows`, then `Write`. The text equals `<stem>.states.csv`.
  - **Built-row cases**, using `ActivityCsvRow`s:
    - The US2 worked example, 38 walking then 113 running: 2 rows, with end trackpoints `38` and
      `151`, StateElapsedSeconds `37` and `113`, and ActivityElapsedTime `00:00:37` and `00:02:30`.
    - A one-row set's averages are its own values.
    - A set whose rows have no Time has empty StateEndDate, StateElapsedSeconds, and
      ActivityElapsedTime.
    - A set with no DistanceMiles has an empty DistanceMiles, and the next set measures from the
      last value before it.
    - Empty values are left out of averages, and an all-empty column gives an empty field.
    - An `unknown` run is its own row.
    - Empty input gives no rows, and `Write` writes only the header.
- [X] T021 [P] [US2] Update `tests/JoakimSoftware.TcxTool.Tests/Parser/TcxCsvConverterTests.cs`
  (research.md R6, R7):
  - Successful conversions list both files in `_output.FileNames()`.
  - `SavedCsv` values have four arguments, for example
    `new SavedCsv("2026-11-01_run-two-laps.csv", 10, "2026-11-01_run-two-laps.states.csv", 1)`.
  - States bytes equal the goldens for `run-states.tcx` and both `multi-activity` CSVs.
  - Converting twice gives byte-identical States CSVs.
  - A new failure test makes the `.states.csv` target a directory. **Expected**: `OutputFailed`,
    and neither the new activity CSV nor any `.tmp` file remains.
- [X] T022 [P] [US2] Update `tests/JoakimSoftware.TcxTool.Tests/Web/ConversionResultViewTests.cs`
  (contracts/pages.md "Convert page result"):
  - Two `li` per activity, in order.
  - The States line reads `1 StateSet`, `3 StateSets`, or `contains no StateSets`.
  - The activity line is unchanged.
- [X] T023 [P] [US2] Update `tests/JoakimSoftware.TcxTool.Tests/Web/State/CurrentCsvStoreTests.cs`
  (contracts/state-file.md):
  - **Written content**: `SelectAsync` writes exactly the two-property content when the States CSV
    exists, and feature 004's content when it does not. `RecordConversionAsync` writes both.
  - **Read rules**: every row of the "Read rules (added)" table. A missing property gives null with
    no log. A matching name is returned. Non-string, other-name, and path values give null with the
    warning and leave `Status` unchanged.
  - **Hidden names**: `ListCsvFiles` leaves out `x.states.csv` and `y.STATES.csv`. `SelectAsync`
    of a States name returns `InvalidName`. A state file whose `csvFilename` is a States name gives
    `None` with the existing warning.

  These fail before the code: States names currently pass the name rule, so no temporary break is
  needed.
- [X] T024 [P] [US2] Update `tests/JoakimSoftware.TcxTool.Tests/Web/ConvertEndpointTests.cs` and
  `Web/HomeSelectEndpointTests.cs`:
  - After a successful POST, the CSV directory holds both files.
  - The state file names both.
  - The result HTML has the States `li`.
  - `GET /` with a States CSV in the folder does not list it.
- [X] T025 [US2] Build and run the suite. Confirm T019–T024 fail: by compile error for the missing
  types and members, then, once those exist, each for its own reason.
  **Done note**: T019–T024 failed to compile (`StatesCsvRow`, the 4-argument `SavedCsv`, and
  `CurrentCsv.StatesFileName` were missing). The test files only compiled once T026–T031 were all in,
  so the hidden-name tests were also shown failing with a temporary break: without the States check
  in `CsvFileNameRule`, 7 cases failed (`CsvFileNameRuleTests`, `CurrentCsvStoreTests`, and
  `HomeSelectEndpointTests`). The break was reverted with an edit, and 478 tests passed.

### Implementation for User Story 2

- [X] T026 [P] [US2] Create `src/JoakimSoftware.TcxTool.Parser/Csv/StatesCsvFileName.cs` per
  data-model.md `StatesCsvFileName` (`StatesCsvFileName.For`, `StatesCsvFileName.IsStatesCsvName`
  ignoring case; research.md R4). T019 passes.
- [X] T027 [P] [US2] Create `src/JoakimSoftware.TcxTool.Parser/Csv/StatesCsvRow.cs`, a positional
  record of the 13 string fields in data-model.md `StatesCsvRow`, in contract order, with XML docs.
- [X] T028 [US2] Create `src/JoakimSoftware.TcxTool.Parser/Csv/StatesCsvWriter.cs` (data-model.md
  `StatesCsvWriter`; research.md R2).
  - `Header` and `FieldCount` = 13.
  - `Rows(rows)` groups by `ActivityCsvRow.State` and fills the fields:
    - `StatesCsvRow.ActivityId`, `StatesCsvRow.State`, and `StatesCsvRow.StateEndTrackpointNum`
    - `StatesCsvRow.StateEndDate`, `StatesCsvRow.StateElapsedSeconds`, and
      `StatesCsvRow.ActivityElapsedTime`, copied as text
    - `StatesCsvRow.AverageLatitude`, `StatesCsvRow.AverageLongitude`,
      `StatesCsvRow.AverageHeartRateBpm`, `StatesCsvRow.AverageRunCadence`,
      `StatesCsvRow.AverageAltitudeMeters`, and `StatesCsvRow.AverageAltitudeFeet`, each an explicit
      left-to-right loop
    - `StatesCsvRow.DistanceMiles`, one subtraction
  - `Write(rows, output)` applies `CsvText.Clean`.
  - Parse with `NumberStyles.Float` and the invariant culture, counting finite values only. T020
    passes.
- [X] T029 [US2] Change `src/JoakimSoftware.TcxTool.Parser/Conversion/SavedCsv.cs` to add
  `SavedCsv.StatesFileName` and `SavedCsv.StateSetCount` (data-model.md), with XML docs. Change
  `Conversion/TcxCsvConverter.cs` to render the activity rows once, then:
  - render the States CSV from them,
  - add both to the two-phase write (research.md R6), and
  - return the four-value `SavedCsv`.

  Fix any other compile errors from the record change. T021 passes.
- [X] T030 [US2] Change `src/JoakimSoftware.TcxTool.Web/Components/Conversion/ConversionResultView.razor`
  to add the States `li` with the singular, plural, and zero texts (contracts/pages.md). T022 passes.
- [X] T031 [US2] Change the state code (contracts/state-file.md; research.md R4, R5):
  - **`src/JoakimSoftware.TcxTool.Web/State/TcxToolState.cs`**: add `TcxToolState.StatesCsvFilename`
    (`statesCsvFilename`, `WhenWritingNull`; data-model.md).
  - **`State/CurrentCsv.cs`**: add `CurrentCsv.StatesFileName`, with the optional argument on
    `Present` and `Missing` (data-model.md).
  - **`State/CurrentCsvStore.cs`**:
    - `ReadAsync` applies the added read rules, with a new `LoggerMessage` for the mismatch warning.
    - `WriteAsync` takes both names.
    - `SelectAsync` writes `StatesCsvFileName.For(name)` only when that file exists.
  - **`State/CsvFileNameRule.cs`**: `IsPlainCsvName` rejects `StatesCsvFileName.IsStatesCsvName`
    names.

  Update the XML docs. T023 and T024 pass.
- [X] T032 [US2] Run gates 1–3. All pass.

**Checkpoint**: each conversion writes and records both files, and the home page offers only
activity CSVs.

---

## Phase 4: User Story 3 - Read the state changes on a States page (Priority: P3)

**Goal**: `/states`, linked straight after Table, shows the current CSV's States CSV as a sticky
header table with band-colored averages, or a notice (spec FR-019 to FR-025; contracts/pages.md
"States page" and "Navbar").

**Independent Test**: convert `run-states.tcx` in an HTTP test host, then `GET /states`. The table
has the 13 headings and one row per States CSV data row, the average cells carry their band
classes, and every notice case returns 200 with its notice.

### Tests for User Story 3 ⚠️

- [X] T033 [P] [US3] Create `tests/JoakimSoftware.TcxTool.Tests/Parser/StatesCsvReaderTests.cs`,
  mirroring `ActivityCsvReaderTests` (data-model.md "Readers"):
  - A golden round trip over the 8 `*.states.csv` files.
  - The field mapping.
  - A header-only file, `\r\n` and `\r` endings, and a BOM.
  - Mismatched headers, including the activity CSV header, give `HeaderMismatch` at line 1.
  - 12 fields, 14 fields, and a blank line at line 4 give `FieldCount` at line 4.
  - A null input throws. A cancelled token throws.
- [X] T034 [P] [US3] Create `tests/JoakimSoftware.TcxTool.Tests/Support/StatesCsvFixture.cs`, a
  test helper with XML docs. Then extend `ActivityCsvFixture.WriteState` with an optional
  `statesCsvFileName` argument, which writes the two-property content of contracts/state-file.md.
  - `Line(...)` has 13 named optional parameters with made-up defaults: ActivityId `fixture-run`,
    State `walking`, StateEndDate `2026-11-01T06:00:09.000Z`, StateEndTrackpointNum `10`,
    StateElapsedSeconds `9`, ActivityElapsedTime `00:00:09`, AverageLatitude `1.5`,
    AverageLongitude `2.5`, AverageHeartRateBpm `120`, AverageRunCadence `140`, DistanceMiles
    `0.01`, AverageAltitudeMeters `10`, and AverageAltitudeFeet `32.808398950131235`.
  - `Write(path, lines)` writes `StatesCsvWriter.Header` and the lines, each followed by `\n`,
    UTF-8 with no BOM.
  - `Synthetic(count)` gives rows that vary.

  This is support code: it compiles, and the suite stays as it was.
- [X] T035 [P] [US3] Create
  `tests/JoakimSoftware.TcxTool.Tests/Web/Analysis/CurrentStatesMessagesTests.cs`: every
  `CurrentStatesStatus` maps to its text in data-model.md `CurrentStatesMessages`, and an
  out-of-range value throws `ArgumentOutOfRangeException`.
- [X] T036 [P] [US3] Create
  `tests/JoakimSoftware.TcxTool.Tests/Web/Analysis/CurrentStatesLoaderTests.cs` (contracts/pages.md
  "States page"; data-model.md `CurrentStates.Status`, `CurrentStates.CsvFileName`,
  `CurrentStates.StatesFileName`, `CurrentStates.Rows`):
  - **Statuses**:
    - no state file → `NoCurrentCsv`
    - no `statesCsvFilename` → `NoStatesCsv` with the CSV name
    - a mismatched name → `NoStatesCsv`
    - a missing file → `Missing`, and a dangling link → `Missing`
    - a `FileShare.None` lock → `Unreadable`, with a warning naming the file and `IOException`
    - a bad header → `WrongFormat`, with a warning naming `HeaderMismatch` and `line 1`
    - header only → `NoStateSets`
    - three lines → `Loaded`, with three rows in order
    - an activity CSV whose own file is missing still gives `Loaded`
  - **Logs** never contain the marker values `12.3456789` or `-65.4321987`, placed in the bad
    file's rows.
  - **Invariant**: `Rows.Count > 0` exactly when the status is `Loaded`.
  - **No writes**: snapshot the temp directory's bytes and mtimes across two loads. This guard
    passes as soon as the loader exists, so show it failing with a temporary append to the States
    CSV inside the loader during T046, then revert with an edit.
- [X] T037 [P] [US3] Create `tests/JoakimSoftware.TcxTool.Tests/Web/States/StatesTableTests.cs`,
  bUnit with `parameters.Add(table => table.Rows, rows)` (contracts/pages.md "Table"):
  - **Structure**: `div.table-responsive.states-table-scroll > table#states`, with classes `table`,
    `table-sm`, `table-bordered`, `small`, and `mb-0`, and no striped or hover.
  - **Heading**: `thead.table-light` with 13 `th[scope=col]` in States CSV order.
  - **Body**: rows in order, cells exact, empty cells kept.
  - **Bands**: column 9 has `TrackpointBands.HeartRateClass` and column 10 has
    `TrackpointBands.RunCadenceClass`. Every other cell has no attributes.
  - **No empty class**: an empty or non-numeric average has no `class` attribute. This guard passes
    from the start, so show it failing with a temporary `?? ""` during T048, then revert with an
    edit.
- [X] T038 [P] [US3] Create `tests/JoakimSoftware.TcxTool.Tests/Web/StatesPageTests.cs`, bUnit
  through `BunitTcxToolServices` (contracts/pages.md "States page"):
  - every status shows its exact notice element, class, role, text, and links
  - `NoStateSets` and `Loaded` show `p#states-file` with the States CSV name
  - `Loaded` puts the table after `p#states-file`, checked with `Markup.IndexOf`
  - there is no table in any other status
- [X] T039 [P] [US3] Create `tests/JoakimSoftware.TcxTool.Tests/Web/StatesPageEndpointTests.cs`
  through `TcxToolWebFactory`:
  - **Basics**: with no state, the page returns 200 with `<title>States</title>`, the `h1`, and
    the `NoCurrentCsv` notice.
  - **Loaded**: 13 `<th scope="col">` headings in order, and the right number of body rows.
  - **Bands**: `<td class="hr-band-5">165.4</td>` and `<td class="cadence-band-3">172</td>` reach
    the browser.
  - **Guard, no writes**: the temp directory's bytes and mtimes are unchanged across two requests.
    Show it failing with a temporary append in `CurrentStatesLoader`, then revert with an edit.
  - **Guard, static**: the body has no `<!--Blazor:{`. Show it failing with a temporary
    `@rendermode InteractiveServer` on `StatesPage`, then revert with an edit.

  Do both guard breaks during T049.
- [X] T040 [P] [US3] Create `tests/JoakimSoftware.TcxTool.Tests/Web/StatesPagePerformanceTests.cs`,
  test `OneThousandStateSetsRenderInUnderFiveSecondsAsync`:
  1. Write `StatesCsvFixture.Synthetic(1_000)` with a matching state file.
  2. Send an untimed warm-up GET.
  3. Time a second GET.

  **Expected**: 200, `id="states"`, exactly 1,001 `<tr>`, and under 5 s (SC-004, server side).
- [X] T041 [P] [US3] Create `tests/JoakimSoftware.TcxTool.Tests/Web/StatesStylesTests.cs` from
  `RepositoryRoot.Combine("src/JoakimSoftware.TcxTool.Web/wwwroot/app.css")`. Strip `/* */`
  comments first. **Expected**:
  - exactly one `.states-table-scroll` rule, with `max-height: 75vh` and `overflow-y: auto`
  - exactly one `.states-table-scroll thead th` rule, with `position: sticky`, `top: 0`, and
    `z-index: 1`

  (contracts/pages.md "Styles").
- [X] T042 [P] [US3] Update `tests/JoakimSoftware.TcxTool.Tests/Web/MainLayoutTests.cs`:
  - the navbar's link texts are, in order, Home, Convert, Table, States, Histograms, Timeline, and
    About
  - the States link has `href="states"`
  - a `GET /states` through `TcxToolWebFactory` marks it active
- [X] T043 [US3] Build and run the suite. Confirm T033 and T035–T042 fail, by compile error or for
  their own reasons.
  **Done note**: the suite failed to compile (`StatesCsvReadResult`, `CurrentStates`,
  `CurrentStatesLoader`, `StatesPage`, and `StatesTable` were missing). After T044–T050, the only
  failures were the two `MainLayoutTests` navbar checks, which T051 fixed. Four guards were shown
  failing with temporary breaks, each reverted with an edit:
  - a one-byte append in `CurrentStatesLoader` failed both no-writes tests
  - `?? ""` in `StatesTable` failed both no-empty-`class` cases
  - `@rendermode InteractiveServer` on `StatesPage` failed the static-render test

  Gates 1–3 then passed with 557 tests.

### Implementation for User Story 3

- [X] T044 [US3] Create the internal `src/JoakimSoftware.TcxTool.Parser/Csv/PipeDelimitedLines.cs`,
  and move `ActivityCsvReader`'s line handling into it (research.md R8). This is a refactor:
  `ActivityCsvReaderTests` stays green with no test change. Then create `Csv/StatesCsvReadResult.cs`
  and `Csv/StatesCsvReader.cs` on top of it (data-model.md "Readers"), with XML docs. T033 passes.
- [X] T045 [US3] Create the internal `src/JoakimSoftware.TcxTool.Web/Analysis/AppCsvFile.cs`, and
  move `CurrentActivityLoader`'s open-and-classify code into it (data-model.md `AppCsvFile`;
  research.md R9). This is a refactor: `CurrentActivityLoaderTests` stays green with no test change.
- [X] T046 [US3] Create the analysis types:
  - `src/JoakimSoftware.TcxTool.Web/Analysis/CurrentStatesStatus.cs`.
  - `Analysis/CurrentStates.cs`, with a private constructor and factories for each status, setting
    data-model.md `CurrentStates.Status`, `CurrentStates.CsvFileName`, and
    `CurrentStates.StatesFileName`, and enforcing data-model.md `CurrentStates.Rows`.
  - `Analysis/CurrentStatesMessages.cs`, using the texts in data-model.md.
  - `Analysis/CurrentStatesLoader.cs`, a sealed partial class using `AppCsvFile`, with the two
    `LoggerMessage` warnings in data-model.md.

  Register `CurrentStatesLoader` as a singleton in `src/JoakimSoftware.TcxTool.Web/Program.cs`, and
  in `tests/JoakimSoftware.TcxTool.Tests/Support/BunitTcxToolServices.cs`. T035 and T036 pass, and
  the T036 guard break is done here.
- [X] T047 [US3] Add `@using JoakimSoftware.TcxTool.Web.Components.States` to
  `src/JoakimSoftware.TcxTool.Web/Components/_Imports.razor`.
- [X] T048 [US3] Create `src/JoakimSoftware.TcxTool.Web/Components/States/StatesTable.razor` per
  contracts/pages.md "Table":
  - one `@foreach` with inline cells
  - `class="@TrackpointBands.HeartRateClass(...)"` on column 9 and
    `class="@TrackpointBands.RunCadenceClass(...)"` on column 10
  - no `.razor.css`
  - `[Parameter, EditorRequired] IReadOnlyList<StatesCsvRow> Rows`

  T037 passes, and its guard break is done here.
- [X] T049 [US3] Create `src/JoakimSoftware.TcxTool.Web/Components/Pages/StatesPage.razor`
  (`@page "/states"`, `PageTitle`, `h1`, a `switch` over the status) and `StatesPage.razor.cs`
  (`[Inject] CurrentStatesLoader`, the cascading `HttpContext`, and `OnInitializedAsync`), per
  contracts/pages.md. No `@rendermode`. T038, T039, and T040 pass, and T039's two guard breaks are
  done here.
- [X] T050 [US3] Append the two `.states-table-scroll` rules to
  `src/JoakimSoftware.TcxTool.Web/wwwroot/app.css`, under a comment naming feature 006 and research
  R11. T041 passes.
- [X] T051 [US3] Add `<NavLink class="nav-link" href="states">States</NavLink>` after the Table link
  in `src/JoakimSoftware.TcxTool.Web/Components/Layout/MainLayout.razor`. T042 passes.
- [X] T052 [US3] Run gates 1–3. All pass.

**Checkpoint**: all three stories work.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T053 Update `CLAUDE.md`:
  - **Feature history**: add the `specs/006-activity-states` row.
  - **CSV contract**: point to `specs/006-activity-states/contracts/csv-format.md` (19 fields) and
    `states-csv-format.md`.
  - **Parser `Csv/`**: add `ActivityState`, `StatesCsvWriter`, `StatesCsvReader`, and
    `StatesCsvFileName`.
  - **`State/`**: add `statesCsvFilename`, and that `CsvFileNameRule` hides States CSVs.
  - **`Analysis/`**: add `CurrentStatesLoader` and the shared `AppCsvFile`.
  - **`Table/`**: correct the `TrackpointBands` line. RunCadence is now already steps per minute.
  - **Lessons from feature 006**:
    - Python's `sum()` is compensated, so generators use loops.
    - APFS is case-insensitive, so name rules must ignore case.
    - Force a write failure by making the target a directory.
    - A format change ripples into every test that builds rows.

  Do not edit `specs/*/checklists/`.
- [X] T054 Run the Compose gate (Principle VII):
  1. Check that `docker info` succeeds and port 8080 is free. Otherwise prefix the command with
     `TCXTOOL_HOST_PORT=8081`.
  2. Run `docker compose -f compose.yaml -f compose.test.yaml up --build --abort-on-container-exit --exit-code-from tests`.
  3. Confirm exit code 0 and the test count.
  4. Run `docker compose -f compose.yaml -f compose.test.yaml down`.

  **Done note**: exit code 0, with 557 tests passing in the container.
- [X] T055 Run the specs/006-activity-states/quickstart.md scenarios against a scratchpad folder,
  never `data/csv/` or `tcxtool-state.json`. Read only the cadence, time, distance, and state
  columns of real data.
  - Scenario 2 must show 541 StateSets, StateElapsedSeconds adding up to 19,881, and a conversion
    under 10 s.
  - Scenario 7 must print `unchanged`.
  - Delete the scratchpad copies.
  - Record the results in a done note. Leave the browser timings and visual checks of scenario 5
    to the maintainer.

  **Done note** (app on `127.0.0.1:5199`, with CSV and state paths in a scratchpad folder):
  - **Scenario 2**:
    - The conversion took 0.81 s and listed `19908 rows` and `541 StateSets`.
    - States: running 11206, walking 7982, stopped 720, and no `unknown`. Every line has 19 fields.
    - StateElapsedSeconds total 19881. DistanceMiles total 26.427414, which is the last DistanceMiles.
  - **Scenario 3**: the state file holds both names. The home page lists only the activity CSV. Selecting
    a copied 17-field CSV wrote no `statesCsvFilename`.
  - **Scenario 4**: `/table` shows the wrong-format notice with the link. `/states` says there is no
    States CSV, with the link.
  - **Scenario 5, server side**: `/states` was served in 0.02 s, with 541 body rows and 13 headings.
    The States link is active. 1,082 average cells have band classes. `/table` colors `186` as
    `cadence-band-4`.
  - **Scenario 6**: each of the four notices showed with a 200.
  - **Scenario 7**: `unchanged`. The scratchpad folder was deleted.
  - **Left to the maintainer**: the browser timing and the visual checks of scenario 5.
- [X] T056 Run the final gates 1–3 and `git status`. Confirm that no file under `data/` and no
  `tcxtool-state.json` was changed by this feature. Report, without fixing, that
  `data/csv/2026-11-01_24932322230.csv` is in the pre-006 format and needs one reconversion by the
  maintainer.

  **Done note**: gates 1–3 pass with 557 tests. `git status` shows no change under `data/` and no
  `tcxtool-state.json`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (T001)**: first.
- **US1 (T002–T017)**: after Setup. It changes the CSV format every later phase uses.
- **US2 (T018–T032)**: after US1. Its goldens and writer read 19-field rows.
- **US3 (T033–T052)**: after US2. It needs `StatesCsvRow`, `StatesCsvWriter.Header`,
  `StatesCsvFileName`, and `CurrentCsv.StatesFileName`.
- **Polish (T053–T056)**: after US3.

### Within Each Phase

- The generator task (T002, T018) comes first. The other test tasks follow, and the run that
  confirms failures (T010, T025, T043) comes before any implementation.
- **US1**: T011 → T012 → T013 → T014 (the reader uses the writer's `FieldCount`); T015 and T016
  after T012.
- **US2**: T026 and T027 → T028 → T029 → T030; T031 after T026.
- **US3**: T044 → T045 → T046 → T047 → T048 → T049; T050 and T051 any time after T043.

### Parallel Opportunities

- US1 tests T003–T009 each touch different files, so all can run in parallel once T002 has
  regenerated the expected files.
- US2 tests T019–T024 are independent, and so are T026 and T027.
- US3 tests T033–T042 are independent, apart from T034, which T036–T040 use.

## Parallel Example: User Story 2

```text
T019 StatesCsvFileNameTests     T020 StatesCsvWriterTests     T021 TcxCsvConverterTests
T022 ConversionResultViewTests  T023 CurrentCsvStoreTests     T024 Convert/HomeSelect endpoint tests
```

## Parallel Example: User Story 3

```text
T033 StatesCsvReaderTests  T035 CurrentStatesMessagesTests  T037 StatesTableTests
T041 StatesStylesTests     T042 MainLayoutTests
(after T034) T036 loader  T038 page  T039 endpoint  T040 performance
```

## Implementation Strategy

### MVP First (User Story 1 Only)

US1 alone gives correct cadence units and a state on every trackpoint, and keeps the Table page
right for both new and old files. It is releasable on its own.

### Incremental Delivery

1. US1: the 19-field CSV and Table page fixes. Gates pass at T017.
2. US2: the States CSV and the state file. Gates pass at T032.
3. US3: the States page. Gates pass at T052.
4. Polish: CLAUDE.md, the Compose gate, the quickstart, and the final check.

## Notes

- Mark each task `[X]` when done, and add a short *done note* when the task did something the text
  did not foresee.
- `specs/*/checklists/` are read-only during `/speckit-implement`.
- `.specify/feature.json` points at `specs/006-activity-states`.
