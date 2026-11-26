# Research: About Page and Current CSV State

**Feature**: `004-about-and-csv-state` | **Date**: 2026-11-24 | **Spec**: [spec.md](./spec.md)

The spec has no open clarification markers. FR-015 was settled in its Clarifications session: the
CSV is chosen from a server-side list. This file records the design decisions the spec leaves to
the plan, and the facts checked on this machine.

## Facts checked on this machine

| Fact | How checked | Result |
|------|-------------|--------|
| Environment and content root of `WebApplicationFactory<Program>` | A throwaway xUnit project in the scratchpad referencing the Web project and `Microsoft.AspNetCore.Mvc.Testing` 11.0.0 | `Development`, content root `tcxtool/src/JoakimSoftware.TcxTool.Web`, `CsvOutputDirectory` = `../../data/csv`. A factory that does not override the state path would write the developer's real `tcxtool/tcxtool-state.json` (see R7). |
| Write access in the runtime image | `docker run --entrypoint sh joakimsoftware/tcxtool:dev` | Runs as `uid=1654(app)`. `/app` is `root:root 755`: not writable. `/tmp` is `1777`: a non-root user can create a directory, write a file, and rename it over another file. |
| Atomic replace in the codebase | `TcxCsvConverter` | Already writes `<name>.<guid>.tmp` and then calls `File.Move(temp, final, overwrite: true)`. That is `rename(2)` on macOS and Linux, which is atomic within one directory. |
| JSON support | BCL | `System.Text.Json` ships in the shared framework. No package is needed. |

## R1. Where the state logic lives

- **Decision**: in the Web project, in a new `State/` folder (namespace
  `JoakimSoftware.TcxTool.Web.State`), as a singleton service `CurrentCsvStore`. Pages inject it
  and bind its typed results. They never touch the file system.
- **Rationale**:
  - The current CSV is application state, not TCX parsing. Principle III keeps the parser library
    about TCX.
  - The store depends only on `IOptions<TcxToolOptions>`, `IHostEnvironment`, and `ILogger<T>`, as
    `CsvConversionService` does. So it can be tested directly against a `TempDirectory`, with no
    web host.
  - FR-009 asks for one shared reader. A single service also gives the later Table, Histograms,
    and Timeline pages one place to ask.
- **Alternatives considered**:
  - In the parser library: rejected, because the library would then hold web app state.
  - Each page reads the JSON itself: rejected by FR-009 and Principle III.

## R2. State file location and configuration

- **Decision**: a new option, `TcxTool:StateFilePath`. A relative path resolves against the content
  root, following the `CsvOutputDirectory` rule.

  | Where | Value | Resolves to |
  |-------|-------|-------------|
  | `appsettings.json` | `tcxtool-state.json` | `<content root>/tcxtool-state.json` |
  | `appsettings.Development.json` | `../../tcxtool-state.json` | `tcxtool/tcxtool-state.json` (FR-005) |
  | `compose.yaml` (`app`) | `TcxTool__StateFilePath=/tmp/tcxtool/tcxtool-state.json` | inside the container only |
  | Tests | a `TempDirectory` per test class (R7) | never the real file |

- **Rationale**:
  - `dotnet run --project src/JoakimSoftware.TcxTool.Web` runs in Development, with the content root
    at the Web project. So `../../` reaches `tcxtool/`, as it already does for `data/csv`.
  - In the container, the content root `/app` is not writable by the non-root user, but `/tmp` is.
    The spec accepts that container state is lost when the container is removed.
  - The parent directory is created on first write, so `/tmp/tcxtool/` needs no Dockerfile change.
- **Alternatives considered**:
  - A bind mount of `./tcxtool-state.json`: rejected. When the host file does not exist yet, Docker
    creates a directory at that path, and the app's writes then fail.
  - A named volume at `/data/state`: rejected for now. The volume is created `root`-owned unless
    the image pre-creates the directory with `chown`. That is a Dockerfile change for persistence
    the spec does not need.
  - Putting the file in `/data/csv`: rejected. It mixes state with outputs, and the state file on
    the host would differ from the one at `tcxtool/` (FR-005).

## R3. File format, reading, and writing

- **Decision**:
  - **Write**: serialize `{ "csvFilename": "<name>" }` with `System.Text.Json`, indented, UTF-8 with
    no BOM, and a trailing `\n`. Write to `<path>.<guid>.tmp` in the same directory, then
    `File.Move(temp, path, overwrite: true)`. Create the parent directory first. On failure, delete
    the temp file.
  - **Read**: read at most 64 KiB, parse with `JsonDocument`, and require a root object whose
    `csvFilename` property is a string that passes the name rule (R4). Other properties are
    ignored on read. Every failure becomes `CurrentCsvStatus.None` and one warning log entry.
  - **Serialize writes**: one `SemaphoreSlim(1, 1)` in the singleton store, so two submits from
    the same process run one after another. The unique temp names mean even unserialized writers
    never share a temp file. The last rename wins (spec edge case "Two browser tabs").
