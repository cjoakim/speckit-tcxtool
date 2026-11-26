# CLAUDE.md

Guidance for Claude Code when working in `tcxtool/`, the Spec Kit project for
`JoakimSoftware.TcxTool`: a .NET 11 Blazor Web App that parses Garmin TCX files,
converts them to CSV, analyzes the CSV, and charts a single activity.

## The constitution governs

`.specify/memory/constitution.md` is the authority for stack, style, testing,
Docker, and quality gates. Read it before any design or code work. When this
file and the constitution disagree, the constitution wins. Do not restate or
reinterpret its rules here; amend it with `/speckit-constitution`.

The rules most easily broken by accident:

- XML parsing uses the BCL only, always through a secure `XmlReader`
  (Principle IV). Never `XDocument.Parse` or `XmlDocument.Load` on a string or path.
- Tests come first and must fail before implementation (Principle V).
- A change is done only when every Quality Gate passes, including the Docker
  Compose gate (Principle VII). A green host `dotnet test` is not enough.

## Spec Kit workflow

Features move through these commands, in order:

1. `/speckit-specify`: write `specs/<feature>/spec.md`
2. `/speckit-clarify`: resolve open questions in the spec
3. `/speckit-plan`: produce `plan.md` and design artifacts
4. `/speckit-tasks`: produce `tasks.md`
5. `/speckit-analyze`: check spec, plan, and tasks for consistency
6. `/speckit-implement`: execute `tasks.md`

7. `/speckit-converge`: after implementing, check the code against the
   artifacts and append any remaining work to `tasks.md`

`/speckit-checklist` is available as needed.

- Do not write application code outside `/speckit-implement` unless asked.
- Do not edit `.specify/templates/`, `.specify/scripts/`, or
  `.claude/skills/speckit-*`. They are managed by the Specify CLI (v1.4.3).
- `.specify/feature.json` points at the current feature directory. It is
  rewritten by `/speckit-specify`, is machine-local, and is git-ignored.

### Lessons from feature 001

- **No branch hook is installed.** `/speckit-specify` creates `specs/NNN-name/`
  but no git branch, and the plan's "Branch" field is only the feature id. If
  a feature branch is wanted, create it with `git switch -c NNN-name` before
  `/speckit-plan`.
- **Always run `/speckit-analyze` before `/speckit-implement`, and fix CRITICAL
  and HIGH findings first.** In 001 it caught a Principle V violation: tasks
  added project settings before the tests that check them.
- **Write tasks in test-first order.**
  - A test that checks configuration or structure must come before the task
    that adds that configuration, so it fails on its own.
  - A test that guards against something being added (such as parser
    independence) passes from the start. Show it failing with a temporary
    break that is reverted in the same task.
- **Checklists under `specs/*/checklists/` are read-only during
  `/speckit-implement`.** Don't write tasks that record results there.
- **Specs for infrastructure features** name technology only where the
  constitution already requires it. Leave tool, template, and path choices to
  the plan.
- **Ground plans in the real machine.** Before deciding on templates or
  versions, generate throwaway projects in the scratchpad and query NuGet.
  Template output and package versions on SDK 11 differ from older
  documentation.

### Lessons from feature 002

- **Antiforgery on .NET 11 renders no hidden token.** `<AntiforgeryToken />` writes nothing; the
  check uses `Sec-Fetch-Site` and `Origin`, and a cross-site POST gets 400. Test that rejection,
  not a `__RequestVerificationToken` field.
- **The `Async` suffix rule (IDE1006) covers test methods too.** Name every `async Task` test
  `…Async`, or the build fails.
- **Revert a temporary break with an edit, not by `mv`-ing a backup back.** The restored file is
  older than the build output, so MSBuild skips the recompile and the suite keeps running the
  broken code. If you do restore a copy, `touch` it.
- **Don't name a component `Convert`.** The class would hide `System.Convert` in its namespace,
  so the `/convert` page is `ConvertPage`.
- **Golden files come from an independent calculation.** The expected CSVs were computed with
  Python (`repr`, expanded to plain decimals), never by running the converter.

### Lessons from feature 004

