---

description: "Task list for feature 004-about-and-csv-state"
---

# Tasks: About Page and Current CSV State

**Input**: Design documents from `specs/004-about-and-csv-state/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included. The constitution requires test-first (Principle V, NON-NEGOTIABLE).

- Every test task comes before the code it covers, and must be run and seen failing first.
- A test that fails to compile because its subject type or member does not exist yet counts as
  failing. Add the subject in the next task.
- If a test passes before its code is written, show it failing with a temporary break in that code.
  Revert the break in the same task, with an edit rather than by restoring a backup (`CLAUDE.md`,
  lessons from 001 and 002).

**Organization**:
- Setup makes the test suite safe, so no test can reach the developer's real state file.
- Foundational builds the shared state store and notice.
- Then come US1 (convert records the current CSV), US2 (select on the home page), and US3 (About).
- US3 does not depend on Foundational. It can be done any time after Setup.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task belongs to (US1, US2, US3)
- All paths are relative to `tcxtool/`. Run every command from `tcxtool/`. The git root is the
  parent directory.

## Path Conventions

These come from plan.md → Project Structure:

- Web: `src/JoakimSoftware.TcxTool.Web/{Conversion,State,Components/Pages,Components/State,Components/Layout}/`
- Tests: `tests/JoakimSoftware.TcxTool.Tests/{Architecture,Support,Web,Web/State}/`
- The parser library `src/JoakimSoftware.TcxTool.Parser/` is not touched by any task.

**Conventions for every task**:

- **Code rules**:
  - one public type per file, file-scoped namespaces
  - `_camelCase` private fields
  - `Async` methods take a `CancellationToken`, and every `async Task` test name ends in `Async`
    (IDE1006)
  - no relaxed settings or suppressed rules
- **Test isolation (FR-020)**:
  - From T003 on, every HTTP test gets its host from `Support/TcxToolWebFactory`.
  - Every bUnit or service test passes `TcxToolOptions` whose `CsvOutputDirectory` and
    `StateFilePath` are inside a `TempDirectory`. No test may use a default or Development path.
- **Write failures in tests**: the Compose test container runs as root, so permission bits (`chmod`)
  cannot cause a failure. To force an I/O failure, use a path that is structurally impossible:
  - a state path whose parent is an existing **file**, which makes `CreateDirectory` fail
  - a state path that is an existing **directory**, which makes the final `File.Move` fail
- **Messages**: user-facing text comes only from `CsvSelectionMessages`. Tests compare against
  `CsvSelectionMessages.For(...)`, or against the literal strings in `contracts/pages.md`, never
  against strings invented in the test.
- **Real data**: never read `data/tcx/` into context. Use the fixtures in
  `tests/JoakimSoftware.TcxTool.Tests/Fixtures/`.

---

## Phase 1: Setup (Test Isolation)

**Purpose**: before any code knows about a state file, make it impossible for a test to reach the
real `tcxtool/` paths (research.md R7).

- [X] T001 Create the guard test `tests/JoakimSoftware.TcxTool.Tests/Architecture/TestIsolationTests.cs`.
  - It enumerates every `*.cs` file under `RepositoryRoot.Combine("tests/JoakimSoftware.TcxTool.Tests")`,
    excluding `bin/`, `obj/`, `Support/`, and `Architecture/TestIsolationTests.cs` itself. The guard
    names the forbidden pattern in its own regex and doc comment, so scanning itself would flag
    itself.
  - It fails, naming the files, if any of them constructs `WebApplicationFactory<Program>` directly:
    the regex `new\s+WebApplicationFactory<Program>`, or a declaration of type
    `WebApplicationFactory<Program>` initialized with a target-typed `new()`.
  - Run it and confirm it **fails**, listing `Web/ConvertEndpointTests.cs` and
    `Web/PlaceholderPageEndpointTests.cs`. Those two files make it fail on its own, so no temporary
    break is needed.
- [X] T002 Create `tests/JoakimSoftware.TcxTool.Tests/Support/TcxToolWebFactory.cs`, a sealed
  `IDisposable` that owns a `TempDirectory`.
  - Its constructor takes optional extra settings (`IDictionary<string, string?>?`). It builds a
    `WebApplicationFactory<Program>` with `WithWebHostBuilder` and `ConfigureAppConfiguration`, adding
    an in-memory collection with:
    - `TcxTool:CsvOutputDirectory` = `<temp>/csv` (the directory is created)
    - `TcxTool:StateFilePath` = `<temp>/state/tcxtool-state.json` (not created)
    - then the extra settings, which win
  - Members:
    - `Factory` (the `WebApplicationFactory<Program>`)
    - `CsvDirectory` and `StateFilePath` (absolute paths)
    - `CreateClient(WebApplicationFactoryClientOptions? options = null)`
    - `Dispose()`, which disposes the factory and then the temp directory
  - The `StateFilePath` setting is not read by the app until T010. Setting it early is harmless.
- [X] T003 Move both endpoint test classes to `TcxToolWebFactory`.
  - `tests/JoakimSoftware.TcxTool.Tests/Web/ConvertEndpointTests.cs`:
    - replace `_output` and `CreateFactory` with a `TcxToolWebFactory` field
    - `CreateSmallLimitFactory()` returns a second `TcxToolWebFactory` with `TcxTool:MaxUploadBytes`
    - `_output.FileNames()` becomes a listing of `factory.CsvDirectory`
    - keep `UploadAsync` as an `internal static` helper that takes a `TcxToolWebFactory` (or its
      `Factory`)
    - keep every assertion unchanged
  - `tests/JoakimSoftware.TcxTool.Tests/Web/PlaceholderPageEndpointTests.cs`: use `TcxToolWebFactory`
    in place of the bare factory.
  - *Done note*: T001's guard also flagged `Web/BootstrapAssetsTests.cs`, a third bare factory that
    plan.md missed. It moved to `TcxToolWebFactory` in the same way.
  - Run `dotnet test JoakimSoftware.TcxTool.slnx`. Confirm T001's guard now passes and every existing
    test still passes.
- [X] T004 [P] First, show the check failing. With no rule present, `git check-ignore -v tcxtool-state.json`
  exits `1`, and `grep -n 'tcxtool-state.json' .dockerignore` finds nothing. Then add
  `tcxtool-state.json` to `.gitignore`, with a comment that it is machine-local app state (feature 004,
  FR-021). Add `tcxtool-state.json` and `**/tcxtool-state.json` to `.dockerignore`,
  under a comment that names FR-021 (research.md R11). Re-run both commands on the host and confirm
  that both now match. T035 confirms the `.dockerignore` rule against the built image. Do **not** add a test that reads `.gitignore`: `.dockerignore`
  excludes `.gitignore` from the build context, so such a test would fail in the Compose gate.

**Checkpoint**: the suite is green, and no test can build a host that uses the developer's paths.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: the option, the name rule, the state types, the store's read and select paths, and the
notice component, which US1 and US2 both use.

**⚠️ CRITICAL**: US1 and US2 cannot begin until this phase is complete. US3 does not need it.

### Tests first

- [X] T005 [P] Extend `tests/JoakimSoftware.TcxTool.Tests/Web/AppSettingsTests.cs` (research.md R2, contracts/state-file.md "File"):
  - `appsettings.json` → `TcxTool.StateFilePath` is `tcxtool-state.json`
  - `appsettings.Development.json` → `TcxTool.StateFilePath` is `../../tcxtool-state.json`
  - a new test reads `compose.yaml` as text and asserts that the `app` service's `environment` sets
    `TcxTool__StateFilePath: /tmp/tcxtool/tcxtool-state.json`. `compose.yaml` is in the build
    context, so this test also runs in the Compose gate.

  Run them and confirm all three **fail**.
- [X] T006 [P] Create `tests/JoakimSoftware.TcxTool.Tests/Web/TcxToolPathsTests.cs`:
  - `new TcxToolOptions().StateFilePath` is `tcxtool-state.json`
  - `StateFilePath` carries `[Required(AllowEmptyStrings = false)]`: validating an options object
    with `""` through `Validator.TryValidateObject(..., validateAllProperties: true)` fails
  - `TcxToolPaths.StateFile` and `TcxToolPaths.CsvDirectory` resolve a relative value against
    `IHostEnvironment.ContentRootPath`, and return an absolute value unchanged (normalized with
    `Path.GetFullPath`)

  Confirm it fails to compile.
- [X] T007 [P] Create `tests/JoakimSoftware.TcxTool.Tests/Web/State/CsvFileNameRuleTests.cs`. It is
  a `[Theory]` over every row of the name-rule table in contracts/state-file.md "Name rule", plus
  `null`, `.`, `..`, and a name containing `\0`. Confirm it fails to compile.
- [X] T008 Create `tests/JoakimSoftware.TcxTool.Tests/Web/State/CurrentCsvStoreTests.cs`, with the
  **read** tests (contracts/state-file.md "Read rules").
  - Setup: each test builds a `CurrentCsvStore` from:
    - `TcxToolOptions` whose CSV directory and state path are inside a `TempDirectory`
    - a `HostingEnvironment` whose content root is that temp directory
    - a `ListLogger<CurrentCsvStore>`
  - Cases:
    - no file → `CurrentCsv.None`, and no log entry
    - a file over 65,536 bytes → `None`, and one warning containing the path and `larger than 64 KiB`
    - `nope` → `None`, warning `not valid JSON`
    - `[]` → `None`, warning `not valid JSON`
    - `{}` or `{"csvFilename": 5}` → `None`, warning `no csvFilename string`
    - `{"csvFilename": "../x.csv"}` → `None`, warning `csvFilename is not a plain CSV name`
    - a valid name whose file exists in the CSV directory → `Present(name)`
    - a valid name with no such file → `Missing(name)`
    - extra properties alongside a valid name → still `Present` or `Missing`
    - a state path that is a directory → `None`
  - For every warning case, also assert that the log entry does not contain the file's content.

  Confirm it fails to compile.
- [X] T009 Add the **select** tests to `tests/JoakimSoftware.TcxTool.Tests/Web/State/CurrentCsvStoreTests.cs`
  (contracts/state-file.md "Content written" and "Write procedure"; data-model.md
  `CsvSelectionOutcome`):
  - **Rejections**, each leaving a pre-seeded state file byte-for-byte unchanged:
    - `null`, `""`, `"  "` → `NoneChosen`, and `FileName` is null
    - `../secrets.csv`, `/etc/passwd.csv`, `REPORT.CSV` → `InvalidName`
    - a valid name that is not in the CSV directory → `NotFound`
  - **Recorded**: a valid existing name → `Recorded`, `IsRecorded` true. The state file's bytes
    equal exactly `{\n  "csvFilename": "<name>"\n}\n`, in UTF-8 with no BOM. The parent directory is
    created when missing. A **new** `CurrentCsvStore` instance built from the same options, standing in for an app
    restart (SC-005), returns `Present(name)` from `ReadAsync`. One Information entry
    reads "Current CSV set to <name>".
  - **Replace**: selecting a second name replaces the first, and the state directory then holds
    only `tcxtool-state.json`, with no `*.tmp` files.
  - **WriteFailed, parent is a file**: the state path's parent is an existing regular file →
    `WriteFailed`. Nothing is created, and one warning contains the path and the exception type
    name.
  - **WriteFailed, path is a directory**: the state path is an existing directory → `WriteFailed`,
    and the parent directory has no `*.tmp` file left behind.
  - **Concurrency**: 20 concurrent `SelectAsync` calls alternating between two valid names. Each
    returns `Recorded`, and afterwards the file parses and names one of the two.

  Confirm the new tests fail to compile, or fail.
- [X] T010 [P] Create `tests/JoakimSoftware.TcxTool.Tests/Web/CurrentCsvNoticeTests.cs` (bUnit,
  contracts/pages.md "CurrentCsvNotice" and "Messages"):
  - `null` renders nothing
  - `Recorded` renders `p.mt-3.mb-0[role=status]` with the text `Current CSV: <name>` and the name
    in a `strong`
  - each of `NoneChosen`, `InvalidName`, `NotFound`, and `WriteFailed` renders
    `div.alert.alert-warning.mt-3[role=alert] > p.mb-0`, with the exact message from
    contracts/pages.md "Messages"
  - a separate `[Theory]` asserts that `CsvSelectionMessages.For` returns those four exact strings

  Confirm it fails to compile.

### Implementation

- [X] T011 [P] Add `StateFilePath` to both settings files: `tcxtool-state.json` in
  `src/JoakimSoftware.TcxTool.Web/appsettings.json`, and `../../tcxtool-state.json` in
  `src/JoakimSoftware.TcxTool.Web/appsettings.Development.json`, as data-model.md
  `TcxToolOptions.StateFilePath` and research.md R2 specify. Add
  `TcxTool__StateFilePath: /tmp/tcxtool/tcxtool-state.json` to the `app` service's `environment` in
  `compose.yaml`, with a comment that `/app` is not writable by the non-root user and that the state
  is lost when the container is removed. T005 passes.
- [X] T012 Add `StateFilePath` to `src/JoakimSoftware.TcxTool.Web/Conversion/TcxToolOptions.cs`, per
  data-model.md `TcxToolOptions.StateFilePath`, with an XML doc comment. Create
  `src/JoakimSoftware.TcxTool.Web/Conversion/TcxToolPaths.cs`, a `public static` class per
  data-model.md `TcxToolPaths`, with XML doc comments. It must be public because T006 tests it from
  the test project, and the Web project has no `InternalsVisibleTo`.
  Change `CsvConversionService.OutputDirectory()` in
  `src/JoakimSoftware.TcxTool.Web/Conversion/CsvConversionService.cs` to call
  `TcxToolPaths.CsvDirectory`, with no change in behavior. T006 passes, and `CsvConversionServiceTests`
  and `ConvertEndpointTests` stay green.
- [X] T013 [P] Create `src/JoakimSoftware.TcxTool.Web/State/CsvFileNameRule.cs` with
  `IsPlainCsvName`, per data-model.md `CsvFileNameRule` and contracts/state-file.md "Name rule"
  rules 1–5. T007 passes.
- [X] T014 [P] Create the state value types, one per file, in `src/JoakimSoftware.TcxTool.Web/State/`:
  - `CurrentCsvStatus.cs`: data-model.md `CurrentCsvStatus` (`None`, `Present`, `Missing`)
  - `CurrentCsv.cs`: data-model.md `CurrentCsv.FileName` and `CurrentCsv.Status`, with the `None`,
    `Present(string)`, and `Missing(string)` members. The invariant: `FileName` is null exactly when
    `Status` is `None`.
  - `CsvSelectionOutcome.cs`: data-model.md `CsvSelectionOutcome`, all five values
  - `CsvSelectionResult.cs`: data-model.md `CsvSelectionResult.Outcome`,
    `CsvSelectionResult.FileName`, and `CsvSelectionResult.IsRecorded`
  - `CsvSelectionMessages.cs`: data-model.md `CsvSelectionMessages`, with the four strings from
    contracts/pages.md "Messages"
  - `TcxToolState.cs`: an internal record, data-model.md `TcxToolState.CsvFilename`, serialized as
    `csvFilename`

  Each public type gets an XML doc comment.
- [X] T015 Create `src/JoakimSoftware.TcxTool.Web/State/CurrentCsvStore.cs`: a `sealed partial`
  class with `ReadAsync` and `SelectAsync`, per data-model.md `CurrentCsvStore`.
  - Reading follows research.md R3 and contracts/state-file.md "Read rules": a 64 KiB cap, and
    `JsonDocument` parsing.
  - Writing follows contracts/state-file.md "Write procedure": an indented `TcxToolState`, UTF-8
    without BOM, a trailing `\n`, then temp-file-then-rename.
  - The selection checks are the name rule, then containment in the CSV directory, then existence.
  - Writes are serialized by a `SemaphoreSlim`.
  - The three `[LoggerMessage]` methods are those in research.md R12.
  - Exceptions to catch:
    - around file access (read, `CreateDirectory`, write, `File.Move`), catch `IOException` and
      `UnauthorizedAccessException`
    - around `JsonDocument.Parse`, catch `JsonException`
    - catch nothing else: `OperationCanceledException` and programming errors propagate
  - Every caught case maps to a row in contracts/state-file.md "Read rules" or to `WriteFailed`, so
    the store never throws for bad input or an I/O failure (data-model.md `CurrentCsvStore`,
    spec FR-008).
  - Register it in `src/JoakimSoftware.TcxTool.Web/Program.cs` with
    `builder.Services.AddSingleton<CurrentCsvStore>();`.

  T008 and T009 pass.
- [X] T016 Create `src/JoakimSoftware.TcxTool.Web/Components/State/CurrentCsvNotice.razor`, per
  contracts/pages.md "CurrentCsvNotice", with `[Parameter] public CsvSelectionResult? Result`. Add
  `@using JoakimSoftware.TcxTool.Web.State` and `@using JoakimSoftware.TcxTool.Web.Components.State`
  to `src/JoakimSoftware.TcxTool.Web/Components/_Imports.razor`. T010 passes.
- [X] T017 Run quality gates 1–3: `dotnet build`, `dotnet format --verify-no-changes`, and
  `dotnet test` on `JoakimSoftware.TcxTool.slnx`. All must pass.

**Checkpoint**: the store and the notice exist and are tested. No page uses them yet.

---

## Phase 3: User Story 1 - Converting a TCX file makes its CSV the current one (Priority: P1) 🎯 MVP

**Goal**: a successful conversion records its first CSV in `tcxtool-state.json` and says so. A
failed one changes nothing.

**Independent Test**: upload a fixture on `/convert`. The page shows `Current CSV: <name>`, and the
state file holds exactly that name. Upload `malformed.tcx`: no notice appears, and the state file is
unchanged.

### Tests for User Story 1 ⚠️

- [X] T018 [US1] Add `RecordConversionAsync` tests to
  `tests/JoakimSoftware.TcxTool.Tests/Web/State/CurrentCsvStoreTests.cs` (research.md R8):
  - a `ConversionResult.Failure(...)` returns `null`, and a pre-seeded state file is unchanged
  - a `ConversionResult.Success([])` returns `null`
  - a success with two `SavedCsv` entries, both present in the CSV directory, returns `Recorded` for
    the **first** entry, and the state file names it

  Confirm they fail to compile.
- [X] T019 [US1] Extend `tests/JoakimSoftware.TcxTool.Tests/Web/ConvertEndpointTests.cs`
  (contracts/pages.md "Convert"; spec US1 scenarios 1–4):
  - uploading `run-two-laps.tcx` writes the state file with exactly
    `{\n  "csvFilename": "2026-11-01_run-two-laps.csv"\n}\n`, and the body contains `role="status"`
    and `Current CSV: <strong>2026-11-01_run-two-laps.csv</strong>`
  - uploading `multi-activity.tcx` records `2026-11-01_071500_multi-activity.csv`, the first file in
    the result list (check it against `Fixtures/Expected/`)
  - uploading a second fixture after the first replaces `csvFilename`
  - a failed upload (`malformed.tcx`, and posting with no file), after a pre-seeded valid state file,
    leaves the state file byte-for-byte unchanged, and the body has no `Current CSV:`
  - with a `TcxToolWebFactory` whose `TcxTool:StateFilePath` is an existing directory, the CSV is
    still saved and listed. Create that directory with a second `TempDirectory`, owned by the test,
    because the factory fixes its settings when it is constructed.
    The body also contains the `WriteFailed` message from `CsvSelectionMessages`.

  Confirm the new tests fail.
  - *Done note*: 4 of the 5 new tests failed before T022. `AFailedConversionLeavesTheCurrentCsvAloneAsync`
    passed from the start, because it guards against a write. Its logic failed first at the store
    level, in T018's `AFailedConversionRecordsNothingAsync`.
- [X] T020 [P] [US1] Register the store in `tests/JoakimSoftware.TcxTool.Tests/Web/ConvertPageTests.cs`:
  `Services.AddSingleton<CurrentCsvStore>()`, with `TcxToolOptions` and a content root inside a
  `TempDirectory` that is disposed with the test class, in place of `Path.GetTempPath()`. Existing
  assertions are unchanged, and the class still passes.

### Implementation for User Story 1

- [X] T021 [US1] Add `RecordConversionAsync` to `src/JoakimSoftware.TcxTool.Web/State/CurrentCsvStore.cs`,
  per data-model.md `CurrentCsvStore` and research.md R8. T018 passes.
- [X] T022 [US1] Update `src/JoakimSoftware.TcxTool.Web/Components/Pages/ConvertPage.razor.cs`:
  - inject `CurrentCsvStore`
  - add a private `CsvSelectionResult? Recorded` property
  - in `ConvertAsync`, after `ConversionService.ConvertAsync`, set
    `Recorded = await CurrentCsvStore.RecordConversionAsync(Result, cancellationToken)`

  Render `<CurrentCsvNotice Result="Recorded" />` directly after `<ConversionResultView ... />` in
  `src/JoakimSoftware.TcxTool.Web/Components/Pages/ConvertPage.razor`. The too-large GET path leaves
  `Recorded` null. T019 passes, and every existing `ConvertEndpointTests` and `ConvertPageTests`
  assertion still passes.

**Checkpoint**: US1 is complete. Conversions now set the current CSV, which is the MVP for the
analysis pages.

---

## Phase 4: User Story 2 - Select an earlier CSV from the home page (Priority: P2)

**Goal**: the home page's "Select CSV File" card lists the CSVs newest first, shows the current
one, and records a new choice.

**Independent Test**: with two CSVs in the CSV directory, open `/`, choose the one that is not
current, and click Select. The card shows `Current CSV: <that name>`, and the state file names it.

### Tests for User Story 2 ⚠️

- [X] T023 [US2] Add `ListCsvFiles` tests to
  `tests/JoakimSoftware.TcxTool.Tests/Web/State/CurrentCsvStoreTests.cs` (research.md R5):
  - a missing CSV directory returns `[]`
  - the result keeps only names that pass `CsvFileNameRule`: `notes.txt`, `REPORT.CSV`, and a
    subdirectory named `sub.csv` are excluded
  - `2026-11-01_b.csv`, `2025-03-02_a.csv`, and `2026-11-01_a.csv` come back as `2026-11-01_b.csv`,
    `2026-11-01_a.csv`, `2025-03-02_a.csv` (descending ordinal)

  Confirm they fail to compile.
- [X] T024 [P] [US2] Extend `tests/JoakimSoftware.TcxTool.Tests/Web/HomePageTests.cs` (bUnit,
  contracts/pages.md "Home").
  - **Setup**: register `CurrentCsvStore` with `TcxToolOptions` and a content root inside a
    `TempDirectory`, disposed with the class. The four existing tests must keep passing unchanged.
  - **Card**:
    - a second column with the same `.col-sm-6.col-lg-4` classes as the Convert column
    - `.card.h-100 > .card-body#select-csv`
    - `h2.card-title.h5` "Select CSV File", and the description text
  - **With 2 CSVs and no state**:
    - the form has `method=post`
    - the select is `select.form-select#csv-file[name=CsvFilename]` with `label[for=csv-file]`
    - the first option has an empty value, is `disabled` and `selected`, and reads "Choose a CSV
      file"
    - the other options come newest first
    - the button is `button.btn.btn-primary[type=submit]` "Select"
    - `#current-csv` reads `No CSV selected yet.`
  - **With a state file naming an existing CSV**: that option is `selected`, and `#current-csv`
    reads `Current CSV: <name>` with the name in `strong`.
  - **With a state file naming a missing CSV**: `#current-csv` ends with `(file missing)`.
  - **With a malformed state file** (`nope`): the page renders without an exception, and
    `#current-csv` reads `No CSV selected yet.` (SC-004).
  - **With no CSVs**: no `form` in `#select-csv`, the text `No CSV files yet.`, and
    `a[href=convert]` "Convert a TCX file".

  Confirm the new tests fail.
