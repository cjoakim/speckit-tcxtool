---

description: "Task list for feature 005-table-page"
---

# Tasks: Table Page for the Current CSV

**Input**: Design documents from `specs/005-table-page/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included. The constitution requires test-first (Principle V, NON-NEGOTIABLE).

- Every test task comes before the code it covers, and must be run and seen failing first.
- A test that fails to compile because its subject type or member does not exist yet counts as
  failing. Add the subject in the next task.
- If a test passes before its code is written, show it failing with a temporary break in that code.
  Revert the break in the same task, with an edit rather than by restoring a backup (`CLAUDE.md`,
  lessons from 001 and 002).

**Organization**:
- Setup adds one test helper.
- Foundational builds the CSV reader and the current-activity loader. Every story needs them.
- Then come:
  - US1: the page states and the header box
  - US2: the trackpoint table
  - US3: the band colors
- US3 builds on US2's table component, so the stories are done in order.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task belongs to (US1, US2, US3)
- All paths are relative to `tcxtool/`. Run every command from `tcxtool/`. The git root is the
  parent directory.

## Path Conventions

These come from plan.md → Project Structure:

- Parser: `src/JoakimSoftware.TcxTool.Parser/Csv/`
- Web: `src/JoakimSoftware.TcxTool.Web/{Analysis,Table,Components/Pages,Components/Table,wwwroot}/`
- Tests: `tests/JoakimSoftware.TcxTool.Tests/{Parser,Support,Web,Web/Analysis,Web/Table}/`

**Conventions for every task**:

- **Code rules**:
  - one public type per file, file-scoped namespaces
  - `_camelCase` only for instance fields; static fields PascalCase; local constants camelCase
  - `Async` methods take a `CancellationToken`, and every `async Task` test name ends in `Async`
    (IDE1006)
  - every public parser type and member has an XML doc comment (`GenerateDocumentationFile`)
  - no relaxed settings or suppressed rules
- **Namespace trap**: do not create a namespace or folder named `Activity` in the Web project. It
  would capture `Activity.Current` in `Components/Pages/Error.razor` (research.md R2 "Naming").
- **Test isolation (FR-022)**:
  - HTTP tests get their host only from `Support/TcxToolWebFactory`.
  - bUnit tests register services only through `Support/BunitTcxToolServices`.
  - Loader tests build `TcxToolOptions` whose paths are inside a `TempDirectory`.
  - No test reads `data/`, `specs/`, or the real `tcxtool-state.json`. `.dockerignore` keeps
    `data/` and `specs/` out of the Compose build context.
- **I/O failures in tests**: the Compose test container runs as root, so `chmod` can't cause a
  failure. Use the two techniques verified in research.md "Facts":
  - **Unreadable**: hold the CSV open with `new FileStream(path, FileMode.Open, FileAccess.Read,
    FileShare.None)` for the duration of the call.
  - **Deleted after the status check**: make the CSV a dangling symbolic link
    (`File.CreateSymbolicLink(csvPath, "<temp>/does-not-exist.csv")`).
- **CSV inputs**: tests use the golden CSVs in `Fixtures/Expected/` or `Support/ActivityCsvFixture`
  (T002). Every value is made up. Never copy rows from `data/csv/`, which holds real GPS positions.
- **Messages**: user-facing text comes only from `CurrentActivityMessages`. Tests compare against
  `CurrentActivityMessages.For(...)`, or against the literal strings in contracts/pages.md
  "Messages".
- **bUnit and async loads**: `TablePage` loads in `OnInitializedAsync` with async file I/O. Wait for
  the result with `WaitForElement` or `WaitForAssertion`, as `HomePageTests` does, before asserting.

---

## Phase 1: Setup

**Purpose**: a green baseline, and one helper that writes contract CSVs and state files for tests.

- [X] T001 Run quality gates 1–3: `dotnet build JoakimSoftware.TcxTool.slnx`,
  `dotnet format JoakimSoftware.TcxTool.slnx --verify-no-changes`, and
  `dotnet test JoakimSoftware.TcxTool.slnx`. All must pass before any change, so every later
  failure is one this feature caused.
- [X] T002 Create `tests/JoakimSoftware.TcxTool.Tests/Support/ActivityCsvFixture.cs`, a `public static`
  test helper with XML doc comments. Its members:
  - `Line(...)` builds one 17-field data line in contract order, joined with `|`. It has named
    optional parameters for every field, each defaulting to a made-up value: ActivityId
    `fixture-run`, Date `2026-11-01`, DeviceName `Forerunner 999`, DeviceId `77`, Sport `Running`,
    TrackpointNum `1`, Time `2026-11-01T06:00:00.000Z`, ElapsedSeconds `0`, ElapsedTime `00:00:00`,
    Latitude `1.5`, Longitude `2.5`, HeartRateBpm `120`, RunCadence `80`, DistanceMeters `0.0`,
    DistanceMiles `0`, AltitudeMeters `10.0`, AltitudeFeet `32.808398950131235`.
  - `Write(string path, IEnumerable<string> lines)` writes `ActivityCsvWriter.Header`, then each
    line, each followed by `\n`. The file is UTF-8 with no BOM, and the parent directory is created.
  - `WriteRaw(string path, string content)` writes the content unchanged, for malformed inputs.
  - `Synthetic(int count)` returns `count` lines whose TrackpointNum runs from 1 to `count`, with
    made-up values that vary from row to row.
  - `WriteState(string statePath, string csvFileName)` writes
    `{\n  "csvFilename": "<name>"\n}\n`, the exact format in
    `specs/004-about-and-csv-state/contracts/state-file.md`, and creates the parent directory.

  This is support code, so no test covers it. It compiles, and the suite stays green.

**Checkpoint**: the suite is green, and tests can write CSVs and state files into temp directories.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: the CSV reader in the parser library, and the loader that every Table page state
comes from (research.md R1–R3, R11).

**⚠️ CRITICAL**: no user story can begin until this phase is complete.

### Tests first

- [X] T003 [P] Create `tests/JoakimSoftware.TcxTool.Tests/Parser/ActivityCsvReaderTests.cs`,
  covering every row of contracts/activity-csv-reader.md:
  - **Round trip** (`[Theory]`): for each golden file in contracts/activity-csv-reader.md "Round
    trip", read it through `RepositoryRoot.Combine("tests/JoakimSoftware.TcxTool.Tests/Fixtures/Expected/<file>")`
    with a `StreamReader`. Assert `IsSuccess`, the row count in the table, and that each row's 17
    properties joined with `|` equal the file's line.
  - **Field mapping**: one line from `ActivityCsvFixture.Line(...)` with a distinct value in every
    field reads back with each value in its own property of data-model.md `ActivityCsvRow`. An empty
    field reads as `""`, not null.
  - **Header-only input** → success with 0 rows.
  - **`\r\n` line endings** → the same rows as `\n`.
  - **`\r` line endings** (a lone CR, per contracts/activity-csv-reader.md "Accepted input") → the
    same rows as `\n`.
  - **A BOM**: a UTF-8 BOM before the header, read through `new StreamReader(stream, detectEncodingFromByteOrderMarks: true)`,
    gives a success.
  - **`HeaderMismatch` at line 1** (data-model.md `ActivityCsvError.LineNumber`):
    - empty input
    - `hello`
    - the header in lowercase
    - the header missing its last field
    - the header with an extra `|X`
    - the header plus a trailing space
  - **`FieldCount`**: the header plus 2 valid lines, then a line with 16 fields, gives a failure with
    `LineNumber` 4 and `Rows` empty (data-model.md `ActivityCsvReadResult.Rows`). Lines with 18 fields
    and a blank middle line fail the same way, each with its own line number.
  - **Arguments and cancellation**: a null input throws `ArgumentNullException`. An already-cancelled
    token throws `OperationCanceledException`.

  Confirm it fails to compile.
- [X] T004 [P] Create `tests/JoakimSoftware.TcxTool.Tests/Web/Analysis/CurrentActivityMessagesTests.cs`:
  - a `[Theory]` that `CurrentActivityMessages.For` returns each exact string in contracts/pages.md
    "Messages", with `""` for `Loaded`
  - `For((CurrentActivityStatus)99)` throws `ArgumentOutOfRangeException`

  Confirm it fails to compile.
- [X] T005 Create `tests/JoakimSoftware.TcxTool.Tests/Web/Analysis/CurrentActivityLoaderTests.cs`.
  - **Setup**: each test builds a `CurrentCsvStore` and a `CurrentActivityLoader` from:
    - `TcxToolOptions` whose CSV directory and state path are inside a `TempDirectory`
    - a `HostingEnvironment` whose content root is that directory
    - `ListLogger<CurrentActivityLoader>` for the loader
    - `NullLogger<CurrentCsvStore>.Instance` for the store, whose own logging 004 already tests

    CSVs and the state file are written with `ActivityCsvFixture`.
  - **Statuses**: one test per row of data-model.md `CurrentActivityStatus`:
    - no state file → `NoCurrentCsv`, `FileName` null, no log entry
    - the state names a CSV that does not exist → `Missing(name)`
    - the CSV is a dangling symbolic link → `Missing(name)`. This is the race branch of research.md R2.
    - the CSV is held with `FileShare.None` → `Unreadable(name)`. One Warning contains the name and
      `IOException`, and does not contain the exception message.
    - the first line is `hello` → `WrongFormat(name)`. One Warning contains the name,
      `HeaderMismatch`, and `1`.
    - the header, 2 valid lines, then a 16-field line → `WrongFormat`, with `Rows` empty. The
      Warning contains `FieldCount` and `4`.
    - header only → `NoTrackpoints`. `Summary` equals `ActivitySummary.From(name, null)`: every
      activity value is `""`, and `FileName` is the name.
    - 3 lines → `Loaded`, with 3 `Rows` in file order. `Summary` takes its values from the first
      line (data-model.md `ActivitySummary.ActivityId`, `ActivitySummary.Date`,
      `ActivitySummary.DeviceName`, `ActivitySummary.DeviceId`, `ActivitySummary.Sport`).
  - **Invariants**, for every status above (data-model.md `CurrentActivity.FileName`,
    `CurrentActivity.Summary`, and `CurrentActivity.Rows`):
    - `FileName` is null exactly for `NoCurrentCsv`
    - `Summary` is set only for `NoTrackpoints` and `Loaded`
    - `Rows` is non-empty only for `Loaded`
  - **Logs never carry content** (research.md R11):
    - In the `FieldCount` and `Unreadable` cases, write the CSV lines with
      `ActivityCsvFixture.Line(latitude: "12.3456789", longitude: "-65.4321987")`. These are
      marker values that can't appear in a file name, an error kind, or a line number.
    - Assert that no log entry contains `12.3456789` or `-65.4321987`.
    - The file name, the error kind, and the line number are expected in the entry. Don't assert
      against other field values, such as the Date, which also appears in the file name.
    - The `NoCurrentCsv`, `Missing`, `NoTrackpoints`, and `Loaded` cases log nothing.
  - **No writes (FR-007)**: after a `Loaded` load, the CSV and the state file have the same bytes
    and last-write times as before, and both directories list the same files.

  Confirm it fails to compile.

### Implementation

- [X] T006 [P] Create the parser value types, one per file, in `src/JoakimSoftware.TcxTool.Parser/Csv/`,
  each with XML doc comments:
  - `ActivityCsvRow.cs`: a sealed record with the 17 non-null `string` properties of data-model.md
    `ActivityCsvRow`, in contract order
  - `ActivityCsvErrorKind.cs`: data-model.md `ActivityCsvErrorKind` (`HeaderMismatch`, `FieldCount`)
  - `ActivityCsvError.cs`: data-model.md `ActivityCsvError.Kind` and `ActivityCsvError.LineNumber`
  - `ActivityCsvReadResult.cs`: data-model.md `ActivityCsvReadResult.Rows`,
    `ActivityCsvReadResult.Error`, and `ActivityCsvReadResult.IsSuccess`, created only through
    `Success(rows)` and `Failure(error)`. `Failure` has empty `Rows`.
- [X] T007 Create `src/JoakimSoftware.TcxTool.Parser/Csv/ActivityCsvReader.cs`, a static class with
  `FieldCount` and `ReadAsync`, per data-model.md `ActivityCsvReader` and
  contracts/activity-csv-reader.md. The rules are research.md R3:
  - read with `ReadLineAsync(cancellationToken)`
  - compare line 1 ordinally with `ActivityCsvWriter.Header`
  - split each later line with `Split('|')` and require exactly `FieldCount` fields
  - stop at the first bad line

  It uses only the BCL. T003 passes, and `Architecture/ParserIndependenceTests` stays green.
- [X] T008 [P] Create the Analysis value types, one per file, in `src/JoakimSoftware.TcxTool.Web/Analysis/`
  (namespace `JoakimSoftware.TcxTool.Web.Analysis`), each with XML doc comments:
  - `CurrentActivityStatus.cs`: data-model.md `CurrentActivityStatus`, all six values
  - `ActivitySummary.cs`: data-model.md `ActivitySummary.FileName`, `ActivitySummary.ActivityId`,
    `ActivitySummary.Date`, `ActivitySummary.DeviceName`, `ActivitySummary.DeviceId`, and
    `ActivitySummary.Sport`, with `From(string fileName, ActivityCsvRow? firstRow)`
  - `CurrentActivity.cs`: data-model.md `CurrentActivity.Status`, `CurrentActivity.FileName`,
    `CurrentActivity.Summary`, and `CurrentActivity.Rows`, with a private constructor. It has:
    - a static property `NoCurrentCsv { get; }`, following `CurrentCsv.None`
    - the static factory methods `Missing(name)`, `Unreadable(name)`, `WrongFormat(name)`, and
      `Read(name, rows)`. `Read` gives `NoTrackpoints` for 0 rows and `Loaded` otherwise.
  - `CurrentActivityMessages.cs`: data-model.md `CurrentActivityMessages`, with the strings in
    contracts/pages.md "Messages"

  T004 passes.
- [X] T009 Create `src/JoakimSoftware.TcxTool.Web/Analysis/CurrentActivityLoader.cs`, a `sealed partial`
  class per data-model.md `CurrentActivityLoader`.
  - `LoadAsync` follows research.md R2:
    1. Call `CurrentCsvStore.ReadAsync`.
    2. Open the file with `FileMode.Open`, `FileAccess.Read`,
       `FileShare.ReadWrite | FileShare.Delete`, `bufferSize: 4096`, and `useAsync: true`.
    3. Wrap it in a UTF-8 `StreamReader` with `detectEncodingFromByteOrderMarks: true`.
    4. Call `ActivityCsvReader.ReadAsync`.
  - Exceptions: one `try` holds both the open and the `ActivityCsvReader.ReadAsync` call, because
    I/O exceptions from the reader reach the caller (data-model.md `ActivityCsvReader`).
    - catch `FileNotFoundException` and `DirectoryNotFoundException`, and return `Missing`
    - then catch any other `IOException` and `UnauthorizedAccessException`, and return `Unreadable`
    - catch nothing else
  - Logging: two `[LoggerMessage]` Warning methods, as research.md R11 describes.
  - Register it in `src/JoakimSoftware.TcxTool.Web/Program.cs` with
    `builder.Services.AddSingleton<CurrentActivityLoader>();`, after `CurrentCsvStore`.

  T005 passes. T005's no-writes assertion has never failed yet, so show it working with a temporary
  break:
  1. In `LoadAsync`, after the read, append `"\n"` to the CSV with `File.AppendAllTextAsync`.
  2. Confirm the no-writes test **fails**.
  3. Remove the line with an edit, and confirm T005 passes again.
- [X] T010 Add `services.AddSingleton<CurrentActivityLoader>();` to
  `tests/JoakimSoftware.TcxTool.Tests/Support/BunitTcxToolServices.cs`, after `CurrentCsvStore`, and
  mention it in the `Add` doc comment. Every existing bUnit test still passes.
- [X] T011 Run quality gates 1–3 (as in T001). All must pass.

**Checkpoint**: the reader and the loader exist and are tested. No page uses them yet.

---

## Phase 3: User Story 1 - See which activity the Table page is showing (Priority: P1) 🎯 MVP

**Goal**: `/table` shows the header box for the current CSV, and a clear notice in every state
without data.

**Independent Test**: select a CSV on `/`, then open `/table`. The Activity card shows the file name
and the first row's ActivityId, Date, DeviceName, DeviceId, and Sport. With no state file, the page
shows the "No CSV is selected yet." notice and its two links.

### Tests for User Story 1 ⚠️

- [X] T012 [P] [US1] Create `tests/JoakimSoftware.TcxTool.Tests/Web/Table/ActivitySummaryCardTests.cs`
  (bUnit, contracts/pages.md "Header box"):
  - the structure is `div.card.mb-3 > div.card-body#activity-summary`, with an
    `h2.card-title.h5` reading `Activity`
  - `dl.row.mb-0` holds exactly 6 `dt.col-sm-3` + `dd.col-sm-9` pairs. The `dt` texts are, in order:
    `CSV file`, `ActivityId`, `Date`, `DeviceName`, `DeviceId`, `Sport`. Each `dd` holds the matching
    data-model.md `ActivitySummary` value.
  - an `ActivitySummary` with empty `DeviceName` and `DeviceId`: their `dd`s contain
    `span.text-muted` with `—` (U+2014), and all 6 `dt`s are still present (FR-010)

  Confirm it fails to compile.