- **Rationale**:
  - The atomic rename is the pattern already proven in `TcxCsvConverter` (FR-007).
  - A size cap stops a corrupted or hand-edited huge file from being read into memory.
  - Tolerating extra keys on read costs nothing. FR-006 governs what the app writes.
- **Alternatives considered**:
  - `JsonSerializer.Deserialize<T>` on read: rejected. It maps a wrong type or missing key to
    `null` or an exception in ways that are harder to report as one clear reason.
  - A `FileStream` with `FileShare.None` as a lock: rejected. It adds cross-process locking the
    spec does not need, and it fails on some bind mounts.

## R4. The CSV name rule

- **Decision**: one static helper, `CsvFileNameRule.IsPlainCsvName(string? name)`, used by both
  the reader and the writer. It accepts a name only when:
  - it is not null, empty, or whitespace
  - `Path.GetFileName(name) == name`, and it contains neither `/` nor `\`
  - it is not `.` or `..`, and it is not rooted
  - it ends with `.csv`, ordinal and case-sensitive, matching what the converter writes
  - it contains no characters from `Path.GetInvalidFileNameChars()`

  Selection also requires `File.Exists(Path.Combine(csvDirectory, name))`, and the full path must
  stay inside `csvDirectory` once resolved.
- **Rationale**: FR-010 and the spec's path-escape edge case. A hand-edited form or state file can
  carry `../secrets.csv` or `/etc/passwd.csv`. Checking that the resolved full path stays inside the
  directory backs up the name checks.
- **Alternatives considered**: accepting `.CSV` as well. Rejected, because the app only writes
  lowercase `.csv`, and the list (R5) uses the same rule. That keeps "listed" and "selectable"
  identical.

## R5. Listing CSVs for the home page

- **Decision**: `CurrentCsvStore.ListCsvFiles()` enumerates the top level of the CSV output
  directory only, keeps names that pass R4, and sorts them **descending, ordinal**. A missing
  directory returns an empty list.
- **Rationale**: names start with `yyyy-MM-dd` (contract 002 `csv-format.md`), so a descending
  ordinal sort puts the newest activity date first. Within one date, the order is still
  deterministic, which satisfies FR-015's "then by name". The directory holds tens to hundreds of
  files, so enumeration per request is trivial.
- **Alternatives considered**:
  - Sort by file modification time: rejected. It is not deterministic in tests, and a re-convert
    of an old activity would jump to the top.
  - Read each CSV's header to prove it came from TcxTool: rejected. The spec's list is "the `.csv`
    files in the CSV output directory", the app is their only writer, and reading every file on
    each home page load costs I/O for no requirement.

## R6. The home page selection form

- **Decision**:
  - `Home.razor` gains a code-behind, `Home.razor.cs`. It reads the current CSV and the list in
    `OnInitializedAsync`.
  - A static SSR form: `method="post"`, `@formname="csv-select"`, `<AntiforgeryToken />`, and one
    `<select class="form-select" name="CsvFilename">` bound through
    `[SupplyParameterFromForm(FormName = "csv-select")] string? CsvFilename`.
  - The submit handler calls `CurrentCsvStore.SelectAsync(CsvFilename, token)`, renders the typed
    `CsvSelectionResult`, and re-reads the current CSV, so the card shows the new value in the same
    response.
  - No redirect after the POST, matching `/convert`.
  - With no CSVs listed, the card shows the empty-state text and a link to `/convert` instead of
    the form.
  - The current CSV is preselected (`selected` on its `<option>`) when it is in the list. Otherwise
    the first option is a disabled, selected "Choose a CSV file" placeholder with an empty value,
    so a submit with nothing chosen posts an empty value (spec Story 2, scenario 5).
- **Rationale**:
  - Static SSR per Principle I. A `select` plus submit is 2 actions (SC-003).
  - The pattern copies the `/convert` form, so the antiforgery behavior from the 002 lessons
    applies: a cross-site POST gets 400, and no hidden token is rendered.
- **Alternatives considered**:
  - `InteractiveServer` with live selection: rejected, because nothing needs a circuit.
  - A separate `/select` page: rejected. The spec puts the choice on the home page.

## R7. Keeping tests away from the real state file (FR-020, SC-007)

- **Decision**:
  - A new helper, `Support/TcxToolWebFactory`, creates a `WebApplicationFactory<Program>` whose
    configuration always sets both `TcxTool:CsvOutputDirectory` and `TcxTool:StateFilePath` to paths
    inside a `TempDirectory` it owns and disposes. Callers can add more settings, such as a small
    `MaxUploadBytes`.
  - `ConvertEndpointTests` and `PlaceholderPageEndpointTests` move to it.
  - A new guard test, `Architecture/TestIsolationTests`, scans the test sources and fails if any file
    outside `Support/` constructs `WebApplicationFactory<Program>` directly (`new
    WebApplicationFactory<Program>` or a target-typed `= new()` on that type). It fails today,
    because both existing endpoint test classes do, so it is genuinely test-first (CLAUDE.md, lesson
    from 001).
  - bUnit and service tests pass explicit `TcxToolOptions` with paths in a `TempDirectory`.
- **Rationale**: the probe above showed that a bare factory runs in Development with the real
  `tcxtool/` paths. Today that already risks writing CSVs to the real `data/csv` if an override is
  forgotten. With the state file, every conversion test would also overwrite the developer's
  current CSV. One factory plus a guard makes the safe path the only path.
- **Alternatives considered**:
  - A `[ModuleInitializer]` that sets `TcxTool__StateFilePath` for the test process: rejected.
    CA2255 flags module initializers in libraries, the test project is a library, and suppressing a
    rule is not allowed (`CLAUDE.md`).
  - Hashing the real file before and after the run: rejected. xUnit 2 has no assembly-level hook
    to run it reliably, and in the container the file does not exist anyway.

## R8. Recording a conversion (FR-011 to FR-013)

- **Decision**:
  - `CurrentCsvStore.RecordConversionAsync(ConversionResult result, CancellationToken token)`
    returns `CsvSelectionResult?`: `null` when the conversion failed or saved no file (state
    untouched), otherwise the result of writing `result.Files[0].FileName`.
  - `ConvertPage` calls it right after `CsvConversionService.ConvertAsync` and passes both results
    to the view.
  - `CsvConversionService` is unchanged, so its tests stand.
- **Rationale**: the first-file rule and the "failure changes nothing" rule live in one tested C#
  method, not in markup (Principle II: "Razor files bind and compose").
- **Alternatives considered**: putting the call inside `CsvConversionService.ConvertAsync`.
  Rejected, because it would couple conversion to state, change a tested signature, and give the
  later analysis features two writers to reason about.

## R9. Showing the current CSV after a conversion or selection

- **Decision**: a new component, `Components/State/CurrentCsvNotice.razor`, takes a
  `CsvSelectionResult?` parameter and renders:
  - `Recorded`: `p[role=status]` with "Current CSV: **name**"
  - any other outcome: `div.alert.alert-warning[role=alert]` with the message from
    `CsvSelectionMessages.For(outcome)`

  `ConversionResultView` keeps its markup unchanged. `ConvertPage` renders `CurrentCsvNotice` after
  it. The home page renders the same component for a selection result.
- **Rationale**:
  - One component and one message table, mirroring `TcxErrorMessages` (the 002 rule that
    user-facing text has one home).
  - The 002 and 003 result contracts stay untouched (FR-019).

## R10. The About page

- **Decision**: `Components/Pages/AboutPage.razor` at `/about`. It is static SSR markup with no
  code-behind and no services. The FR-003 content is written as Razor and HTML: `h2` for each `##`
  heading, `<strong>` for bold, `ul`/`li` for lists, and `<a href>` for links. The `#` heading is the
  page's single `h1.h3.mb-3`. The navbar gains `<NavLink class="nav-link" href="about">About</NavLink>`
  as its last item.