- [X] T025 [P] [US2] Create `tests/JoakimSoftware.TcxTool.Tests/Web/HomeSelectEndpointTests.cs`
  (HTTP, through `TcxToolWebFactory`; contracts/pages.md "POST `/`").
  - Seed two CSV files in `factory.CsvDirectory`. Post `FormUrlEncodedContent` with
    `_handler=csv-select` and `CsvFilename=<value>` to `/`.
  - Cases:
    - **valid name**: `200`, the state file names it, and the body contains `Current CSV:` and the
      name
    - **empty value**: the `NoneChosen` message, and the state file is unchanged
    - **`../secrets.csv`**: the `InvalidName` message, and the state file is unchanged
    - **a valid name not on disk**: the `NotFound` message, and the state file is unchanged
    - **`Sec-Fetch-Site: cross-site`**: `400`, and the state file is unchanged
    - **GET `/`**: `200`, with the options listed
  - "Unchanged" means a pre-seeded state file keeps the same bytes, or a missing one is still
    missing.
  - Name every test `…Async`.

  Confirm they fail.
  - *Done note*: every case failed before T027 except the cross-site one. Antiforgery rejects a
    cross-site POST with `400` whether or not a form handler exists, so that test is a guard from
    the start.

### Implementation for User Story 2

- [X] T026 [US2] Add `ListCsvFiles` to `src/JoakimSoftware.TcxTool.Web/State/CurrentCsvStore.cs`,
  per data-model.md `CurrentCsvStore` and research.md R5. T023 passes.