- [X] T013 [US1] Move the Table page out of the placeholder tests, then test its states.
  1. In `tests/JoakimSoftware.TcxTool.Tests/Web/PlaceholderPageTests.cs`, remove the `TablePage`
     row from `Pages`, and change the class doc comment to name only Histograms and Timeline. The
     class still passes.
  2. Create `tests/JoakimSoftware.TcxTool.Tests/Web/TablePageTests.cs` (bUnit, `BunitContext`,
     services through `BunitTcxToolServices`; contracts/pages.md "Page" and "Content by status").
     - **Carried over from the placeholder test** (these two pass from the start, because the
       placeholder page already has them):
       - the route is exactly `/table`
       - in every state, there is exactly one `h1`, it is `h1.h3.mb-3`, and it reads `Table`
     - **`NoCurrentCsv`**:
       - `div.alert.alert-info#table-status[role=status]` holds `CurrentActivityMessages.For(NoCurrentCsv)`
       - it has an `a[href=""]` reading `Select a CSV file` and an `a[href=convert]` reading
         `convert a TCX file`
       - there is no `#activity-summary` and no `table`
     - **`Missing`** (the state names an absent file):
       - `div.alert.alert-warning#table-status[role=alert]` holds the message, the name in a
         `strong`, and an `a[href=""]` reading `Select another CSV file`
       - there is no `#activity-summary` and no `table`
     - **`Unreadable`** (`FileShare.None`) and **`WrongFormat`** (first line `hello`): each shows a
       `role=alert` warning with its message and the name in a `strong`, and no `#activity-summary`
       and no `table`
     - **`NoTrackpoints`**: `#activity-summary` comes first, then
       `div.alert.alert-info#table-status[role=status]` with the message. There is no `table`.
     - **`Loaded`**: `#activity-summary` shows the file name and the first row's values. There is no
       `#table-status`.

  Confirm the status tests fail.
  - *Done note*: the heading theory failed first too, not only the status tests: it waits for
    `#table-status` or `#activity-summary` before asserting, and the placeholder page renders neither.
    Only the route test passed from the start. A `CompareDocumentPosition` order check was replaced by
    comparing markup offsets, because AngleSharp reported the card's position the other way round.