- **A bare `WebApplicationFactory<Program>` writes to your real files.** It runs in Development
  with the content root at `src/JoakimSoftware.TcxTool.Web`, so `../../data/csv` and
  `../../tcxtool-state.json` are the real `tcxtool/` paths. HTTP tests use
  `Support/TcxToolWebFactory`, and `Architecture/TestIsolationTests` fails on any direct
  construction. bUnit page tests register services through `Support/BunitTcxToolServices`.
- **No test may read `.gitignore`.** `.dockerignore` keeps it out of the build context, so the test
  passes on the host and fails in the Compose gate. Check ignore rules with `git check-ignore`.
- **Force I/O failures with impossible paths, not `chmod`.** The test container runs as root. Use a
  path whose parent is a file, or a path that is a directory.
- **Static fields are PascalCase, and local constants are camelCase** (IDE1006). Only instance
  fields take `_camelCase`.
- **An `async` form handler needs the `Async` suffix.** Keep the handler non-async
  (`private Task OnSubmit() => DoAsync(RequestAborted)`), so the work method takes a token.

### Lessons from feature 005

- **Don't name a Web namespace `Activity`.** `Components/Pages/Error.razor` calls `Activity.Current`
  (`System.Diagnostics`). C# searches enclosing namespaces before `@using` directives, so
  `JoakimSoftware.TcxTool.Web.Activity` would capture the name and break the build. The folder is
  `Analysis/`. This is the same trap as `Convert`.
- **Two more ways to force I/O cases without `chmod`**, both verified on macOS and as root in
  `sdk:11.0`:
  - **Unreadable**: hold the file with `new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)`.
    .NET's advisory lock makes a second open throw `IOException`.
  - **Deleted after the check**: make it a dangling symbolic link. `File.Exists` is true, and the
    open throws `FileNotFoundException`.
- **Large tables use global CSS, not scoped CSS.** A `.razor.css` file adds a `b-xxxxxxxxxx`
  attribute to every element the component renders. For the Table page that is about 220,000
  elements, or about 2.6 MB of HTML.
- **Bootstrap table colors**: set `--bs-table-bg` and `--bs-table-color` on the cell, and leave out
  `table-striped` and `table-hover`. Their inset shadow shades over the color.
- **A static page still has `<!--Blazor-…` comments.** `Blazor-Server-Component-State` and
  `Blazor-Web-Initializers` are on every page. Only `<!--Blazor:{` marks an interactive component,
  so that is the string a "stays static" test checks.
- **Strip `/* */` comments before regex-parsing `app.css`**, or a comment above a rule becomes part
  of its selector.
- **A guard test passes before the code exists**, so show it failing with a temporary break. 005
  used three: appending to the CSV (no writes), `@rendermode InteractiveServer` (stays static), and
  `?? ""` on a class (no empty `class` attribute).

### Lessons from feature 006

- **Never use Python's `sum()` in a golden generator.** Since Python 3.12 it is compensated, so it
  can differ in the last digit from C#'s left-to-right `+=`. Use an explicit loop.
- **APFS is case-insensitive, so name rules must ignore case.** `a.states.csv` and `A.STATES.CSV`
  are the same file on macOS. `StatesCsvFileName.IsStatesCsvName` uses `OrdinalIgnoreCase`.
- **Force a write failure by making the target path a directory.** `File.Move` onto a directory
  throws, as root too. `TempDirectory.FileNames()` lists files only, so the blocking directory
  is not in it.
- **A CSV format change ripples into every test that builds rows.** That includes
  `ActivityCsvFixture.Line`, the hand-built `ActivityCsvRow`s in the table tests, the short marker
  rows in the loader tests, and every golden file. Regenerate goldens only with
  `generate_success_fixtures.py`.
- **"The mean of one value is itself" only holds for values already in shortest round-trip form.**
  `41.010498687664045` comes back as `41.01049868766404`. Use short test values such as `41.25`.
- **A log assertion must not check for digits across the temp path.** `TempDirectory` names hold a
  GUID, so remove the path before checking for `NotContain("5")`.

### Lessons from feature 007