- [X] T027 [US2] Build the Select CSV File card, per contracts/pages.md "Home" and research.md R6.
  - Create `src/JoakimSoftware.TcxTool.Web/Components/Pages/Home.razor.cs` (partial class `Home`):
    - inject `CurrentCsvStore`
    - `[CascadingParameter] HttpContext?`
    - `[SupplyParameterFromForm(FormName = "csv-select")] public string? CsvFilename`
    - private `Current` (`CurrentCsv`), `Files`, and `Selection` (`CsvSelectionResult?`)
    - `OnInitializedAsync` reads `Current` and `Files`
    - the submit handler calls `SelectAsync` with `HttpContext?.RequestAborted`, then re-reads
      `Current`
  - In `src/JoakimSoftware.TcxTool.Web/Components/Pages/Home.razor`, add the second column with:
    - the current CSV line, with text by data-model.md `CurrentCsv.Status`
    - the form, with `<AntiforgeryToken />` and the placeholder option
    - the empty state
    - `<CurrentCsvNotice Result="Selection" />`

  Keep the existing heading, description, and Convert card byte-for-byte. Add no `@rendermode`.
  T024 and T025 pass.

**Checkpoint**: US1 and US2 both work. The current CSV can be set by converting or by selecting.

---

## Phase 5: User Story 3 - Read about the app (Priority: P3)