- [X] T014 [P] [US1] Create `tests/JoakimSoftware.TcxTool.Tests/Web/TablePageEndpointTests.cs`
  (HTTP, through `TcxToolWebFactory`; contracts/pages.md "HTTP"). Name every test `…Async`.
  - **No state file**: `GET /table` returns `200`. The body contains `<title>Table</title>`,
    `>Table</h1>`, and the `NoCurrentCsv` message.
  - **Loaded**: write a 3-line CSV to `factory.CsvDirectory` and the state file to
    `factory.StateFilePath` with `ActivityCsvFixture`. `GET /table` returns `200`, and the body
    contains `id="activity-summary"` and the file name.
  - **Files unchanged (FR-007)**: in the Loaded setup, record both files' bytes and last-write
    times, and both directory listings. `GET /table` twice. Afterwards, all four are unchanged.
  - **Static SSR (FR-020)**: in the Loaded setup, the body of `GET /table` does not contain
    `<!--Blazor:{`, the marker Blazor writes for an interactive component.
    - Assert on that exact string, not on `<!--Blazor`.
    - A static page still carries `<!--Blazor-Server-Component-State:` and
      `<!--Blazor-Web-Initializers:` comments. That was checked against the running app's current
      `/table`.

  Confirm the first two **fail**. The last two pass from the start: they are guards, and T016 shows
  each failing.

