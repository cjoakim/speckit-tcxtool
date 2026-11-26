# Implementation Plan: Activity States

**Branch**: `006-activity-states` (feature id; work is currently on git branch `main`) | **Date**: 2026-11-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/006-activity-states/spec.md`

## Summary

The converter learns the runner's state and writes a second file for each activity.

- **Activity CSV (19 fields)**: RunCadence is doubled to steps per minute. Two fields follow it:
  - `State`: stopped, walking, running, or unknown
  - `StateSeconds`: the time since the previous StateSet ended
- **States CSV**: `<name>.states.csv` sits beside the activity CSV, with one 13-field row per
  StateSet. Its averages, distance, and end point are calculated from the activity CSV's own
  values.
- **State file**: `tcxtool-state.json` records the States CSV as `statesCsvFilename`, next to
  `csvFilename`. The home page never lists States CSVs.
- **States page**: a new `/states` page, after Table in the navbar, shows the States CSV with the
  Table page's band colors.
- **Table page**: it stops doubling cadence. Its wrong-format notice, which now covers every CSV
  from before this feature, says to convert again.

**Technical approach** (details in research.md):

- **Parser library** (R1–R4, R6–R8): the states are calculated once, in `Csv/`.
  - `ActivityCsvWriter.Rows` builds the 19-field rows, and `StatesCsvWriter.Rows` groups them into
    StateSets.
  - The converter writes both files with its existing temp-then-move procedure.
  - Arithmetic is fixed by the code: `TimeSpan` for StateSeconds, text copies for end values, and
    explicit left-to-right loops for means.
- **Web project** (R5, R9–R11):
  - `CurrentCsvStore` reads and writes the new property.
  - `CsvFileNameRule` hides `.states.csv` names, ignoring case.
  - A new `CurrentStatesLoader` feeds the static `/states` page. It shares the I/O classification
    of `CurrentActivityLoader` through a small internal helper.
- **Golden files** (R12): `generate_success_fixtures.py` regenerates the 7 activity goldens and
  writes 8 States goldens, including a new `run-states.tcx`, independently of the C# code.

## Technical Context

**Language/Version**: C# (ships with SDK 11.0.100), `net11.0`, pinned by `global.json`.

**Primary Dependencies**: the ASP.NET Core / Blazor shared framework and the vendored Bootstrap
5.3.0. No new runtime or test packages.

**Storage**: files only.
- Activity CSVs and States CSVs are in `TcxTool:CsvOutputDirectory`.
- `tcxtool-state.json` gains one optional property.
- The converter and the home page selection write. Pages only read.

**Testing**: xUnit 2.9.3, bUnit 2.15.2, FluentAssertions 7.2.2, `Microsoft.AspNetCore.Mvc.Testing`
11.0.0, and coverlet. All of these are unchanged.
- HTTP tests use `TcxToolWebFactory`.
- bUnit tests use `BunitTcxToolServices`, which gains `CurrentStatesLoader`.
- Expected files come from the Python generator (R12).
- NSubstitute is still not needed.

**Target Platform**: the macOS host (`dotnet run`), and the Linux container from `aspnet:11.0`,
running non-root on port 8080.

**Project Type**: Web application (Blazor Web App), with a parser class library and a test project.

**Performance Goals**:
- **SC-003**: each sample TCX converts to both files in under 10 s. The new work is one pass over
  the rows already built, so the existing `ConversionPerformanceTests` keeps guarding it.
- **SC-004**: the marathon's States page, 541 rows (about 2.7% of the Table page's 19,908), is fully
  shown in under 5 s. A test holds the server side to that time for 1,000 rows. The browser side is
  checked by hand (quickstart scenario 5).

**Constraints**:
- Static SSR only (Principle I, FR-019).
- Values as written, with no rounding (FR-020).
- The two files always agree (FR-010).
- Both files are written or neither is (R6).
- The state file never holds a mismatched pair (FR-014).
- Error text and logs never include file content.
- Tests never touch the real `tcxtool/` files (FR-026).
- Tests run offline.

**Scale/Scope**: one local user and one activity per page view, with up to about 20,000 trackpoints
and about 540 StateSets.
- **New parser types (7)**: `ActivityState`, `StatesCsvRow`, `StatesCsvWriter`,
  `StatesCsvFileName`, `StatesCsvReader`, `StatesCsvReadResult`, and the internal
  `PipeDelimitedLines`. Plus the `run-states.tcx` fixture.
- **Changed parser types (5)**: `ActivityCsvRow`, `ActivityCsvWriter`, `ActivityCsvReader`,
  `SavedCsv`, and `TcxCsvConverter`.
- **New web types (5 plus the page)**: `AppCsvFile`, `CurrentStatesStatus`, `CurrentStates`,
  `CurrentStatesLoader`, and `CurrentStatesMessages`. Plus `StatesPage` (`.razor` and `.razor.cs`)
  and `StatesTable`.
- **Changed web types**: `CurrentCsv`, `CurrentCsvStore`, `TcxToolState`, `CsvFileNameRule`,
  `CurrentActivityLoader`, and `TrackpointBands`.
- **Changed web files**: `TablePage.razor`, `ConversionResultView.razor`, `MainLayout.razor`,
  `Program.cs`, `_Imports.razor`, and `app.css`.

No item is marked NEEDS CLARIFICATION. The spec's three clarifications and research R1–R13 settle
every design question.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | How this plan complies | Status |
|-----------|------------------------|--------|
| I. .NET 11 Blazor Web App | `/states` is static SSR, with no `@rendermode`. A guard test checks for no `<!--Blazor:{` and is shown failing with a temporary break. The table uses Bootstrap classes. D3 is not needed | Pass |
| II. Style enforced by the toolchain | No project setting changes. Each new public type is in its own file. New parser APIs carry XML docs (the build enforces it). `async` methods and tests end in `Async` and take a token. Records are immutable. `StatesPage` has a code-behind, and the Razor files only bind | Pass |
| III. Parser library, UI on top | Every calculation and both readers are in the parser library. The Web project gets typed results (`StatesCsvReadResult`, `CurrentStates`) and only displays them. `ParserIndependenceTests` still applies | Pass |
| IV. Built-in XML parsing only | The TCX parsing path is unchanged. No new XML is read | Pass |
| V. Test-first | Each test task comes before its code. Guard tests (no writes, static SSR, no empty `class`, hidden States names) are shown failing with temporary breaks, reverted with an edit. Golden files come from the independent generator (R12) | Pass |
| VI. Docker multi-stage, non-root | Unchanged. States CSVs land in the bind-mounted `/data/csv` | Pass |
| VII. Compose is the test gate | The Compose gate runs at the end. The new fixtures sit under the read-only mounted `Fixtures/`. No test reads `.gitignore` or `specs/` | Pass |
| Technology Constraints / Logging | `ILogger<T>` warnings name the file and the exception type or error kind and line. No file content is logged | Pass |
| Governance: three projects | No new project. Tests are new folders and files in the one test project | Pass |

**Post-design re-check** (after research.md, data-model.md, and contracts/): still Pass on every
row.
- Sharing the file-open code through `AppCsvFile` (R9) and reusing `ActivityCsvError` (R8) remove
  code rather than add it.
- No complexity entry is needed.

## Project Structure

### Documentation (this feature)

```text
specs/006-activity-states/
├── spec.md
├── plan.md                    # this file
├── research.md                # R1–R13 and the facts verified on this machine
├── data-model.md              # types, fields, and rules
├── quickstart.md              # manual end-to-end checks
├── contracts/
│   ├── csv-format.md          # the 19-field activity CSV (replaces 002's field list)
│   ├── states-csv-format.md   # the 13-field States CSV
│   ├── state-file.md          # statesCsvFilename and the changed name rule
│   └── pages.md               # navbar, Convert result, Table page changes, States page
├── checklists/requirements.md
└── tasks.md                   # /speckit-tasks
```

### Source Code (`tcxtool/`)

```text
src/JoakimSoftware.TcxTool.Parser/
├── Csv/
│   ├── ActivityState.cs              # new
│   ├── ActivityCsvRow.cs             # changed: 19 fields
│   ├── ActivityCsvWriter.cs          # changed: Rows, Write(rows), 19-field header
│   ├── ActivityCsvReader.cs          # changed: 19 fields, uses PipeDelimitedLines
│   ├── PipeDelimitedLines.cs         # new, internal
│   ├── StatesCsvRow.cs               # new
│   ├── StatesCsvWriter.cs            # new
│   ├── StatesCsvFileName.cs          # new
│   ├── StatesCsvReader.cs            # new
│   └── StatesCsvReadResult.cs        # new
└── Conversion/
    ├── SavedCsv.cs                   # changed: StatesFileName, StateSetCount
    └── TcxCsvConverter.cs            # changed: writes both files

src/JoakimSoftware.TcxTool.Web/
├── State/
│   ├── TcxToolState.cs               # changed: statesCsvFilename
│   ├── CurrentCsv.cs                 # changed: StatesFileName
│   ├── CurrentCsvStore.cs            # changed: read and write the pair
│   └── CsvFileNameRule.cs            # changed: rejects .states.csv
├── Analysis/
│   ├── AppCsvFile.cs                 # new, internal (moved from CurrentActivityLoader)
│   ├── CurrentActivityLoader.cs      # changed: uses AppCsvFile
│   ├── CurrentStatesStatus.cs        # new
│   ├── CurrentStates.cs              # new
│   ├── CurrentStatesLoader.cs        # new
│   └── CurrentStatesMessages.cs      # new
├── Table/TrackpointBands.cs          # changed: no doubling
├── Components/
│   ├── Layout/MainLayout.razor       # changed: States link
│   ├── Conversion/ConversionResultView.razor   # changed: States CSV line
│   ├── Pages/TablePage.razor         # changed: wrong-format link
│   ├── Pages/StatesPage.razor        # new
│   ├── Pages/StatesPage.razor.cs     # new
│   ├── States/StatesTable.razor      # new
│   └── _Imports.razor                # changed: Components.States
├── Program.cs                        # changed: CurrentStatesLoader singleton
└── wwwroot/app.css                   # changed: .states-table-scroll rules

tests/JoakimSoftware.TcxTool.Tests/
├── Fixtures/
│   ├── generate_success_fixtures.py  # changed: 19 fields, States goldens, run-states
│   ├── run-states.tcx                # new (generated)
│   ├── README.md                     # changed
│   └── Expected/                     # 7 regenerated activity CSVs, plus 8 *.states.csv and run-states.csv
├── Parser/
│   ├── ActivityStateTests.cs         # new
│   ├── StatesCsvWriterTests.cs       # new
│   ├── StatesCsvFileNameTests.cs     # new
│   ├── StatesCsvReaderTests.cs       # new
│   └── (ActivityCsvWriterTests, ActivityCsvReaderTests, TcxCsvConverterTests: changed)
├── Support/ActivityCsvFixture.cs     # changed: 19 fields; StatesCsvFixture.cs new
└── Web/
    ├── Analysis/CurrentStatesLoaderTests.cs, CurrentStatesMessagesTests.cs   # new
    ├── States/StatesTableTests.cs    # new
    ├── StatesPageTests.cs, StatesPageEndpointTests.cs, StatesPagePerformanceTests.cs, StatesStylesTests.cs   # new
    └── (State/CurrentCsvStoreTests, Table/*, TablePage*, ConversionResultViewTests,
         ConvertEndpointTests, MainLayoutTests, Analysis/CurrentActivityLoaderTests: changed)
```

**Structure Decision**: the existing three-project layout, with no change in project count:

- the parser library holds the format, the calculation, and the readers
- the Web project holds state, loading, and pages
- the test project holds everything else

## Complexity Tracking

No constitution violations, so nothing to justify.