**Goal**: an `/about` page with the FR-003 content, linked last in the navbar.

**Independent Test**: from any page, click About. The page shows the content and links in
contracts/pages.md "About", and its navbar link is active.

### Tests for User Story 3 ⚠️

- [X] T028 [P] [US3] Update `NavbarLinksOnlyToPagesThatExist` in
  `tests/JoakimSoftware.TcxTool.Tests/Web/MainLayoutTests.cs` to expect `("About", "about")` after
  `("Timeline", "timeline")` (contracts/pages.md "Navbar"). Confirm it fails.
- [X] T029 [P] [US3] Create `tests/JoakimSoftware.TcxTool.Tests/Web/AboutPageTests.cs` (bUnit,
  contracts/pages.md "About"):
  - exactly one `h1.h3.mb-3` with the text "About"
  - the intro `p`'s whitespace-collapsed text equals the FR-003 paragraph, and its `strong`
    elements are `TcxTool`, `Chris Joakim`, `Software Factory`, `Claude Code`, and `Spec-Kit`, in
    that order
  - the `h2` texts appear in contract order
  - the startup list has 1 `li` and the loop list has 5, with the exact FR-003 texts
  - the links list has 5 `li > a`, whose texts and `href`s equal the contract table
  - the page has no `img`, `script`, or `link` elements

  Confirm it fails to compile.