### Implementation for User Story 1

- [X] T015 [P] [US1] Create `src/JoakimSoftware.TcxTool.Web/Components/Table/ActivitySummaryCard.razor`,
  per contracts/pages.md "Header box", with `[Parameter, EditorRequired] public ActivitySummary Summary { get; set; } = default!;`.
  Render an empty value as `<span class="text-muted">—</span>`. Add these usings to
  `src/JoakimSoftware.TcxTool.Web/Components/_Imports.razor`:
  - `@using JoakimSoftware.TcxTool.Parser.Csv`
  - `@using JoakimSoftware.TcxTool.Web.Analysis`
  - `@using JoakimSoftware.TcxTool.Web.Table`
  - `@using JoakimSoftware.TcxTool.Web.Components.Table`

  T012 passes.
  - *Done note*: `@using JoakimSoftware.TcxTool.Web.Table` was **not** added here. That namespace
    has no type until T031, so the using would not compile. T032 adds it.
- [X] T016 [US1] Replace the content of `src/JoakimSoftware.TcxTool.Web/Components/Pages/TablePage.razor`,
  and create `src/JoakimSoftware.TcxTool.Web/Components/Pages/TablePage.razor.cs`.
  - The code-behind is a `public partial class TablePage`, following `Home.razor.cs`:
    - `[Inject] CurrentActivityLoader`
    - `[CascadingParameter] public HttpContext? HttpContext`
    - a `RequestAborted` property
    - `OnInitializedAsync`, which sets a private `CurrentActivity? Current`
  - The markup keeps `@page "/table"`, `<PageTitle>Table</PageTitle>`, and the `h1`. It then renders
    each status per contracts/pages.md "Content by status".
  - For `Loaded`, render only `ActivitySummaryCard` for now. US2 adds the table.
  - No `@rendermode` (FR-020).

  T013 and T014 pass, and `PlaceholderPageEndpointTests` stays green. Then show each of T014's two
  guards failing, one at a time:
  1. **Files unchanged**: temporarily append `"\n"` to the CSV in `CurrentActivityLoader.LoadAsync`.
     Confirm the guard **fails**, then revert with an edit.
  2. **Static SSR**: temporarily add `@rendermode InteractiveServer` to `TablePage.razor`. Confirm
     the guard **fails**, then revert with an edit.
  3. Confirm T013 and T014 pass again.
