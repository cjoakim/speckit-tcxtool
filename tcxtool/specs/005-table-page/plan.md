# Implementation Plan: Table Page for the Current CSV

**Branch**: `005-table-page` (feature id; work is currently on git branch `main`) | **Date**: 2026-11-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/005-table-page/spec.md`

## Summary

The Table page shows the current CSV: a header box naming the activity, and every trackpoint in a
table, with HeartRateBpm and RunCadence cells colored by band.

- **Reading**: a new `ActivityCsvReader` in the parser library, next to the writer, reads the CSV
  contract back. It returns typed rows, or a typed error for a wrong header or a wrong field count.
  It checks the header against `ActivityCsvWriter.Header`, so the format is defined in one place.
- **Loading**: a singleton `CurrentActivityLoader`, in a new Web folder `Analysis/`, asks
  `CurrentCsvStore` for the current CSV, opens it, and reads it. It returns a `CurrentActivity` with
  one of six statuses: `NoCurrentCsv`, `Missing`, `Unreadable`, `WrongFormat`, `NoTrackpoints`, or
  `Loaded`. It never writes, and never fails the page. Histograms and Timeline will reuse it.
- **Page**: `TablePage` gains a code-behind. It loads once, then shows a notice for each state
  without data, or an `ActivitySummaryCard` and a `TrackpointTable`.
  - The table is a single loop with no component per row.
  - It sits in a scroll box whose header row is sticky.
  - It has no striping or hover, so the band colors are exact.
- **Colors**: a pure `TrackpointBands` class maps a value to one of ten CSS classes. RunCadence is
  doubled first, as the clarification decided. The classes live in the global `app.css` and set
  Bootstrap's `--bs-table-bg` and `--bs-table-color`. Every pair has a contrast of at least 6.4:1.
- **Measured**: a throwaway Blazor app rendered 20,000 rows in under 0.3 s on the server, as 4.6 MB
  of HTML. So static SSR with every row on one page fits SC-002, with no paging or interactivity.

Decisions and measurements are in [research.md](./research.md).

## Technical Context

**Language/Version**: C# (ships with SDK 11.0.100), `net11.0`, pinned by `global.json`.

**Primary Dependencies**: the ASP.NET Core / Blazor shared framework, plus the vendored Bootstrap
5.3.0. No new runtime or test packages.

**Storage**: files only, read-only for this feature.
- The current CSV is in `TcxTool:CsvOutputDirectory`.
- Its name comes from `tcxtool-state.json`, through `CurrentCsvStore`.
- Nothing is written (FR-007).

**Testing**: xUnit 2.9.3, bUnit 2.15.2, FluentAssertions 7.2.2, `Microsoft.AspNetCore.Mvc.Testing`
11.0.0, and coverlet. All of these are unchanged.
- HTTP tests use `TcxToolWebFactory`.
- bUnit tests use `BunitTcxToolServices`, which gains `CurrentActivityLoader`.
- CSV inputs come from the existing golden CSVs and a new `Support/ActivityCsvFixture` writer.
- NSubstitute is still not needed.

**Target Platform**: the macOS host (`dotnet run`), and the Linux container from `aspnet:11.0`,
running non-root on port 8080.

**Project Type**: Web application (Blazor Web App), with a parser class library and a test project.

**Performance Goals**: SC-002. The real marathon CSV (19,908 rows, 4.3 MB) must be fully shown in
under 5 s.
- Server render, measured on a probe: 0.18–0.26 s for 20,000 rows, and 0.9 s for 70,000 rows.
- A test holds the server side to under 5 s for 20,000 rows.
- The browser side is checked by hand (quickstart scenario 4).

**Constraints**:
- Static SSR only (Principle I, FR-020).
- Every row on one page (FR-012).
- Values exactly as written (FR-013).
- No partial table on a bad file (FR-005).
- Error text and logs never include file content (FR-006, research R11).
- No writes (FR-007).
- Tests never touch the real `tcxtool/` files (FR-022).
- Tests run offline.

**Scale/Scope**: one local user and one activity per page view, up to about 70,000 rows.
- **New**: 5 parser types, 5 `Analysis/` types, 1 `Table/` type, 2 components, and a code-behind.
- **Changed**: `TablePage.razor`, `Program.cs`, `app.css`, `_Imports.razor`, and `CLAUDE.md`.
- **Tests**: 10 new test classes and 1 new support helper. 2 test files change.

No item is marked NEEDS CLARIFICATION. Research R1–R11 settle every design question the spec leaves
open.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Section | Requirement | Plan | Pre | Post |
|---|---|---|---|---|
| I. .NET 11 Blazor Web App | Static SSR default, interactivity only where needed; Bootstrap for UI | `TablePage` and its two components are static SSR, with no `@rendermode`. `Virtualize` was rejected because it needs interactivity (R4). The look uses Bootstrap cards, tables, alerts, and palette colors (R5, R7). D3.js is not needed: this page has no chart. | ✅ | ✅ |
| II. Style by toolchain | Settings in each project; one type per file; records; `Async` + `CancellationToken`; XML docs on public parser APIs; code-behind when a component is more than markup | One type per file. `ActivityCsvRow`, `ActivityCsvError`, `ActivityCsvReadResult`, `ActivitySummary`, and `CurrentActivity` are `record`s. `ReadAsync` and `LoadAsync` take a `CancellationToken`. New parser types carry XML docs. `TablePage.razor.cs` is added. Static fields are PascalCase and local constants camelCase (`CLAUDE.md` 004). | ✅ | ✅ |
| III. Parser library, UI on top | Parser has no ASP.NET Core reference; typed results cross the boundary; components don't open files | The reader is in the parser library and uses only the BCL (R1). `TablePage` binds `CurrentActivity`, and only the loader opens the file (R2). `ParserIndependenceTests` stays green. | ✅ | ✅ |
| IV. BCL XML only (NON-NEGOTIABLE) | Secure `XmlReader` for all XML | No XML is read or written. The existing TCX paths are untouched. | ✅ | ✅ |
| V. Test-first (NON-NEGOTIABLE) | Tests first and failing; parser tests use fixtures; component tests with bUnit; no network or browser | Tasks will put every test before its subject. The reader's tests use the checked-in golden CSVs. The page tests are bUnit and `TcxToolWebFactory`. The style test precedes the `app.css` edit, so it fails on its own. No test drives a browser: SC-002's browser side is a manual quickstart step. | ✅ | ✅ |
| VI. Docker multi-stage, non-root | Non-root, no samples in the image | The Dockerfile is unchanged. The page only reads `/data/csv`, which is already bind-mounted. No new files go into the image. | ✅ | ✅ |
| VII. Compose test gate (NON-NEGOTIABLE) | Tests in the build stage, no network | `compose.yaml` and `compose.test.yaml` are unchanged. There are no new packages, so the offline restore holds. The `FileShare.None` lock that forces `Unreadable` was verified as root in `sdk:11.0` (research facts). | ✅ | ✅ |
| Technology Constraints | 3 projects; `ILogger<T>`; parse failures log file name and error type, never the body | No new project. The loader logs the file name plus the exception type or error kind and line number. It logs no field text (R11). | ✅ | ✅ |
| Quality Gates 1–4 | All pass | Validated by [quickstart.md](./quickstart.md) scenarios 1 and 6 | ✅ | ✅ |

Result: **PASS**, with no violations, so Complexity Tracking stays empty.

Points to note, none a violation:

- **Existing tests change.**
  - `PlaceholderPageTests` drops `TablePage` from its two theories. The page now injects
    `CurrentActivityLoader`, and its heading and route move to `TablePageTests`.
  - `PlaceholderPageEndpointTests` keeps `/table` unchanged. With no state file, the page still
    serves `<title>Table</title>` and `>Table</h1>`.
- **The 003 placeholder contract** for Table ("`<h1>` only") is replaced by
  [contracts/pages.md](./contracts/pages.md), as 003 itself anticipated ("Later features replace
  their content"). FR-021 allows this.
- **Enhanced navigation** is a noted risk, not a change (R4). The navbar is untouched unless
  quickstart scenario 4 shows a problem.

## Project Structure

### Documentation (this feature)

```text
specs/005-table-page/
├── plan.md                       # This file
├── research.md                   # Phase 0: decisions R1–R11 and machine-verified facts
├── data-model.md                 # Phase 1: reader types, Analysis/ types, bands, components, flow
├── quickstart.md                 # Phase 1: validation scenarios 1–7
├── contracts/
│   ├── activity-csv-reader.md    # what the reader accepts, its results, the golden round trip
│   └── pages.md                  # /table markup per status, card, table, styles, messages
├── checklists/
│   └── requirements.md           # Spec quality checklist (from /speckit-specify)
└── tasks.md                      # Phase 2 output (/speckit-tasks, not created here)
```

### Source Code (`tcxtool/`)

```text
src/JoakimSoftware.TcxTool.Parser/Csv/
├── ActivityCsvReader.cs                  # new: ReadAsync(TextReader, CancellationToken)
├── ActivityCsvReadResult.cs              # new
├── ActivityCsvRow.cs                     # new: 17 string fields
├── ActivityCsvError.cs                   # new
└── ActivityCsvErrorKind.cs               # new