- [X] T030 [P] [US3] Create `tests/JoakimSoftware.TcxTool.Tests/Web/AboutPageEndpointTests.cs`
  (HTTP, through `TcxToolWebFactory`):
  - GET `/about` returns `200` and the body contains `<title>About</title>` and `>About</h1>`
  - parse the body with AngleSharp (from bUnit) and assert that the `nav a.nav-link[href=about]` is
    the only nav link with the class `active`

  Confirm it fails.

### Implementation for User Story 3

- [X] T031 [US3] Create `src/JoakimSoftware.TcxTool.Web/Components/Pages/AboutPage.razor` with
  `@page "/about"`, `<PageTitle>About</PageTitle>`, and the FR-003 content as markup, per
  contracts/pages.md "About" and research.md R10. It is static, with no code-behind, services, or
  `@rendermode`. T029 passes.
- [X] T032 [US3] Add `<NavLink class="nav-link" href="about">About</NavLink>` as the last link in
  `src/JoakimSoftware.TcxTool.Web/Components/Layout/MainLayout.razor`. T028 and T030 pass.

**Checkpoint**: all three stories work independently.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T033 [P] Update `CLAUDE.md`:
  - add a feature history row: `specs/004-about-and-csv-state`, Complete, with About page,
    `tcxtool-state.json` current CSV, home page Select CSV File card, and the convert notice
  - describe the Web `State/` namespace: `CurrentCsvStore` is the only reader and writer of the state
    file, and later pages read the current CSV through it
  - add the rule that HTTP tests use `Support/TcxToolWebFactory` and never construct
    `WebApplicationFactory<Program>` directly, enforced by `Architecture/TestIsolationTests`, because
    a bare factory runs in Development against the real `tcxtool/` paths
  - record the lessons:
    - no test may read `.gitignore`, which is excluded from the Docker build context
    - force write failures with impossible paths, because the test container runs as root
  - list `tcxtool-state.json` under Repository layout as git-ignored, machine-local state, at
    `/tmp/tcxtool/` in the container