- [X] T017 [US1] Run quality gates 1–3. All must pass.

**Checkpoint**: US1 is complete. The Table page names the current activity, and it handles every
state without data. This is the MVP.

---

## Phase 4: User Story 2 - Read every trackpoint in a table (Priority: P2)

**Goal**: below the card, every row of the CSV appears in a ten-column table, in file order, with
values exactly as written, inside a scroll box whose heading row stays visible.

**Independent Test**: open `/table` for a small known CSV. The table has one body row per data row,
in order, with the ten headings. Each cell shows the CSV text for its column.

### Tests for User Story 2 ⚠️

- [X] T018 [P] [US2] Create `tests/JoakimSoftware.TcxTool.Tests/Web/Table/TrackpointTableTests.cs`
  (bUnit, contracts/pages.md "Table"). Build the rows with `ActivityCsvReader` from
  `ActivityCsvFixture` lines, or construct `ActivityCsvRow`s directly.
  - **Container**: the outer element is `div.table-responsive.trackpoint-table-scroll`.
  - **Table classes**: the table is `table.table.table-sm.table-bordered.small.mb-0#trackpoints`,
    and its class list has neither `table-striped` nor `table-hover`.
  - **Headings**: `thead.table-light` has one `tr` with exactly 10 `th[scope=col]`, whose texts are
    the 10 names, in contract order.
  - **Rows**: 3 rows give 3 `tbody tr` in input order, each with 10 `td`.
  - **Mapping**: give one row a distinct value in every one of its 17 fields. Each `td`'s text
    content equals the field for its column exactly, including long decimals such as
    `0.0009444842003490327`. The DistanceMeters, AltitudeMeters, and five activity values appear in
    no cell.
  - **Empty values**: a row with empty Time, Latitude, Longitude, DistanceMiles, and AltitudeFeet
    gives those `td`s an empty text content, and the row is still rendered.
  - **No extra attributes**: the 8 unbanded columns' `td`s have no attributes at all. That means no
    `class`, no `style`, and no scoped-CSS `b-…` attribute (research.md R5).

  Confirm it fails to compile.
- [X] T019 [US2] Extend `tests/JoakimSoftware.TcxTool.Tests/Web/TablePageTests.cs`. In the `Loaded`
  state, `div.trackpoint-table-scroll` comes after `#activity-summary`, and `table#trackpoints` has
  one `tbody tr` per CSV line. Confirm it **fails**.
- [X] T020 [P] [US2] Extend `tests/JoakimSoftware.TcxTool.Tests/Web/TablePageEndpointTests.cs`. In the
  Loaded setup, the body contains `id="trackpoints"`, the 10 `<th scope="col">` headings in order,
  and 3 body rows. Confirm it **fails**.
- [X] T021 [P] [US2] Create `tests/JoakimSoftware.TcxTool.Tests/Web/TablePagePerformanceTests.cs`
  (HTTP, through `TcxToolWebFactory`; SC-002 server side, research.md R4).
  - Write `ActivityCsvFixture.Synthetic(20_000)` and the state file.
  - **Warm up first, untimed**: create the client and send one `GET /table`, reading its whole body.
    The first request through the factory starts the host and JIT-compiles the first render, which
    SC-002 doesn't measure. The probe's 0.2 s was measured with the server already running
    (research.md "Facts").
  - Then time a second `GET /table` with a `Stopwatch`, from sending the request to reading the whole
    body. All the assertions below apply to this second response.
  - Assert `200`, that the body contains `id="trackpoints"`, exactly 20,001 `<tr` (1 heading row and
    20,000 body rows), and the TrackpointNum `20000`, and that the elapsed time is under 5 seconds.
  - Name it `TwentyThousandRowsRenderInUnderFiveSecondsAsync`.

  Confirm it **fails**.
