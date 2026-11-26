# Implementation Plan: About Page and Current CSV State

**Branch**: `004-about-and-csv-state` (feature id; work is currently on git branch `main`) | **Date**: 2026-11-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/004-about-and-csv-state/spec.md`

## Summary

The feature gives the app a **current CSV** that later analysis pages can rely on, and adds an
About page.

- **State**: a singleton `CurrentCsvStore` in a new `Web/State/` folder reads and writes
  `tcxtool-state.json`, which is `{ "csvFilename": "<name>" }`.
  - The path comes from a new `TcxTool:StateFilePath` option: `tcxtool/` on the host, and
    `/tmp/tcxtool/` in the container.
  - Writes are atomic, using the temp-then-rename pattern from `TcxCsvConverter`.
  - Reads never fail a page. Anything invalid means "no current CSV" and logs one warning.
  - One name rule decides which names the app can list, select, or read.
- **Convert**: after a successful conversion, `ConvertPage` asks the store to record the first saved
  CSV, then shows a `CurrentCsvNotice` under the unchanged result view.
- **Home**: a second card, "Select CSV File", is a static SSR form with a `<select>` that lists the
  CSV directory newest first. It shows the current CSV and posts the choice to the store.
- **About**: a static `/about` page with the FR-003 content written as markup, linked last in the
  navbar.
- **Test isolation**: a probe showed that a bare `WebApplicationFactory<Program>` runs in
  Development, against the real `tcxtool/` paths. So every endpoint test moves to a
  `TcxToolWebFactory` that points both paths at a temp directory, and a guard test fails if any test
  builds a factory directly.

Decisions and measurements are in [research.md](./research.md).

## Technical Context

**Language/Version**: C# (ships with SDK 11.0.100), `net11.0`, pinned by `global.json`.

**Primary Dependencies**: the ASP.NET Core / Blazor shared framework, plus `System.Text.Json` from
the BCL. No new runtime or test packages.

**Storage**: files only.
- CSVs: unchanged (`TcxTool:CsvOutputDirectory`).
- State: `TcxTool:StateFilePath`.
  - Host: `tcxtool/tcxtool-state.json`.
  - Container: `/tmp/tcxtool/tcxtool-state.json`. `/app` is not writable by `uid 1654`, which this
    plan verified.
  - Tests: a temp directory.

**Testing**: xUnit 2.9.3, bUnit 2.15.2, FluentAssertions 7.2.2, `Microsoft.AspNetCore.Mvc.Testing`
11.0.0, and coverlet. All of these are unchanged. Hand-written support only (`TempDirectory`,
`ListLogger<T>`, and the new `TcxToolWebFactory`). NSubstitute is still not needed.

**Target Platform**: the macOS host (`dotnet run`), and the Linux container from `aspnet:11.0`,
running non-root on port 8080.

**Project Type**: Web application (Blazor Web App), with a parser class library and a test project.

**Performance Goals**: the home page lists a directory of hundreds of CSVs and reads a state file of
64 KiB at most. Both are negligible next to rendering. No new performance test.

**Constraints**:
- Static SSR only (Principle I).
- The state file is never half written (FR-007).
- Bad state never yields an error page (FR-008).
- No path escape from the CSV directory (FR-010).
- Tests never touch the real state file (FR-020).
- Logs never include the state file's content.
- Tests run offline.

**Scale/Scope**: one local user. Adds 1 page, 1 component, 1 service, 7 small `State/` types,
1 option, 1 path helper, and 1 test-support factory. Changes 3 components, `Program.cs`, both
appsettings files, `compose.yaml`, `.gitignore`, `.dockerignore`, and `CLAUDE.md`. 8 new and 6 changed test classes, plus 1 test-support
factory.

No item is marked NEEDS CLARIFICATION. Research R1–R12 settle every design question the spec leaves
open.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Section | Requirement | Plan | Pre | Post |
|---|---|---|---|---|
| I. .NET 11 Blazor Web App | Static SSR default, interactivity only where needed | The About page, the home page form, and the convert notice are all static SSR; no `@rendermode` (R6, R10) | ✅ | ✅ |
| II. Style by toolchain | Settings in each project; records; one type per file; `Async` + `CancellationToken`; XML docs on public parser APIs; code-behind when a component is more than markup | New types are one per file; `CurrentCsv`, `CsvSelectionResult`, `TcxToolState` are `record`s; `ReadAsync`, `SelectAsync`, `RecordConversionAsync` take a `CancellationToken`; `Home.razor.cs` code-behind added; no parser API change | ✅ | ✅ |
| III. Parser library, UI on top | Parser has no ASP.NET Core reference; typed results cross the boundary; components don't open files | State lives in the Web project (R1); pages bind `CurrentCsv` / `CsvSelectionResult` and never touch the file system; parser unchanged, `ParserIndependenceTests` stays green | ✅ | ✅ |
| IV. BCL XML only (NON-NEGOTIABLE) | Secure `XmlReader` for all XML | No XML is read or written by this feature. The state file is JSON via BCL `System.Text.Json`. Existing XML paths untouched. | ✅ | ✅ |
| V. Test-first (NON-NEGOTIABLE) | Tests first and failing | Tasks will put every test before its subject. The isolation guard fails today against the two existing bare factories, so it fails on its own before the factory change (R7). Config tests (`AppSettingsTests` for `StateFilePath`) precede the settings. | ✅ | ✅ |
| VI. Docker multi-stage, non-root | Non-root, no samples in the image | Dockerfile unchanged; container state goes to `/tmp` (writable by the non-root user, verified); `tcxtool-state.json` added to `.dockerignore` (R11) | ✅ | ✅ |
| VII. Compose test gate (NON-NEGOTIABLE) | Tests in the build stage, no network | `compose.test.yaml` unchanged; no new packages, so the offline restore holds; `compose.yaml` gains one environment variable on `app` | ✅ | ✅ |
| Technology Constraints | 3 projects; `ILogger<T>`; never log raw input | No new project; `CurrentCsvStore` logs via `ILogger<CurrentCsvStore>` with the path and a fixed reason only (R12) | ✅ | ✅ |
| Quality Gates 1–4 | All pass | Validated by [quickstart.md](./quickstart.md) scenarios 1 and 5 | ✅ | ✅ |

Result: **PASS**, with no violations, so Complexity Tracking stays empty.

Points to note, none a violation:

- **Existing tests change.**
  - `MainLayoutTests.NavbarLinksOnlyToPagesThatExist` gains "About" (FR-019 allows this).
  - `HomePageTests` must register `CurrentCsvStore`, because `Home` now injects it.
  - `ConvertEndpointTests` and `PlaceholderPageEndpointTests` move to `TcxToolWebFactory`.
  - Their assertions are otherwise unchanged.
- **`CsvConversionService` changes only internally.** It resolves its directory through the new
  `TcxToolPaths`, and its public signature is the same.

## Project Structure

### Documentation (this feature)

```text
specs/004-about-and-csv-state/
├── plan.md                  # This file
├── research.md              # Phase 0: decisions R1–R12 and machine-verified facts
├── data-model.md            # Phase 1: state file, State/ types, lifecycle, components
├── quickstart.md            # Phase 1: validation scenarios 1–6
├── contracts/
│   ├── state-file.md        # tcxtool-state.json: location, exact bytes, read rules, name rule
│   └── pages.md             # navbar, /about, home Select CSV File card, convert notice, messages
├── checklists/
│   └── requirements.md      # Spec quality checklist (from /speckit-specify)
└── tasks.md                 # Phase 2 output (/speckit-tasks, not created here)
```

### Source Code (`tcxtool/`)

```text
src/JoakimSoftware.TcxTool.Web/
├── Program.cs                          # + AddSingleton<CurrentCsvStore>()
├── appsettings.json                    # TcxTool: + StateFilePath "tcxtool-state.json"
├── appsettings.Development.json        # TcxTool: + StateFilePath "../../tcxtool-state.json"
├── Conversion/
│   ├── TcxToolOptions.cs               # + StateFilePath
│   ├── TcxToolPaths.cs                 # new: CsvDirectory(), StateFile()
│   └── CsvConversionService.cs         # OutputDirectory() → TcxToolPaths.CsvDirectory()
├── State/                              # new
│   ├── CsvFileNameRule.cs
│   ├── CurrentCsvStatus.cs
│   ├── CurrentCsv.cs
│   ├── CsvSelectionOutcome.cs
│   ├── CsvSelectionResult.cs
│   ├── CsvSelectionMessages.cs
│   ├── TcxToolState.cs
│   └── CurrentCsvStore.cs
└── Components/
    ├── _Imports.razor                  # + Web.State, Components.State usings
    ├── Layout/MainLayout.razor         # + About NavLink, last
    ├── Pages/AboutPage.razor           # new, /about
    ├── Pages/Home.razor (+ Home.razor.cs, new)   # + Select CSV File card and form
    ├── Pages/ConvertPage.razor (+ .razor.cs)     # + CurrentCsvNotice after the result
    └── State/CurrentCsvNotice.razor    # new