- [X] T034 Run quality gates 1–3 (`dotnet build`, `dotnet format --verify-no-changes`, `dotnet test`
  on `JoakimSoftware.TcxTool.slnx`). Fix code, not settings, until all pass.
- [X] T035 Run quality gate 4.
  - Confirm `docker info` succeeds, then run
    `docker compose -f compose.yaml -f compose.test.yaml up --build --abort-on-container-exit --exit-code-from tests`,
    and confirm `echo $?` prints `0`. Use `TCXTOOL_HOST_PORT=8081` if 8080 is busy.
  - If `tcxtool-state.json` exists on the host, confirm it did not enter the image:
    `docker run --rm --entrypoint ls tcxtool-tests:latest /src/tcxtool-state.json` must fail.
  - Finish with `docker compose down`.
- [X] T036 Walk through quickstart.md scenarios 2–4 and 6 against `dotnet run`.
  - Note the state file's checksum before T034 and compare it after.
  - Delete only the CSVs and the state file the walkthrough created. Leave files that git
    already tracks.
  - Do not paste the contents of `data/tcx/` or generated CSVs into the conversation.
  - *Done note*:
    - Scenarios 2–4 passed against `dotnet run`, driven with `curl`: convert `24964064020.tcx`,
      the malformed upload, a restart, a selection, a missing file, a corrupt state file, a
      cross-site 400, and `/about`.
    - Scenario 6 passed: no `tcxtool-state.json` existed before, during, or after the test runs.
    - The walkthrough's state file and `2026-11-01_24964064020.csv` were deleted afterwards. The
      tracked CSV was only selected, never rewritten.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (T001–T004)**: no dependencies. T001 runs before T002 and T003, so it fails first.