- [X] T022 [P] [US2] Create `tests/JoakimSoftware.TcxTool.Tests/Web/TableStylesTests.cs`. It reads
  `RepositoryRoot.Combine("src/JoakimSoftware.TcxTool.Web/wwwroot/app.css")` and parses the rule
  blocks with a small regex.
  - `.trackpoint-table-scroll` sets `max-height: 75vh` and `overflow-y: auto`.
  - `.trackpoint-table-scroll thead th` sets `position: sticky`, `top: 0`, and `z-index: 1`
    (contracts/pages.md "Styles", FR-019).

  Confirm it **fails**.

### Implementation for User Story 2

- [X] T023 [US2] Create `src/JoakimSoftware.TcxTool.Web/Components/Table/TrackpointTable.razor`, per
  contracts/pages.md "Table".
  - It takes `[Parameter, EditorRequired] public IReadOnlyList<ActivityCsvRow> Rows { get; set; } = default!;`.
  - One `@foreach` writes each `<tr>` and its 10 `<td>`s inline, with no child component per row
    (research.md R4).
  - The cells map data-model.md `ActivityCsvRow` properties to the column order in data-model.md.
  - Write each cell as `<td>@row.Field</td>`, with no whitespace inside the `td`.
  - No `TrackpointTable.razor.css` file.
  - The HeartRateBpm and RunCadence cells get no class yet. US3 adds it.

  T018 passes.
  - *Done note*: T018 references `TrackpointTable`, so the test project could not compile until this
    component existed. T019–T022 were therefore seen failing right after T023 and before T024 and
    T025: 5 failures, each for its own reason. `TableStylesTests` strips `/* */` comments before
    parsing, because a comment above a rule otherwise becomes part of its selector.
- [X] T024 [US2] In `src/JoakimSoftware.TcxTool.Web/Components/Pages/TablePage.razor`, render
  `<TrackpointTable Rows="Current.Rows" />` after `ActivitySummaryCard` in the `Loaded` state.
  T019, T020, and T021 pass.
- [X] T025 [P] [US2] Add the two scroll-box rules from contracts/pages.md "Styles" to
  `src/JoakimSoftware.TcxTool.Web/wwwroot/app.css`, under a comment that names feature 005 and
  research.md R7. T022 passes.
- [X] T026 [US2] Run quality gates 1–3. All must pass.

**Checkpoint**: US2 is complete. Every trackpoint is readable, and the heading row stays in view.

---

## Phase 5: User Story 3 - See heart-rate and cadence zones by color (Priority: P3)

**Goal**: HeartRateBpm and RunCadence cells get the band colors of FR-014 and FR-015, with readable
text. Every other cell stays uncolored.

**Independent Test**: open `/table` for a CSV whose rows hit every band edge. Each HeartRateBpm and
RunCadence cell carries the right band class. The classes paint the contract colors, and empty or
non-numeric values have no class.

### Tests for User Story 3 ⚠️

- [X] T027 [P] [US3] Create `tests/JoakimSoftware.TcxTool.Tests/Web/Table/TrackpointBandsTests.cs`
  (data-model.md `TrackpointBands`; research.md R6):
  - **`HeartRateClass` theory**:

    | Value | Class |
    |-------|-------|
    | `-5`, `0`, `89`, `89.999` | `hr-band-1` |
    | `90`, `119`, `119.5` | `hr-band-2` |
    | `120`, `139` | `hr-band-3` |
    | `140`, `159` | `hr-band-4` |
    | `160`, `179`, `179.5` | `hr-band-5` |
    | `180`, `250` | `hr-band-6` |

  - **`RunCadenceClass` theory** (CSV values, doubled before comparison; spec Story 3 scenarios 2
    and 3):

    | Value | Class |
    |-------|-------|
    | `0`, `9.5`, `9.99` | `cadence-band-1` |
    | `10`, `55`, `74.5` | `cadence-band-2` |
    | `75`, `88`, `89.5` | `cadence-band-3` |
    | `90`, `129` | `cadence-band-4` |

  - **No band** (null), for both methods: `""`, `abc`, `NaN`, `Infinity`, `-Infinity`, `1,5`.

  Confirm it fails to compile.
- [X] T028 [US3] Extend `tests/JoakimSoftware.TcxTool.Tests/Web/Table/TrackpointTableTests.cs`:
  - Rows with HeartRateBpm `89`, `90`, `140`, `180` and RunCadence `9`, `10`, `75`, `90`: the
    HeartRateBpm `td` (column 5) and the RunCadence `td` (column 6) have exactly the class that
    `TrackpointBands` returns for their value.
  - A row with empty HeartRateBpm and `abc` RunCadence: both `td`s have no `class` attribute
    (FR-016).
  - The 8 other columns still have no attributes.

  Confirm it **fails**.