- **Enhanced navigation doesn't run page scripts, so browser code goes in a custom element.**
  `App.razor` loads `js/timeline.js` once, as a module. It defines `<tcx-timeline>`, and the
  element's `connectedCallback` and `attributeChangedCallback` run after both a full load and an
  enhanced navigation.
- **An enhanced navigation to the same page strips what a script drew.** Blazor merges the server's
  markup into the live element: it keeps the element and its attributes, and removes children the
  server didn't render. No callback runs, because nothing was inserted and `data-timeline` didn't
  change. `<tcx-timeline>` has a `MutationObserver` that redraws when a drawn `svg` is removed.
  `#draw` calls `takeRecords()` so its own removals don't trigger it.
- **Browser behavior can be checked without a JS test runner.** Drive headless Chrome over the
  DevTools protocol with Node's built-in `WebSocket`: run `--remote-debugging-port`, evaluate code in
  the page, and send `PointerEvent`s. Run the app on a spare port with `TcxTool__CsvOutputDirectory`
  and `TcxTool__StateFilePath` pointing at the scratchpad, so the real files aren't touched.
- **A `<script>` inside a component is RZ9992, so pass data in an attribute.** The page renders the
  layout as JSON in `data-timeline`. Blazor HTML-encodes it, and the script reads it with
  `JSON.parse(element.dataset.timeline)`.
- **Put drawing geometry in C#, so tests cover it without a browser.** `TimelineLayout` computes
  every position as a fraction of the bar. The script only scales the fractions to pixels.
- **D3's `d3.min.mjs` can't be imported in a browser.** It is 796 bytes that re-export 30 bare
  `d3-*` package names. A test that checks the file is served as JavaScript still passes, so check
  what a vendored module imports. The app vendors the UMD `d3.min.js`, and `timeline.js` adds a
  `<script>` for it on demand, so every page still has one shared script reference (FR-022).
- **`<ImportMap />` lists every JS asset in `wwwroot`.** A "page doesn't load X" test must match
  `<script` or `<link` tags, not the bare path.
- **A FluentAssertions predicate is an expression tree, so it can't hold `out _` (CS8207).** Use
  `AllSatisfy` with a nested assertion.
- **Don't name a record property after a private helper.** `TimelineHeader.Date` clashed with a
  `Date(...)` method (CS0102).
- **A guard test can share its run with code it doesn't cover.** When several implementation files
  go in at once, remove each one temporarily to show its test failing, then restore it and compare
  it byte for byte.

### Lessons from feature 008

- **Test a page removal through the whole route set, not the removed address.**
  `Web/PageRoutesTests` reflects over every `RouteAttribute` in the Web assembly and expects an
  exact list. It fails while the page exists, and no test has to name it. Add a page's route there
  when you add the page, as you add its navbar link to `MainLayoutTests`.
- **A "no file mentions X" rule also covers comments that cite the feature folder.** The folder
  name carries the feature's words. Cite a feature's documents as "feature NNN contracts/…" in code.
- **`git rm` stages the deletion.** To delete files without changing the index, use `rm`, or
  follow `git rm` with `git restore --staged`.

## Feature history

| Feature | Status | Notes |
|---------|--------|-------|
| `specs/001-project-bootstrap` | Complete, converged | Empty solution, home page, architecture tests, Docker and Compose gate |
| `specs/002-tcx-to-csv` | Complete | `/convert` upload page, TCX parser, 17-field `\|` CSV per activity, typed errors |
| `specs/003-claude-ui-enhancements` | Complete, reverse-engineered | Bootstrap 5.3.0 PLog look, navbar, placeholder Table/Histograms/Timeline pages |
| `specs/004-about-and-csv-state` | Complete | `/about` page; current CSV in `tcxtool-state.json`, set by a conversion or the home page's Select CSV File card |
| `specs/005-table-page` | Complete | `/table` page: the current CSV's Activity card, a 10-column trackpoint table, and HR and cadence band colors |
| `specs/006-activity-states` | Complete | 19-field CSV (doubled RunCadence, State, StateSeconds), a `.states.csv` per activity, `statesCsvFilename` in the state file, and the `/states` page |
| `specs/007-timeline-page` | Complete | 16-field States CSV (StateTrackpointCount, ActivityDistanceMiles, ActivityElapsedSeconds; StateDistanceMiles), and the `/timeline` page: header card, D3 state bar with popup, three aligned charts |
| `specs/008-remove-histograms-page` | Complete | Removed the feature 003 Histograms placeholder page, its navbar link, and its tests. The address now gets the Not Found page |