- **Foundational (T005–T017)**: depends on Setup, because tests use `TcxToolWebFactory` and
  temp-directory options. It blocks US1 and US2.
- **US1 (T018–T022)**: depends on Foundational.
- **US2 (T023–T027)**: depends on Foundational. It reuses `CurrentCsvNotice` but nothing else from
  US1, so it can run before or alongside US1.
- **US3 (T028–T032)**: depends on Setup only, because T030 uses `TcxToolWebFactory`.
- **Polish (T033–T036)**: depends on every story the release includes.

### Within phases

- T005, T006, T007, and T010 are independent tests, and can be written in parallel.
- T008 comes before T009: both are in `CurrentCsvStoreTests.cs`.
- T011 needs T005. T012 needs T006. T013 needs T007. T014 has no dependencies within the phase.
- T015 needs T008, T009, T012, T013, and T014. T016 needs T010 and T014.
- `CurrentCsvStoreTests.cs` is extended by T008, T009, T018, and T023 in sequence. Never mark two of
  them as parallel.
- `CurrentCsvStore.cs` is extended by T015, T021, and T026 in sequence.
- In US1: T018 → T021, and T019 + T020 → T022.
- In US2: T023 → T026, and T024 + T025 → T027.
- In US3: T028, T029, and T030 can run in parallel. T029 → T031, and T028 + T030 → T032. T030's
  active-link check needs T031's page to exist.