- [X] T029 [P] [US3] Extend `tests/JoakimSoftware.TcxTool.Tests/Web/TableStylesTests.cs`
  (contracts/pages.md "Styles"; FR-017):
  - Each of the ten band classes has a rule that sets exactly `--bs-table-bg` and
    `--bs-table-color`, with the contract values.
  - Compute the WCAG 2.x contrast ratio of each pair: relative luminance from sRGB, then
    `(L1 + 0.05) / (L2 + 0.05)`. Assert it is at least 4.5.
  - The expected values are written in the test, because `specs/` is not in the Compose build
    context.

  Confirm it **fails**.
- [X] T030 [P] [US3] Extend `tests/JoakimSoftware.TcxTool.Tests/Web/TablePageEndpointTests.cs`. A
  Loaded CSV with one line of HeartRateBpm `185` and RunCadence `88`: the body contains
  `<td class="hr-band-6">185</td>` and `<td class="cadence-band-3">88</td>`. Confirm it **fails**.

### Implementation for User Story 3

- [X] T031 [P] [US3] Create `src/JoakimSoftware.TcxTool.Web/Table/TrackpointBands.cs` (namespace
  `JoakimSoftware.TcxTool.Web.Table`), a `public static` class with XML doc comments. It has
  `HeartRateClass` and `RunCadenceClass`, per data-model.md `TrackpointBands` and research.md R6:
  - parse with `double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)`
  - reject values that are not finite
  - double the cadence before comparing
  - write the thresholds once, as PascalCase constants
  - return the class names as constants

  T027 passes.
- [X] T032 [US3] In `src/JoakimSoftware.TcxTool.Web/Components/Table/TrackpointTable.razor`, set
  `class="@TrackpointBands.HeartRateClass(row.HeartRateBpm)"` on the HeartRateBpm `td`, and
  `class="@TrackpointBands.RunCadenceClass(row.RunCadence)"` on the RunCadence `td`. Razor leaves
  out an attribute whose value is null. T028 and T030 pass, and T018 still passes.
  - *Done note*: this task also added `@using JoakimSoftware.TcxTool.Web.Table` to `_Imports.razor`,
    deferred from T015. T028's "no `class` attribute" test passed from the start, because no cell had a
    class yet. It was shown failing with a temporary `?? ""` on the HeartRateBpm class, which renders
    `class=""`, and that was reverted with an edit.
- [X] T033 [P] [US3] Add the ten band classes from contracts/pages.md "Styles" to
  `src/JoakimSoftware.TcxTool.Web/wwwroot/app.css`, under a comment that names FR-014, FR-015, and
  research.md R5. T029 passes.
- [X] T034 [US3] Run quality gates 1–3. All must pass.

**Checkpoint**: all three stories are complete. The Table page shows the activity, every
trackpoint, and the zones by color.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: project guidance, the Compose gate, and the manual checks the tests can't do.

- [X] T035 [P] Update `CLAUDE.md`:
  - **Feature history**: add a row, `specs/005-table-page` | Complete | `/table` page, with the
    current CSV's header box, a 10-column trackpoint table, and HR and cadence band colors.
  - **Parser library**: under `Csv/`, add `ActivityCsvReader` (typed rows or a typed error, never
    throws on bad content).
  - **Web project**: describe the new namespaces.
    - `Analysis/`: `CurrentActivityLoader` is how analysis pages get the current CSV's rows. They
      never open the file.
    - `Table/`: `TrackpointBands`.
  - **"Lessons from feature 005"**:
    - Don't name a Web namespace `Activity`, or it captures `Activity.Current` in `Error.razor`.
    - Force I/O cases with `FileShare.None` (unreadable) or a dangling symlink (deleted after the
      check).
    - Large tables use global CSS, not scoped CSS. Scoped CSS adds an attribute to every element.
    - Band colors set `--bs-table-bg` and `--bs-table-color`, with no striping or hover.
- [X] T036 Run the Compose gate (quality gate 4).
  1. Confirm `docker info` succeeds.
  2. Run `docker compose -f compose.yaml -f compose.test.yaml up --build --abort-on-container-exit --exit-code-from tests`,
     and confirm `echo $?` prints `0`.
  3. Run `docker compose -f compose.yaml -f compose.test.yaml down`.

  If port 8080 is busy, prefix the command with `TCXTOOL_HOST_PORT=8081`.
- [ ] T037 Run specs/005-table-page/quickstart.md scenarios 2–5 and 7 by hand against a real CSV in
  `data/csv/`.
  - Record the browser timings from scenario 4 (direct load and navbar click) as a done note on
    this task.
  - If the navbar route breaks SC-002, stop and report it. Do not add `data-enhance-nav="false"`
    without the maintainer's decision, because it changes a contract from feature 003 (research.md
    R4).
  - Afterwards, delete any CSVs added in scenario 5, and confirm scenario 7 prints `unchanged`.
  - *Partly done (2026-11-25), left open for the browser step.* Claude had no browser, so it ran
    scenarios 2–5 and 7 over HTTP. The app ran in Release, with `TcxTool__*` overrides pointing at a
    scratchpad copy of the real marathon CSV, so the real `data/csv/` and `tcxtool-state.json` were
    never touched.
    - Scenario 2: the card matches the file name and fields 1–5 of the first row.
    - Scenario 3:
      - The 10 headings are in order, and there are 19,908 body rows (equal to the file), ending at
        TrackpointNum 19908.
      - The band counts match an independent `awk` histogram: HR 37 / 2,433 / 8,283 / 8,096 /
        1,057 / 2, and cadence 720 / 7,982 / 9,717 / 1,489.
      - There are no scoped `b-` attributes and no `<!--Blazor:{` marker.
    - Scenario 4, server side: 0.10–0.15 s, 5.1 MB.
    - Scenario 5: no state, missing file, header only, and wrong format each return 200 with the
      contract notice and no error page. The one warning logged is `Current CSV hello.csv is not in
      the expected format: HeaderMismatch at line 1`.
    - Scenario 7: `unchanged`.
    - **Still to do, by the maintainer**: the scenario 4 browser timings (a direct load, and a navbar
      click through enhanced navigation), the visual checks in scenario 3 steps 2, 6, and 7, and the
      SC-005 glance check.