The parser library has three namespaces:

- `Tcx/`: `TcxParser` (secure `XmlReader`, typed errors, never throws on bad input) and the
  immutable model records. `RecordedNumber` and `RecordedTime` keep the text as written.
- `Csv/`: `ActivityCsvWriter`, plus `ActivityIdName`, `CsvFileName`, `DerivedNumber`,
  `ElapsedTime`, and `CsvText` for the field rules.
  - The activity CSV has 19 fields, in `specs/006-activity-states/contracts/csv-format.md`, which
    replaces the field list of feature 002's contract. A pre-006 17-field file is a
    `HeaderMismatch`.
  - `ActivityState.For` maps the doubled cadence to `stopped`, `walking`, `running`, or `unknown`.
    `ActivityCsvWriter.Rows` builds the rows once, with StateSeconds from `TimeSpan`s.
  - `StatesCsvWriter.Rows` groups those rows into StateSets for the 16-field States CSV
    (`specs/007-timeline-page/contracts/states-csv-format.md`, which replaces 006's field list).
    A feature 006 13-field file is a `HeaderMismatch`. `StatesCsvFileName` names it
    `<name>.states.csv`.
  - `ActivityCsvReader` and `StatesCsvReader` read the two formats back through the internal
    `PipeDelimitedLines`. They return rows with every field as written, or a typed
    `ActivityCsvError` (`HeaderMismatch` or `FieldCount`, with a line number), and never throw for
    bad content.
- `Conversion/`: `TcxCsvConverter` (atomic temp-file-then-rename writes of both files per
  activity), `SavedCsv` (both names and counts), `ConversionResult`, `TcxError`, and
  `TcxErrorMessages`, the only place user-facing error text is written.

The Web project's `State/` namespace holds the application state (feature 004):

- `CurrentCsvStore` (singleton) is the only reader and writer of `tcxtool-state.json`. It never
  throws for bad input or an I/O failure.
- Later pages get the current CSV from `CurrentCsvStore.ReadAsync`, as a `CurrentCsv` whose status
  is `None`, `Present`, or `Missing`. They never read the file themselves.
- The state file also records `statesCsvFilename` (feature 006). `CurrentCsv.StatesFileName` is
  set only when it is exactly `StatesCsvFileName.For(csvFilename)`. Selecting a CSV records its
  States CSV when that file exists.
- `CsvFileNameRule` decides which names can be listed, selected, or read, and
  `CsvSelectionMessages` holds the user-facing text. It rejects States CSV names, ignoring case,
  so the home page never offers them.
- `Conversion/TcxToolPaths` resolves `CsvOutputDirectory` and `StateFilePath` against the content
  root.

Feature 005 added two Web namespaces:

- `Analysis/`: `CurrentActivityLoader` (singleton) is how analysis pages get the current CSV's rows.
  - It reads `CurrentCsvStore`, opens the file, and returns a `CurrentActivity`, whose status is
    `NoCurrentCsv`, `Missing`, `Unreadable`, `WrongFormat`, `NoTrackpoints`, or `Loaded`.
  - It writes nothing, and never throws for a bad or missing file.
  - Analysis pages get the current CSV's rows from it and never open the CSV themselves. Timeline reads only the States CSV, through `CurrentStatesLoader`.
  - `CurrentActivityMessages` holds the user-facing status text.
  - `CurrentStatesLoader` (feature 006, singleton) does the same for the States page, returning a
    `CurrentStates` (`NoCurrentCsv`, `NoStatesCsv`, `Missing`, `Unreadable`, `WrongFormat`,
    `NoStateSets`, or `Loaded`). Its text is in `CurrentStatesMessages`.
  - Both loaders open files through the internal `AppCsvFile`, which classifies `Missing` and
    `Unreadable`.
- `Table/`: `TrackpointBands` maps HeartRateBpm and RunCadence to the band classes in `app.css`.
  Since feature 006, RunCadence is already in steps per minute, so it is compared as written. The
  States page reuses the bands for its average heart rate and cadence columns.

Feature 007 added the Timeline page:

- `Timeline/` (Web): `TimelineLayout.From(rows)` turns the States CSV rows into `TimelineSegment`s
  and three `TimelineChart`s of `TimelinePoint`s, all as fractions of the bar. Invalid seconds count
  as 0, and all-zero seconds give equal widths. `TimelineHeader.From` holds the header card's
  values.
- `Components/States/StatesNotice` renders the `CurrentStates` notices for both the States and the
  Timeline page, with the id the page passes in (`states-status` or `timeline-status`).
- Browser code:
  - D3 7.9.2's UMD `d3.min.js` is vendored in `wwwroot/lib/d3/`, with `LICENSE` and a `README.md`
    that records the checksums.
  - `wwwroot/js/timeline.js` defines `<tcx-timeline>`. `App.razor` loads it on every page, and it
    loads D3 only when a timeline is on the page.
  - The four state colors are defined once, as `--tcx-state-*` in `app.css`, and the script only
    sets `state-*` classes. Tests guard against hex colors and URLs in the script.

## Repository layout

- The git repository root is the parent directory, `cj-speckit-explore/`.
  This project lives in its `tcxtool/` subdirectory. Run git commands with
  that in mind.
- `data/tcx/` holds real Garmin activity exports (about 13 to 15 MB each).
  They are sample input for manual runs, not test fixtures:
  - Do not read them whole into context. Use `head`, `grep`, or a
    targeted parse.
  - Test fixtures are small, purpose-built `.tcx` files kept with the tests
    (Principle V), not copies of these files.
  - They contain real GPS positions. Do not publish them or send their
    contents to external services.
- `tcxtool-state.json` is the application state: the current CSV's file name. It is git-ignored
  and machine-local. In the container it lives at `/tmp/tcxtool/tcxtool-state.json` and is lost
  when the container is removed. Delete it after manual runs if you like; the app treats a missing
  file as "no current CSV".
- `data/csv/` is the output location for CSV conversions. It is git-ignored, because the CSVs
  contain the same real GPS positions. In Compose it is bind-mounted at `/data/csv`. Delete
  generated CSVs after manual runs.

## Environment

- macOS with zsh. Do not produce PowerShell or Windows-specific instructions.
- .NET SDK 11.0.100, pinned by `global.json`.
- Docker Desktop and Compose v2 are installed. The daemon is not always running: check that
  `docker info` succeeds before the Compose gate. The first gate run needs network access to
  pull the `sdk:11.0` and `aspnet:11.0` images. After that, the tests run with no network.

## Build and test commands

Run every command from `tcxtool/`, not from the git root.

### Solution layout

```text
JoakimSoftware.TcxTool.slnx         # the only solution; global.json pins SDK 11.0.100 (latestFeature)
src/JoakimSoftware.TcxTool.Parser/  # BCL-only parser library (no ASP.NET Core / Blazor references)
src/JoakimSoftware.TcxTool.Web/     # Blazor Web App, static SSR by default, references Parser
tests/JoakimSoftware.TcxTool.Tests/ # xUnit + bUnit + FluentAssertions 7.x, references both
  Architecture/                     # ProjectSettingsTests, ParserIndependenceTests
  Parser/                           # parser, CSV writer, converter, performance tests
  Web/                              # bUnit component tests and WebApplicationFactory endpoint tests
  Support/                          # RepositoryRoot, SecureXml, Fixture, TempDirectory, ListLogger
  Fixtures/                         # small .tcx fixtures + Expected/ golden CSVs, mounted read-only
Dockerfile                          # stages: build (sdk:11.0), runtime (aspnet:11.0, non-root, :8080)
compose.yaml                        # service app
compose.test.yaml                   # service tests (overlay; network_mode: none)
```

### Quality gates (all four must pass; constitution Quality Gates)

```zsh
dotnet build JoakimSoftware.TcxTool.slnx                         # 1. warnings are errors
dotnet format JoakimSoftware.TcxTool.slnx --verify-no-changes    # 2. style
dotnet test JoakimSoftware.TcxTool.slnx                          # 3. tests
docker compose -f compose.yaml -f compose.test.yaml up \
  --build --abort-on-container-exit --exit-code-from tests       # 4. Compose gate; check `echo $?` is 0
```

The gate also starts the `app` container on host port 8080. If that port is busy, prefix the gate
command with `TCXTOOL_HOST_PORT=8081`.

### Run

```zsh
dotnet run --project src/JoakimSoftware.TcxTool.Web      # http://localhost:5169
docker compose up --build app                            # http://localhost:8080
TCXTOOL_HOST_PORT=8081 docker compose up --build app     # different host port
docker compose down
```

### Toolchain quirks

- Every `.csproj` must declare `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`, and
  `AnalysisLevel` itself (not via `Directory.Build.props`). `ProjectSettingsTests` checks this.
- `EnforceCodeStyleInBuild=true` makes `.editorconfig` rules at `warning` severity break the build.
  Naming rules (IDE1006: `_camelCase` private fields, `Async` suffix) are only enforced because
  `.editorconfig` sets `dotnet_diagnostic.IDE1006.severity = warning`. Keep that line.
- The Parser project has `GenerateDocumentationFile=true`, so every public parser API needs an XML
  doc comment or the build fails.
- Never relax a setting or suppress a rule to get a green build. Fix the code.
- If the `app` container exits during the Compose gate, `--abort-on-container-exit` stops the
  tests and the gate fails. A runtime image that can't start counts as a failed gate.

## Code conventions established so far

These are additions to the constitution's idioms, not replacements.

- **Render modes**: the Web project was created with `--interactivity Server`. Interactive Server
  is registered, but no component uses it. Add `@rendermode InteractiveServer` only on a
  component that needs it (Principle I), never globally.
- **UI styling** is Bootstrap 5.3.0, vendored in `wwwroot/lib/bootstrap/` (copied from the PLog
  app, whose look the UI matches) and linked through `@Assets[...]` in `App.razor`. No CDN, so the
  app and tests need no network. `MainLayout` is the PLog shell: a `navbar-dark bg-dark` navbar,
  `main.container.my-4`, and a light footer. Page titles are a semantic `h1` styled
  `class="h3 mb-3"`. Use plain Bootstrap classes (cards, `form-control`, `btn btn-primary`,
  `alert alert-*`) before writing custom CSS. Add a navbar link only when its page exists.
- **Tests** all live in the one test project. Add folders, not projects. A fourth project
  needs a recorded reason under Governance.
  - `Architecture/`: structural rules (project settings, parser independence)
  - `Parser/`: parser, CSV, and converter tests against the fixtures and golden files
  - `Web/`: bUnit component tests (derive from `BunitContext`, use `Render<T>()`) and
    `WebApplicationFactory<Program>` tests of the real HTTP pipeline
  - `Support/`: shared helpers, including `TcxToolWebFactory` (the only way to build an HTTP test
    host) and `BunitTcxToolServices`
  - `Fixtures/`: `.tcx` fixtures, mounted read-only into the test container
- **Reading files in tests**: read any XML through `Support/SecureXml.Load`, even in tests
  (Principle IV). Find repo files with `Support/RepositoryRoot.Combine("relative/path")`,
  which works on the host and in the container at `/src`.
- **Test packages**: `bunit` 2.15.2, xUnit 2.9.3 (template default), and `FluentAssertions`
  **7.2.2**.
  - Version 7.x is Apache-2.0; 8.x needs a commercial licence for commercial use. Don't
    upgrade without the maintainer's decision.
  - `Microsoft.AspNetCore.Mvc.Testing` 11.0.0 for `WebApplicationFactory<Program>`. It needs no
    `public partial class Program`.
  - Hand-written fakes live in `Support/`, for example `ListLogger<T>`, which records log entries.
  - Add NSubstitute only when a boundary needs a fake that a small hand-written class can't cover.