tests/JoakimSoftware.TcxTool.Tests/
├── Architecture/TestIsolationTests.cs  # new guard (R7)
├── Support/TcxToolWebFactory.cs        # new
├── Support/BunitTcxToolServices.cs     # new: bUnit services with temp-directory paths (FR-020; added in T020, recorded by T038)
└── Web/
    ├── State/CsvFileNameRuleTests.cs   # new
    ├── State/CurrentCsvStoreTests.cs   # new
    ├── TcxToolPathsTests.cs            # new
    ├── CurrentCsvNoticeTests.cs        # new (bUnit)
    ├── AboutPageTests.cs               # new (bUnit)
    ├── AboutPageEndpointTests.cs       # new (HTTP)
    ├── HomeSelectEndpointTests.cs      # new (HTTP)
    ├── HomePageTests.cs                # + store registration, card tests
    ├── MainLayoutTests.cs              # navbar list + About
    ├── ConvertPageTests.cs             # + store registration
    ├── ConvertEndpointTests.cs         # → TcxToolWebFactory; + state recorded / unchanged
    ├── PlaceholderPageEndpointTests.cs # → TcxToolWebFactory
    └── AppSettingsTests.cs             # + StateFilePath defaults

compose.yaml        # app: + TcxTool__StateFilePath=/tmp/tcxtool/tcxtool-state.json
.gitignore          # + tcxtool-state.json
.dockerignore       # + tcxtool-state.json, **/tcxtool-state.json
CLAUDE.md           # feature history row; State/ namespace; test-factory rule; About page
```

**Structure Decision**: the existing three-project layout, with no change in project count:

- `src/JoakimSoftware.TcxTool.Web` gains a `State/` folder for application state, plus the new
  components.
- `src/JoakimSoftware.TcxTool.Parser` is untouched.
- `tests/JoakimSoftware.TcxTool.Tests` gains a `Web/State/` folder, a guard test in
  `Architecture/`, and a factory in `Support/`. Following `CLAUDE.md`, these are folders, not
  projects.

## Complexity Tracking

No constitution violations to justify.