### Parallel Opportunities

```text
Setup:        T004 alongside T001–T003
Foundational: T005 | T006 | T007 | T010   (tests), then T011 | T013 | T014   (implementation)
US1:          T020 alongside T018/T019
US2:          T024 | T025 after T023
US3:          T028 | T029 | T030, and the whole phase alongside Foundational, US1, or US2
Polish:       T033 alongside T034
```

## Parallel Example: User Story 3

```text
Task: "T028 Update the navbar expectation in tests/JoakimSoftware.TcxTool.Tests/Web/MainLayoutTests.cs"
Task: "T029 Create tests/JoakimSoftware.TcxTool.Tests/Web/AboutPageTests.cs"
Task: "T030 Create tests/JoakimSoftware.TcxTool.Tests/Web/AboutPageEndpointTests.cs"
```

## Parallel Example: Foundational tests

```text
Task: "T005 Extend tests/JoakimSoftware.TcxTool.Tests/Web/AppSettingsTests.cs"
Task: "T006 Create tests/JoakimSoftware.TcxTool.Tests/Web/TcxToolPathsTests.cs"
Task: "T007 Create tests/JoakimSoftware.TcxTool.Tests/Web/State/CsvFileNameRuleTests.cs"
Task: "T010 Create tests/JoakimSoftware.TcxTool.Tests/Web/CurrentCsvNoticeTests.cs"
```

---

## Implementation Strategy

### MVP first (User Story 1)

1. Setup (T001–T004): the suite is isolated from the real paths.
2. Foundational (T005–T017): the store and the notice.
3. US1 (T018–T022): conversions record the current CSV.
4. **Stop and validate**: quickstart.md scenario 2. The analysis features can start from here.

### Incremental delivery

1. Then US2 (T023–T027): select an earlier CSV. Validate with quickstart scenario 3.
2. Then US3 (T028–T032): the About page. Validate with quickstart scenario 4. US3 can also go
   first, since it is independent.
3. Then Polish (T033–T036): `CLAUDE.md`, all four quality gates, and the quickstart walkthrough.

## Notes

- Checklists under `specs/004-about-and-csv-state/checklists/` are read-only during
  `/speckit-implement`. No task writes to them.
- The parser library, the Dockerfile, and `compose.test.yaml` are not changed by any task.
- The commented-out `# data/csv/` line in `.gitignore` is deliberately out of scope.

---

## Phase 7: Convergence

- [X] T037 Make a `csv-select` POST to `/` render the page with the `NotFound` (or `NoneChosen`) notice and status `200`, even when the CSV folder lists no files, instead of Blazor's bare `400 Cannot submit the form 'csv-select' because no form on the page currently has that name.`. Today this happens when the last CSV was deleted after the page loaded; verified on a probe instance with an empty CSV directory. Per US2/AC5 and SC-004 (partial).
  - Test first: add a case to `tests/JoakimSoftware.TcxTool.Tests/Web/HomeSelectEndpointTests.cs`. With an empty `CsvDirectory`, post `CsvFilename=2026-11-01_gone.csv`, then assert `200`, the `CsvSelectionMessages.For(CsvSelectionOutcome.NotFound)` text, and no state file. Confirm it fails with `400`.
  - Fix in `src/JoakimSoftware.TcxTool.Web/Components/Pages/Home.razor`: in the empty state, keep an `@formname="csv-select"` `<form>` element so Blazor can dispatch the POST. It holds only `<AntiforgeryToken />`, with no select or button, so nothing visible changes. The empty-state text and the `convert` link stay as they are.
  - Update the `HomePageTests.WithNoCsvsThereIsNoFormAndALinkToConvert` assertion to "no `select` and no submit button in `#select-csv`". Update the Home "Empty state" row in `specs/004-about-and-csv-state/contracts/pages.md` to match.
  - Re-run quality gates 1–3.
- [X] T038 Review `tests/JoakimSoftware.TcxTool.Tests/Support/BunitTcxToolServices.cs`, a helper that plan.md's source tree does not list. It registers the store with temp-directory paths for the bUnit page tests, so it serves FR-020. Keep it, and record it in plan.md's source tree on the next plan revision, or fold it back into `ConvertPageTests` and `HomePageTests`. Per plan: Project Structure (unrequested).