- **Rationale**:
  - FR-004 asks for one place in the source. The page file is that place.
  - Rendering markdown at runtime would need a third-party package or a hand-written renderer, for
    one static page.
  - The component is named `AboutPage`, matching `TablePage` and the rest. The 002 lesson about not
    naming a component `Convert` applies in spirit: page components end in `Page`.
- **Alternatives considered**: an embedded `.md` file rendered by Markdig. Rejected, because it adds
  a NuGet dependency and a sanitization question for no user-visible gain.

## R11. Source control and image hygiene (FR-021)

- **Decision**:
  - Add `tcxtool-state.json` to `tcxtool/.gitignore`.
  - Add `tcxtool-state.json` and `**/tcxtool-state.json` to `.dockerignore`. The build stage runs
    `COPY . .`, so without this rule the developer's file would land in `/src` in the test
    container, where the Development content root resolves the default path.
- **Note** (out of scope, already reported): `tcxtool/.gitignore` has `# data/csv/` commented out,
  and commit `2bd6c7d` tracks a CSV with real GPS positions. This feature does not change that
  line. The maintainer decides.

## R12. Logging

- **Decision**: `[LoggerMessage]` source-generated methods on `CurrentCsvStore`:
  - `Warning`: "State file {Path} ignored: {Reason}", where the reason is a fixed phrase such as
    "not valid JSON", "no csvFilename string", "csvFilename is not a plain CSV name", or "larger
    than 64 KiB"
  - `Warning`: "State file {Path} could not be written: {ExceptionType}"
  - `Information`: "Current CSV set to {FileName}"

  The file's content and the exception message are never logged.
- **Rationale**: FR-008, and the constitution's rule against logging raw input. `ListLogger<T>`
  already exists for asserting on log entries.