- [X] T038 Run quality gates 1–3 a final time. Run `git status`, and confirm that no file under
  `data/` and no `tcxtool-state.json` is staged or tracked.
  - *Done note*: gates 1–3 pass (402 tests, 0 warnings), and so does the Compose gate (T036: exit 0, 402
    tests in the container). `tcxtool-state.json` is not tracked, and this feature changed nothing
    under `data/`. But `data/csv/2026-11-01_24932322230.csv` and both `data/tcx/` files **are**
    tracked: the maintainer added them in commit `2bd6c7d`, and the `data/csv/` rule in `.gitignore`
    is commented out. That contradicts `CLAUDE.md` ("`data/csv/` … is git-ignored"). It is left for
    the maintainer to decide.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: depends on Setup (T002, for the CSV helper). It blocks every story.
- **US1 (Phase 3)**: depends on Foundational.
- **US2 (Phase 4)**: depends on US1. It extends `TablePage` (T016) and `TablePageTests` (T013).
- **US3 (Phase 5)**: depends on US2. It adds classes to `TrackpointTable` (T023).
- **Polish (Phase 6)**: depends on every story.

### Within Each Phase

- T003, T004, and T005 come before T006–T009.
- T006 comes before T007. T008 comes before T009. T009 comes before T010.
- T012–T014 come before T015–T016. T015 (`_Imports.razor`) comes before T016.
- T018–T022 come before T023–T025. T023 comes before T024.
- T027–T030 come before T031–T033. T031 comes before T032.
- Each phase ends with its gates task (T011, T017, T026, T034).

### Parallel Opportunities

- **Foundational tests**: T003 (parser) and T004 (messages) touch different files. T005 can be
  written alongside them, but it is listed after them because it is the largest.
- **Foundational implementation**: T006 (parser types) and T008 (Analysis types) touch different
  projects.
- **US1**: T012 and T014 are parallel with each other and with T013.
- **US2**: T018, T020, T021, and T022 are four different new or extended files. T025 (`app.css`)
  is parallel with T023 and T024.
- **US3**: T027, T029, and T030 are parallel. T031 and T033 are parallel.
- **Polish**: T035 is parallel with T036.

---

## Parallel Example: User Story 2

```text
# Write the failing tests together:
Task: "T018 TrackpointTableTests in tests/JoakimSoftware.TcxTool.Tests/Web/Table/TrackpointTableTests.cs"
Task: "T020 Loaded table markup in tests/JoakimSoftware.TcxTool.Tests/Web/TablePageEndpointTests.cs"
Task: "T021 20,000-row timing in tests/JoakimSoftware.TcxTool.Tests/Web/TablePagePerformanceTests.cs"
Task: "T022 scroll-box rules in tests/JoakimSoftware.TcxTool.Tests/Web/TableStylesTests.cs"

# Then, while T023 builds the component:
Task: "T025 scroll-box CSS in src/JoakimSoftware.TcxTool.Web/wwwroot/app.css"
```

## Parallel Example: User Story 3

```text
Task: "T027 TrackpointBandsTests in tests/JoakimSoftware.TcxTool.Tests/Web/Table/TrackpointBandsTests.cs"
Task: "T029 band classes and contrast in tests/JoakimSoftware.TcxTool.Tests/Web/TableStylesTests.cs"
Task: "T030 band classes over HTTP in tests/JoakimSoftware.TcxTool.Tests/Web/TablePageEndpointTests.cs"
# then
Task: "T031 TrackpointBands in src/JoakimSoftware.TcxTool.Web/Table/TrackpointBands.cs"
Task: "T033 band CSS in src/JoakimSoftware.TcxTool.Web/wwwroot/app.css"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup (T001–T002)
2. Phase 2: Foundational (T003–T011)
3. Phase 3: US1 (T012–T017)
4. **Stop and validate**: open `/table` with and without a current CSV. Quickstart scenarios 2 and
   5 apply.

### Incremental Delivery

1. Setup + Foundational: the reader and loader are tested, and no UI has changed.
2. US1: the header box and the states. This is the MVP.
3. US2: the full table. Check SC-002 by hand here (quickstart scenario 4), before US3 adds classes.
4. US3: the band colors.
5. Polish: `CLAUDE.md`, the Compose gate, and the quickstart run.

Each phase ends with gates 1–3 green, so the app is shippable at every checkpoint.

---

## Notes

- [P] tasks touch different files and don't depend on incomplete tasks.
- Do not write to `specs/*/checklists/` during `/speckit-implement` (`CLAUDE.md`, lessons from 001).
- Revert every temporary break with an edit, never by restoring a backup. If a restored file is
  older than the build output, `touch` it (`CLAUDE.md`, lessons from 002).
- Run `/speckit-analyze` before `/speckit-implement`, and fix CRITICAL and HIGH findings first.