src/JoakimSoftware.TcxTool.Web/
├── Program.cs                            # + AddSingleton<CurrentActivityLoader>()
├── Analysis/                             # new
│   ├── CurrentActivityLoader.cs
│   ├── CurrentActivity.cs
│   ├── CurrentActivityStatus.cs
│   ├── ActivitySummary.cs
│   └── CurrentActivityMessages.cs
├── Table/                                # new
│   └── TrackpointBands.cs
├── wwwroot/app.css                       # + scroll box, sticky head, 10 band classes
└── Components/
    ├── _Imports.razor                    # + Web.Analysis, Web.Table, Components.Table, Parser.Csv usings
    ├── Pages/TablePage.razor             # content replaced
    ├── Pages/TablePage.razor.cs          # new code-behind
    └── Table/                            # new
        ├── ActivitySummaryCard.razor
        └── TrackpointTable.razor

tests/JoakimSoftware.TcxTool.Tests/
├── Support/
│   ├── ActivityCsvFixture.cs             # new: writes contract CSVs with made-up values
│   └── BunitTcxToolServices.cs           # + CurrentActivityLoader
├── Parser/ActivityCsvReaderTests.cs      # new
└── Web/
    ├── Analysis/CurrentActivityLoaderTests.cs    # new
    ├── Analysis/CurrentActivityMessagesTests.cs  # new
    ├── Table/TrackpointBandsTests.cs             # new
    ├── Table/ActivitySummaryCardTests.cs         # new (bUnit)
    ├── Table/TrackpointTableTests.cs             # new (bUnit)
    ├── TablePageTests.cs                         # new (bUnit)
    ├── TablePageEndpointTests.cs                 # new (HTTP)
    ├── TablePagePerformanceTests.cs              # new (HTTP, 20,000 rows)
    ├── TableStylesTests.cs                       # new (app.css bands and contrast)
    └── PlaceholderPageTests.cs                   # − TablePage from the theories

CLAUDE.md   # feature history row; Analysis/ and Table/ namespaces; reader in Csv/
```

**Structure Decision**: the existing three-project layout, with no change in project count:

- The parser library gains the reader in its existing `Csv/` namespace.
- The Web project gains an `Analysis/` folder, for loading the current CSV, which later pages
  share, and a `Table/` folder, for this page's band rules, plus `Components/Table/`.
- The test project gains matching folders, following `CLAUDE.md`: folders, not projects.

## Complexity Tracking

No constitution violations to justify.
