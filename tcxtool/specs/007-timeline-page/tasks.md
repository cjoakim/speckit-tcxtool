---

description: "Task list for feature 007-timeline-page"
---

# Tasks: Timeline Page

**Input**: Design documents from `specs/007-timeline-page/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included. The constitution requires test-first (Principle V, NON-NEGOTIABLE).

- Every test task comes before the code it covers, and must be run and seen failing first.
- A test that fails to compile because its subject type or member does not exist yet counts as
  failing. Add the subject in a later task of the same phase.
- A test that checks a file or structure (the vendored D3, `app.css`, `timeline.js`) comes before
  the task that adds it, so it fails on its own (CLAUDE.md, lessons from 001).
- If a test passes before its code is written (a guard), show it failing with a temporary break.
  Revert the break in the same task with an edit, not by restoring a backup (CLAUDE.md, lessons
  from 002 and 005).
- Expected files come only from `Fixtures/generate_success_fixtures.py`, never from running the
  converter (research.md R2).
- No test uses a live browser (constitution Principle V). The drawing is checked by hand in T045
  and quickstart scenario 5.

**Organization**:
- Setup checks the baseline.
- There is no separate foundational phase. US1 changes `StatesCsvRow`, which US2 and US3 build on,
  so the stories are done in order:
  - US1: the 16-field States CSV, and the States page's columns
  - US2: the Timeline page's header, notices, bar, popup, colors, and assets
  - US3: the three charts

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task belongs to (US1, US2, US3)
- All paths are relative to `tcxtool/`. Run every command from `tcxtool/`. The git root is the
  parent directory.

## Path Conventions

These come from plan.md → Project Structure:

- Parser: `src/JoakimSoftware.TcxTool.Parser/Csv/`
- Web: `src/JoakimSoftware.TcxTool.Web/{Timeline,Components/{App.razor,Pages,States,Timeline},wwwroot/{js,lib/d3}}/`
- Tests: `tests/JoakimSoftware.TcxTool.Tests/{Fixtures,Parser,Support,Web,Web/States,Web/Timeline,Web/Analysis}/`

**Conventions for every task**:

- **Code rules**:
  - one public type per file, file-scoped namespaces
  - `_camelCase` only for instance fields; static fields PascalCase; local constants camelCase
  - `Async` methods take a `CancellationToken`, and every `async Task` test name ends in `Async`
    (IDE1006)
  - every public parser type and member has an XML doc comment (`GenerateDocumentationFile`)
  - no relaxed settings or suppressed rules. In particular, no `<script>` in a `.razor` file
    (RZ9992, research.md R7)
- **Namespace traps**: the new Web namespace is `JoakimSoftware.TcxTool.Web.Timeline`, and the
  component folder is `Components/Timeline/`. If the first build after adding them reports an
  ambiguous or hidden name, stop and rename, as for `Convert` and `Activity` (CLAUDE.md, lessons
  from 002 and 005).
- **Test isolation (FR-024)**:
  - HTTP tests get their host only from `Support/TcxToolWebFactory`.
  - bUnit tests register services only through `Support/BunitTcxToolServices`.
  - No test reads `data/`, `specs/`, `.gitignore`, or the real `tcxtool-state.json`.
  - Repository files (`wwwroot/…`, `App.razor`) are found with `Support/RepositoryRoot.Combine`.
- **Real data**: never copy rows from `data/csv/` or `data/tcx/` into a test, fixture, or log. All
  test values are made up, including coordinates.
- **Messages**: notice text comes only from `CurrentStatesMessages` and the markup in
  contracts/pages.md. Tests compare against those.
- **bUnit and async loads**: pages load in `OnInitializedAsync`. Wait with `WaitForElement` or
  `WaitForAssertion` before asserting, as `StatesPageTests` does.
- **Gates**: check `dotnet format`'s exit code directly, never through a pipe.

---

## Phase 1: Setup

**Purpose**: a green baseline, so every later failure is one this feature caused.

- [X] T001 Run quality gates 1–3: `dotnet build JoakimSoftware.TcxTool.slnx`,
  `dotnet format JoakimSoftware.TcxTool.slnx --verify-no-changes`, and
  `dotnet test JoakimSoftware.TcxTool.slnx`. All must pass before any change. Record the test count,
  which was 557 at the end of feature 006.

**Checkpoint**: the suite is green.

---

## Phase 2: User Story 1 - The States CSV carries activity totals and set sizes (Priority: P1) 🎯 MVP

**Goal**: every States CSV has the 16 fields of contracts/states-csv-format.md, with
StateTrackpointCount, ActivityDistanceMiles, and ActivityElapsedSeconds, and with DistanceMiles
renamed StateDistanceMiles. The States page shows them in that order, and a feature 006 file is the
wrong format.

**Independent Test**: convert `run-states.tcx`. Its States CSV equals
`Expected/run-states.states.csv` byte for byte, and has 16 fields per line. Its StateTrackpointCount
values add up to 14, the activity CSV's row count. `/states` shows 16 headings.

### Tests for User Story 1 ⚠️

> Write these first and see them fail before T010.

- [X] T002 [US1] Update `tests/JoakimSoftware.TcxTool.Tests/Fixtures/generate_success_fixtures.py`
  so that `states_rows()` writes the 16 fields of contracts/states-csv-format.md, in that order:
  - `StateTrackpointCount` is `len(set)`.
  - `ActivityDistanceMiles` and `ActivityElapsedSeconds` use the generator's existing
    `last_value()` over the set's DistanceMiles and ElapsedSeconds text.
  - The column formerly `DistanceMiles` is now `StateDistanceMiles`, with the same calculation.

  Use no `sum()` (CLAUDE.md, lessons from 006). Then:
  1. Run the generator.
  2. Confirm with `git diff --stat -- tests/JoakimSoftware.TcxTool.Tests/Fixtures/Expected` that
     only the 8 `*.states.csv` files changed, and that every activity golden is byte-identical.
  3. Update `Fixtures/README.md`'s States CSV description to 16 fields.
- [X] T003 [P] [US1] Update `tests/JoakimSoftware.TcxTool.Tests/Support/StatesCsvFixture.cs`
  (`Line`, `Write`, `Synthetic`) to build 16-field lines in contracts/states-csv-format.md order,
  from `StatesCsvWriter.Header`. Synthetic values stay made up.
- [X] T004 [P] [US1] Update `tests/JoakimSoftware.TcxTool.Tests/Parser/StatesCsvWriterTests.cs`:
  - `Header` is exactly the contract's header line, and `FieldCount` is 16.
  - `StateTrackpointCount` is the set's row count (data-model.md `StatesCsvRow.StateTrackpointCount`),
    including `1` for a one-row set.
  - `ActivityDistanceMiles` and `ActivityElapsedSeconds` are the last non-empty values, as written
    (data-model.md `StatesCsvRow.ActivityDistanceMiles`, `StatesCsvRow.ActivityElapsedSeconds`):
    - a set whose last row has no DistanceMiles takes the previous row's text
    - a set with none is empty
    - the text is copied, not reformatted, so `41.250` stays `41.250`
  - `StateDistanceMiles` keeps feature 006's `DistanceMiles` values (data-model.md
    `StatesCsvRow.StateDistanceMiles`, FR-003).
  - Over the `run-states` rows, the counts add up to the activity row count, and the last row's two
    activity values equal the last activity row's DistanceMiles and ElapsedSeconds.
  - `Write` puts the fields in record order. The other 006 tests are updated to the new field
    positions.
- [X] T005 [P] [US1] Update `tests/JoakimSoftware.TcxTool.Tests/Parser/StatesCsvReaderTests.cs`:
  - every `Expected/*.states.csv` reads back to 16 fields per row, mapped to the right properties
  - a file with feature 006's 13-field header, copied into the test as a literal, gives
    `HeaderMismatch` on line 1 (FR-006, research.md R3)
  - field-count errors now report against 16
- [X] T006 [P] [US1] Update `tests/JoakimSoftware.TcxTool.Tests/Web/States/StatesTableTests.cs`:
  - 16 `th` in the contracts/states-csv-format.md order, with `StateDistanceMiles` in place of
    `DistanceMiles`
  - 16 `td` per row, each as written
  - the heart-rate band class on column 11 and the cadence band class on column 12, and no
    attribute on any other cell (contracts/pages.md "States page")
  - hand-built `StatesCsvRow`s use the 16-argument order
- [X] T007 [P] [US1] Update `tests/JoakimSoftware.TcxTool.Tests/Web/Analysis/CurrentStatesLoaderTests.cs`:
  - its rows use the 16-field fixture
  - a States CSV with the 13-field 006 header loads as `WrongFormat`, with a warning that names the
    file and `HeaderMismatch` (FR-006)
- [X] T008 [P] [US1] Update `tests/JoakimSoftware.TcxTool.Tests/Web/StatesPageTests.cs`,
  `StatesPageEndpointTests.cs`, and `StatesPagePerformanceTests.cs`:
  - each uses the 16-field fixture
  - the performance test's marker cell and row count still identify the rows
  - add one endpoint case: a current CSV whose States CSV has the 13-field header shows the
    `WrongFormat` notice with its Convert link
- [X] T009 [US1] Build and run the suite. Confirm that T003–T008 fail, by compile error (the
  16-argument `StatesCsvRow`) or for their own reasons. Confirm that `TcxCsvConverterTests` fails
  against the regenerated goldens.

  **Done note**: the test project failed to compile (15 errors: no 16-argument `StatesCsvRow`
  constructor, and no `ActivityDistanceMiles`, `ActivityElapsedSeconds`, `StateTrackpointCount`, or
  `StateDistanceMiles`). Because of that, `TcxCsvConverterTests` couldn't run against the new goldens
  until T010–T012. After T013, all 564 tests passed.

### Implementation for User Story 1

- [X] T010 [US1] Change `src/JoakimSoftware.TcxTool.Parser/Csv/StatesCsvRow.cs` to the 16 positional
  properties, in the order of data-model.md `StatesCsvRow`:
  - **New**: `StatesCsvRow.ActivityDistanceMiles`, `StatesCsvRow.ActivityElapsedSeconds`, and
    `StatesCsvRow.StateTrackpointCount`, at positions 2, 4, and 8.
  - **Renamed**: `DistanceMiles` becomes `StatesCsvRow.StateDistanceMiles`, at position 10.
  - **Carried over, rules unchanged, at their new positions**: `StatesCsvRow.ActivityId`,
    `StatesCsvRow.ActivityElapsedTime`, `StatesCsvRow.State`, `StatesCsvRow.StateEndDate`,
    `StatesCsvRow.StateEndTrackpointNum`, `StatesCsvRow.StateElapsedSeconds`,
    `StatesCsvRow.AverageHeartRateBpm`, `StatesCsvRow.AverageRunCadence`,
    `StatesCsvRow.AverageLatitude`, `StatesCsvRow.AverageLongitude`,
    `StatesCsvRow.AverageAltitudeMeters`, and `StatesCsvRow.AverageAltitudeFeet`.

  Update its XML docs.
- [X] T011 [US1] Change `src/JoakimSoftware.TcxTool.Parser/Csv/StatesCsvWriter.cs`:
  - `Header` and `FieldCount = 16` (contracts/states-csv-format.md)
  - `Rows` sets data-model.md `StatesCsvRow.StateTrackpointCount` to `set.Count`, formatted with
    `CultureInfo.InvariantCulture`
  - `Rows` sets `StatesCsvRow.ActivityDistanceMiles` and `StatesCsvRow.ActivityElapsedSeconds` with
    `LastNonEmpty` (research.md R1)
  - `Write` writes the fields in record order

  Every other value's rule is unchanged.
- [X] T012 [US1] Change `src/JoakimSoftware.TcxTool.Parser/Csv/StatesCsvReader.cs` to map field
  `i` to the `i`th property of data-model.md `StatesCsvRow`, from `StatesCsvRow.ActivityId` (1) to
  `StatesCsvRow.AverageAltitudeFeet` (16), each as written. Its header check already rejects the
  13-field file.
- [X] T013 [US1] Change `src/JoakimSoftware.TcxTool.Web/Components/States/StatesTable.razor` to the
  16 headings and cells, moving the band classes with the AverageHeartRateBpm and AverageRunCadence
  cells (FR-005, contracts/pages.md "States page"). Keep the one-line row loop and no scoped CSS.
- [X] T014 [US1] Run quality gates 1–3. Every test passes.

**Checkpoint**: conversions write 16-field States CSVs, the States page shows them, and a feature
006 file is the wrong format.

---

## Phase 3: User Story 2 - See the whole activity as one bar of states (Priority: P2)

**Goal**: `/timeline` shows the same notices as `/states`, or a header card, a legend, and
`<tcx-timeline>` with the layout JSON. In the browser, the vendored D3 draws one bar of
time-proportional, state-colored segments, with a time axis and a hover popup that a click outside
closes.

**Independent Test**: in an HTTP test host, write a 16-field States CSV and make it current, then
`GET /timeline`.
- The header card shows the five values.
- The decoded `data-timeline` JSON has 16 `fieldNames` and one contiguous segment per row, ending at
  exactly 1.
- The page is static, and changes no file.
- `js/timeline.js` and `lib/d3/d3.min.mjs` are served.

### Tests for User Story 2 ⚠️

> Write these first and see them fail before T025.

- [X] T015 [P] [US2] Create `tests/JoakimSoftware.TcxTool.Tests/Web/Timeline/TimelineHeaderTests.cs`
  for data-model.md `TimelineHeader` (FR-010, research.md R13):
  - `FileName` is the CSV name.
  - `ActivityId` comes from the first row.
  - `TimelineHeader.Date` is `2026-11-01` for `2026-11-01_x.csv` and
    `2026-11-01_120407_x.csv`, and empty for `notes.csv` and `2026-13-01_x.csv`.
  - `TimelineHeader.ActivityDistanceMiles` and `TimelineHeader.ActivityElapsedTime` come from the
    last row with each value, so a last row with none takes the previous one.
  - An empty `rows` throws `ArgumentException`.
- [X] T016 [P] [US2] Create `tests/JoakimSoftware.TcxTool.Tests/Web/Timeline/TimelineLayoutTests.cs`
  for the segment half of data-model.md `TimelineLayout` and `TimelineSegment` (FR-012, FR-013,
  research.md R5):
  - `TimelineLayout.FieldNames` is the 16 header names.
  - `TimelineLayout.TotalSeconds` is the left-to-right sum.
  - Seconds 30, 10, 60 give `TimelineSegment.Start` 0, 0.3, 0.4 and `TimelineSegment.End` 0.3, 0.4,
    exactly 1, and each `TimelineSegment.Center` is the midpoint.
  - Each `End` equals the next `Start`, so there are no gaps.
  - An empty, non-numeric, negative, `NaN`, or infinite StateElapsedSeconds counts as 0, so that
    segment has zero width.
  - When every value is 0, all `n` segments are `1/n` wide.
  - One set gives one segment from 0 to 1.
  - `TimelineSegment.State` and the 16 `TimelineSegment.Values` are exactly as written, in order.
  - An empty `rows` throws `ArgumentException`.
- [X] T017 [P] [US2] Create `tests/JoakimSoftware.TcxTool.Tests/Web/States/StatesNoticeTests.cs`
  (research.md R14, contracts/pages.md "`StatesNotice`"):
  - for each non-`Loaded` status, the rendered alert has the feature 006 classes, role, text from
    `CurrentStatesMessages`, `<strong>` name, and links, with `id` equal to the `StatusId`
    parameter
  - `Loaded` renders nothing
- [X] T018 [P] [US2] Create `tests/JoakimSoftware.TcxTool.Tests/Web/TimelinePageTests.cs` (bUnit,
  contracts/pages.md "Timeline page"):
  - For each non-`Loaded` status, the page shows the notice with `id="timeline-status"` and the
    same text and links as the States page. It has no `.card` and no `tcx-timeline`.
  - For `Loaded`:
    - `#timeline-summary` holds the five labels and values, with a muted dash for an empty value.
    - `#timeline-legend` has the four `timeline-swatch state-…` items, in order.
    - `tcx-timeline` has a `data-timeline` attribute whose value parses as JSON with camelCase
      names, 16 `fieldNames`, and one segment per row (contracts/timeline-data.md).
    - `tcx-timeline` contains `div.timeline-bar`.
    - `#timeline-noscript` is inside `noscript`.
  - Exactly one `h1`, reading "Timeline".
- [X] T019 [P] [US2] Create `tests/JoakimSoftware.TcxTool.Tests/Web/TimelinePageEndpointTests.cs`
  (through `TcxToolWebFactory`):
  - `GET /timeline` returns 200, with `<title>Timeline</title>` and the `h1`, and the Timeline
    navbar link is active.
  - **Static guard**: the body has no `<!--Blazor:{` (FR-022, CLAUDE.md lessons from 005).
  - **No-writes guard**: the temp directory's bytes and modification times are unchanged across two
    requests (FR-009).
  - **Encoding**: a States CSV value containing `"`, `<`, `&`, and `</script>` is HTML-encoded in
    the attribute, and parses back to the exact text after decoding (research.md R7).
  - A 13-field States CSV shows the `WrongFormat` notice (FR-006).
  - **States CSV only** (FR-007): with the current activity CSV deleted and its States CSV present,
    `GET /timeline` still returns 200 with the header card and one segment per row.
  - The body loads `js/timeline.js` with `type="module"` (contracts/pages.md "Shared").
- [X] T020 [P] [US2] Create `tests/JoakimSoftware.TcxTool.Tests/Web/TimelinePagePerformanceTests.cs`,
  modelled on `StatesPagePerformanceTests`:
  - 541 synthetic StateSets, with an untimed warm-up
  - `GET /timeline` returns 200 within 5 s
  - the decoded JSON has 541 segments
- [X] T021 [P] [US2] Create `tests/JoakimSoftware.TcxTool.Tests/Web/TimelineAssetsTests.cs`
  (research.md R8, R9; FR-023):
  - `GET /js/timeline.js` and `GET /lib/d3/d3.min.mjs` return 200, with a JavaScript content type.
  - `wwwroot/lib/d3/d3.min.mjs` starts with `// https://d3js.org v7.9.2`.
  - `wwwroot/lib/d3/LICENSE` exists, and `README.md` names 7.9.2 and the npm integrity value.
  - `wwwroot/js/timeline.js` contains `customElements.define("tcx-timeline"` and uses the
    `state-` class prefix.
  - **No-external-URL guard**: `timeline.js` contains no `http://` or `https://`.
  - **No-color guard**: `timeline.js` contains no hex color literal (regex
    `#[0-9a-fA-F]{3}(?:[0-9a-fA-F]{3})?\b`), and no `rgb(` (research.md R12).
- [X] T022 [P] [US2] Create `tests/JoakimSoftware.TcxTool.Tests/Web/TimelineStylesTests.cs`. Strip
  `/* */` comments before parsing `wwwroot/app.css` (CLAUDE.md, lessons from 005). Then check:
  - `:root` defines the four `--tcx-state-*` properties with exactly the FR-014 values
  - each of the four hex values appears once in the file
  - `.state-stopped`, `.state-walking`, `.state-running`, and `.state-unknown` set `fill` and
    `background-color` from their property
  - `.timeline-swatch`, `.timeline-line` (stroke `#adb5bd`, 1px), `tcx-timeline`, and
    `.timeline-popup` exist, as in contracts/pages.md "Styles"
- [X] T023 [P] [US2] Remove the Timeline entries from
  `tests/JoakimSoftware.TcxTool.Tests/Web/PlaceholderPageTests.cs` and
  `PlaceholderPageEndpointTests.cs`. Update their summaries: only Histograms is a placeholder.
  `MainLayoutTests` is unchanged.
- [X] T024 [US2] Build and run the suite. Confirm that T015–T022 fail:
  - by compile error (`TimelineHeader`, `TimelineLayout`, `StatesNotice`), or for their own reasons
  - the asset and style tests fail because the files and rules don't exist yet

  The two guards in T021 fail here only because `timeline.js` is missing. They are shown failing
  again for the right reason in T033.

### Implementation for User Story 2


  **Done note**: the build failed only in the three test files whose types didn't exist yet (`Timeline`
  namespace, `StatesNotice`). Once T025–T029 compiled, 18 tests failed, all asset, style, or module
  tests whose files didn't exist yet, so each failed on its own before T030–T032.
- [X] T025 [US2] Create the `JoakimSoftware.TcxTool.Web.Timeline` namespace:
  - `src/JoakimSoftware.TcxTool.Web/Timeline/TimelineSegment.cs`, a record with data-model.md
    `TimelineSegment.State`, `.Start`, `.End`, `.Center`, and `.Values`
  - `src/JoakimSoftware.TcxTool.Web/Timeline/TimelineLayout.cs`, with data-model.md
    `TimelineLayout.TotalSeconds`, `.FieldNames`, `.Segments`, and `From(rows)`, per research.md R5
    - parse with `double.TryParse(…, NumberStyles.Float, CultureInfo.InvariantCulture, …)`, finite
      and ≥ 0 only
    - sum left to right
    - set the last `End` to exactly `1.0`

  `Charts` is added in US3.
- [X] T026 [US2] Create `src/JoakimSoftware.TcxTool.Web/Timeline/TimelineHeader.cs` with
  data-model.md `TimelineHeader.FileName`, `TimelineHeader.ActivityId` (first row),
  `TimelineHeader.Date`, `TimelineHeader.ActivityDistanceMiles`, and
  `TimelineHeader.ActivityElapsedTime` (last row with each value), and `From(csvFileName, rows)`.
  Parse the date with
  `DateOnly.TryParseExact(name[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, …)`, guarding the
  length.
- [X] T027 [US2] Create `src/JoakimSoftware.TcxTool.Web/Components/States/StatesNotice.razor` by
  moving the notice `switch` out of `Components/Pages/StatesPage.razor` (research.md R14):
  - parameters `Current` and `StatusId`; nothing for `Loaded`
  - `StatesPage` renders `p#states-file` (for `NoStateSets` and `Loaded`), then
    `<StatesNotice StatusId="states-status" …/>`, then the table

  The unchanged `StatesPageTests` must still pass.
- [X] T028 [US2] Create `src/JoakimSoftware.TcxTool.Web/Components/Timeline/TimelineHeaderCard.razor`,
  copying `ActivitySummaryCard`'s markup with `id="timeline-summary"`. Then create
  `Components/Timeline/TimelineView.razor` per contracts/pages.md "Timeline view":
  - the legend
  - `<tcx-timeline data-timeline="@Json">` containing `div.timeline-bar`
  - the `noscript` note
  - `Json` comes from a static `JsonSerializerOptions` (`JsonSerializerDefaults.Web`), held in a
    PascalCase static field

  Add `@using` lines for `Components.Timeline` and `JoakimSoftware.TcxTool.Web.Timeline` to
  `Components/_Imports.razor`.
- [X] T029 [US2] Replace `src/JoakimSoftware.TcxTool.Web/Components/Pages/TimelinePage.razor`, and
  create `TimelinePage.razor.cs`, modelled on `StatesPage`:
  - inject `CurrentStatesLoader`, and load in `OnInitializedAsync` with `RequestAborted`
  - show `<StatesNotice StatusId="timeline-status">`, or, for `Loaded`, `TimelineHeaderCard` and
    `TimelineView`
  - no `@rendermode`
- [X] T030 [US2] Add the contracts/pages.md "Styles" rules to the end of
  `src/JoakimSoftware.TcxTool.Web/wwwroot/app.css`, under a comment naming feature 007 and
  research.md R10 and R12. Include `.timeline-line`, so US3 needs no CSS change.
- [X] T031 [US2] Vendor D3 (research.md R9):
  1. Download `https://registry.npmjs.org/d3/-/d3-7.9.2.tgz` into the scratchpad.
  2. Check that its SHA-512 in base64 equals npm's `integrity` value
     (`sha512-ZKb/mONPVKzaO5UlcWcwN8bS4ezvQaYu+bsou+XgNABdGC800kwH651ADTbbTxMPHW13UlvbQafdmJQDk1w7Mg==`).
  3. Copy `package/dist/d3.min.mjs` and `package/LICENSE` into
     `src/JoakimSoftware.TcxTool.Web/wwwroot/lib/d3/`.
  4. Write `wwwroot/lib/d3/README.md` with the version, the tarball URL, the integrity value, and
     the file's SHA-256.

  Send no project or personal data with the request.

  **Done note**: the tarball's SHA-512 matched npm's integrity value. But `package/dist/d3.min.mjs` is
  796 bytes that only re-export 30 bare `d3-*` package names (`export*from"d3-array"` …), which a
  browser can't resolve without an import map and 30 more vendored modules. The planned check (the
  `.mjs` is served as JavaScript) would have passed while the import failed in the browser. So the
  self-contained UMD `package/dist/d3.min.js` (SHA-256 `912111098ea9…07f0c1`, as recorded in
  planning) is vendored instead, with `LICENSE` and `README.md`. The R8 fallback (a plain
  `<script>` in `App.razor`) would have broken FR-022's "one shared script reference", so
  `timeline.js` instead adds a `<script>` for `../lib/d3/d3.min.js` (resolved with
  `import.meta.url`) the first time a `<tcx-timeline>` connects, and uses the `d3` global. D3 is still
  fetched only on the Timeline page. T021's two D3 tests were changed to match: the module names
  that path and `/lib/d3/d3.min.js` is served as `text/javascript`, and no page has a `<script>` or
  `<link>` for `lib/d3/` (`<ImportMap />` lists every JS asset but loads none, so the test matches
  tags, not the bare path).
- [X] T032 [US2] Create `src/JoakimSoftware.TcxTool.Web/wwwroot/js/timeline.js`, an ES module, per
  research.md R8, R10, R11 and contracts/timeline-data.md:
  - `class TcxTimeline extends HTMLElement`, with `observedAttributes = ["data-timeline"]`, and
    `connectedCallback`, `attributeChangedCallback`, and `disconnectedCallback`
  - D3 imported with `import(new URL("../lib/d3/d3.min.mjs", import.meta.url))`
  - the bar's rects with `timeline-segment state-<state>` classes, using `state-unknown` for any
    other value
  - the `h:mm:ss` time axis
  - the popup: `pointermove` bisect over the segment starts, then the 4 px nearest-center rule of
    contracts/timeline-data.md "Popup"; values set with `textContent`; closed
    by a document `click` outside it
  - a `ResizeObserver` redraw
  - margins 56 and 16 px, shared by every drawing
  - no colors and no URLs

  Then add `<script type="module" src="@Assets["js/timeline.js"]"></script>` after `blazor.web.js`
  in `src/JoakimSoftware.TcxTool.Web/Components/App.razor`.

  If T021's served-`.mjs` check fails, use the research.md R8 fallback instead (a plain `d3.min.js`
  script, with the module using the `d3` global), and record it in a done note.

  **Done note**: the module is as described, with D3 loaded per the T031 done note. The popup is
  `div.timeline-popup.card.small.shadow` with `role="tooltip"` and a Bootstrap `dl.row`. It is
  rebuilt only when the hovered segment changes, and is placed beside the pointer, flipped left near
  the right edge. `node --check` passes. Browser behavior is checked in T045 and by hand.
- [X] T033 [US2] Show the four guards failing with temporary breaks, each reverted with an edit, and
  rerun the filtered tests after each revert:
  - **No writes**: append one byte to the States CSV in `CurrentStatesLoader.LoadAsync`. The
    T019 no-writes test fails.
  - **Static**: add `@rendermode InteractiveServer` to `TimelinePage.razor`. The T019 static test
    fails.
  - **No color**: add `const debug = "#ffc107";` to `timeline.js`. The T021 color guard fails.
  - **No URL**: add a `// https://d3js.org` comment to `timeline.js`. The T021 URL guard fails.

  Touch or rebuild after each Razor or C# revert (CLAUDE.md, lessons from 002).

  **Done note**: all four guards failed with their break and passed after the revert. No writes: 3
  `TimelinePageEndpointTests` failed, including `ServingThePageChangesNoFileAsync` (the appended
  byte also corrupts the last row, which fails two more). Static: only
  `ThePageIsStaticallyRenderedAsync`. Color and URL: exactly one `TimelineAssetsTests` test each.
  Each C# or Razor file was `touch`ed after its revert, and `timeline.js` was compared byte for byte
  with a copy taken before the breaks.
- [X] T034 [US2] Run quality gates 1–3. Every test passes.

**Checkpoint**: `/timeline` shows the header, the legend, and a drawn, hoverable bar of states, or
the same notices as `/states`.

---

## Phase 4: User Story 3 - Compare cadence, heart rate, and altitude across the activity (Priority: P3)

**Goal**: under the bar, three charts (Run Cadence, Heart Rate, Altitude) of the StateSets'
averages, each over its own minimum and maximum. Each point sits at its segment's center, and thin
gray lines join the points. All four drawings have the same width and left edge.

**Independent Test**:
- `TimelineLayout.From` gives three charts in order, with the right domains and points.
- `/timeline` renders three labels, with a `div.timeline-chart` for each chart that has points and
  "No data." for one that has none.
- Alignment is checked by hand in T045.

### Tests for User Story 3 ⚠️

> Write these first and see them fail before T038.

- [X] T035 [P] [US3] Add chart tests to
  `tests/JoakimSoftware.TcxTool.Tests/Web/Timeline/TimelineLayoutTests.cs` (FR-017, FR-019 to
  FR-021, research.md R6):
  - `TimelineLayout.Charts` has exactly three, with `TimelineChart.Label` "Run Cadence", "Heart
    Rate", "Altitude" and `TimelineChart.Field` `AverageRunCadence`, `AverageHeartRateBpm`,
    `AverageAltitudeMeters`, in that order.
  - Each `TimelineChart.Points` has one `TimelinePoint` per set with a finite value, in set order.
    Each `TimelinePoint.Center` equals its segment's `Center`, and `TimelinePoint.Value` is the
    parsed value. An empty or non-numeric set is skipped.
  - `TimelineChart.Min` and `TimelineChart.Max` are the smallest and largest values.
  - All equal values `v` give `v − 1` and `v + 1`.
  - No values give null for both, and empty `Points`.
  - A synthetic 541-set layout, with charts, is built in under 100 ms after one untimed warm-up
    call.
- [X] T036 [P] [US3] Add chart tests to `tests/JoakimSoftware.TcxTool.Tests/Web/TimelinePageTests.cs`:
  - The decoded JSON has three `charts`, with camelCase `label`, `field`, `min`, `max`, and
    `points`.
  - Inside `tcx-timeline`, after `div.timeline-bar`, are three `h2.h6` labels in order. Each is
    followed by `div.timeline-chart[data-chart=i]` when its chart has points, or by
    `p.timeline-no-data` "No data." when it has none. Use a fixture with all AverageAltitudeMeters
    empty (contracts/pages.md "Timeline view").
- [X] T037 [P] [US3] Add a structural check to
  `tests/JoakimSoftware.TcxTool.Tests/Web/TimelineAssetsTests.cs`: `timeline.js` uses `axisLeft`,
  `d3.line`, and the `timeline-line` and `timeline-chart` class names. This only checks that the
  module is wired to the contract; the drawing itself is checked in T045.
- [X] T038 [US3] Build and run the suite. Confirm that T035–T037 fail, by compile error
  (`TimelineChart`, `TimelinePoint`, `TimelineLayout.Charts`) or for their own reasons.

### Implementation for User Story 3


  **Done note**: the build failed with 28 errors in `TimelineLayoutTests` (`TimelinePoint` and
  `TimelineLayout.Charts` missing). T039, T040, and the point style went in together, so the T036
  structure test and the point-style test were then shown failing by removing the chart markup and
  the `.timeline-point` rule temporarily. Both failed, and both passed once the files were restored
  (compared byte for byte, then `touch`ed). The JSON-charts test needs T039's types, so its failure
  was the compile error.
- [X] T039 [US3] Create `src/JoakimSoftware.TcxTool.Web/Timeline/TimelineChart.cs` and
  `TimelinePoint.cs` per data-model.md `TimelineChart` (`Label`, `Field`, `Min`, `Max`, `Points`)
  and `TimelinePoint` (`Center`, `Value`). Add data-model.md `TimelineLayout.Charts` to
  `TimelineLayout.From`, per research.md R6, using the same invariant, finite-only parse as the
  segments.
- [X] T040 [US3] Extend `src/JoakimSoftware.TcxTool.Web/Components/Timeline/TimelineView.razor` to
  render, inside `tcx-timeline` and after `div.timeline-bar`, each chart's `h2.h6.mt-3.mb-1` label,
  then its `div.timeline-chart` or `p.timeline-no-data` (contracts/pages.md).
- [X] T041 [US3] Extend `src/JoakimSoftware.TcxTool.Web/wwwroot/js/timeline.js` to draw each chart
  into its `div.timeline-chart[data-chart=i]`, per contracts/timeline-data.md "Charts":
  - an SVG 140 px tall, with the shared margins
  - `d3.scaleLinear([min, max])` with `d3.axisLeft` and 4 ticks
  - circles of radius 2.5 at `56 + center × p`
  - one `path.timeline-line` from `d3.line()`
  - redrawn on resize together with the bar
  - still no colors and no URLs

  **Done note**: the circles have class `timeline-point`, and `app.css` gives it
  `fill: var(--bs-body-color)`. Without a rule they would be black, and a new hex color would be a
  second place colors are defined. `TimelineStylesTests.ThePointsUseTheBodyTextColor` checks it. The
  y-scale range keeps 8 px of padding at the top and bottom of each 140 px chart.
- [X] T042 [US3] Run quality gates 1–3. Every test passes, including the T021 guards.

**Checkpoint**: all three stories work.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T043 Update `CLAUDE.md`:
  - **Feature history**: add the `specs/007-timeline-page` row.
  - **Parser `Csv/`**: the States CSV has 16 fields, in
    `specs/007-timeline-page/contracts/states-csv-format.md`, which replaces 006's field list.
  - **New Web `Timeline/` namespace**: `TimelineLayout`, as fractions; `TimelineHeader`.
  - **`Components/States/StatesNotice`**: shared by the States and Timeline pages.
  - **Browser code**: D3 7.9.2 vendored in `wwwroot/lib/d3/`, and `wwwroot/js/timeline.js` defines
    `tcx-timeline`, loaded once in `App.razor`.
  - **Lessons from feature 007**:
    - Enhanced navigation doesn't run page scripts, so use a custom element.
    - A `<script>` in a component is RZ9992, so pass data in an attribute.
    - Put drawing geometry in C# so that it can be tested without a browser.
    - Add any lesson that implementation turns up.

  Do not edit `specs/*/checklists/`.

  **Done note**: besides the listed items, the lessons record: what D3's `.mjs` really is, that
  `<ImportMap />` lists every JS asset, CS8207 in FluentAssertions predicates, the CS0102 clash with
  `TimelineHeader.Date`, and showing a test failing when several files went in at once. The
  `CurrentActivityLoader` line now says Timeline reads only the States CSV.
- [X] T044 Run the Compose gate (Principle VII):
  1. Check that `docker info` succeeds and port 8080 is free. Otherwise prefix the command with
     `TCXTOOL_HOST_PORT=8081`.
  2. Run `docker compose -f compose.yaml -f compose.test.yaml up --build --abort-on-container-exit --exit-code-from tests`.
  3. Confirm exit code 0 and the test count.
  4. Run `docker compose -f compose.yaml -f compose.test.yaml down`.

  **Done note**: `docker info` succeeded and port 8080 was free. The gate exited 0 with 650 tests
  passed, then `down` removed the containers and network.
- [X] T045 Run the specs/007-timeline-page/quickstart.md scenarios 2, 3, 4, and 6 against a
  scratchpad folder, never `data/csv/` or `tcxtool-state.json`. Read only the columns the scenarios
  name.
  - Scenario 2 must show 541 StateSets, counts adding up to 19,908, and the last row's two activity
    values equal to the activity CSV's last DistanceMiles and ElapsedSeconds.
  - Scenario 6 must print `unchanged`.
  - From scenario 5, check server-side only: `/timeline` returns 200, with 541 segments in its JSON
    and three chart labels.
  - Delete the scratchpad copies.
  - Record the results in a done note. Leave scenario 5's browser checks (drawing, popup,
    alignment at three widths, draw time, enhanced navigation, no script) to the maintainer.

  **Done note**: run against a scratchpad folder on port 5199.
  - **Scenario 2**: the 16-field header, no row with another field count, 541 StateSets, counts
    adding up to 19,908 (the activity CSV's row count), and a last row of `26.427414388253847|19881`,
    equal to the activity CSV's last DistanceMiles and ElapsedSeconds.
  - **Scenario 3**: `/states` has 16 headings and 16 cells per row, with classes only on columns 11
    and 12. A made-up 13-field file shows the WrongFormat notice on `/states` and `/timeline`.
  - **Scenario 4**: no state file, the States CSV moved away, `hello`, and header-only each return
    200 on both pages, with the same notice text and no `<tcx-timeline>`.
  - **Scenario 5, server side**: `/timeline` returned 200, with 541 segments (last `end` 1,
    `totalSeconds` 19881), three charts of 541 points, the three labels, five header values, and no
    `<!--Blazor:{`.
  - **Also, beyond the task**: headless Chrome's `--dump-dom` at 800, 1200, and 1800 px drew 4 SVGs of
    equal width each time (696, 1116, 1296 px), with 541 state rects in three state classes, 1,623
    points, 3 lines, and one `d3.min.js` script added by the module. A screenshot at 1200 px showed
    the header, legend, bar, time axis, and three charts aligned. The time ticks fall at D3's round
    second steps (0:33:20, 1:06:40, …), which the contract allows.
  - **Scenario 6** printed `unchanged`. The app was stopped and the scratchpad copies deleted.
  - Left for the maintainer: the popup, resize, enhanced navigation, no-script, and timing checks
    of scenario 5, in an interactive browser.
- [X] T046 Run the final gates 1–3 and `git status`. Confirm that no file under `data/` and no
  `tcxtool-state.json` was changed by this feature. Report, without fixing, that every existing
  States CSV under `data/csv/` is now in the 13-field format and needs one reconversion by the
  maintainer. Also report that SC-002, SC-003, SC-004, FR-015, FR-016, and FR-018's resize case
  can only be checked in a browser, which the constitution rules out for tests. They stay
  unchecked until the maintainer runs quickstart scenario 5.

  **Done note**: gates 1–3 passed (0 warnings, format clean, 650 tests). `git status` shows no
  change under `data/` and no `tcxtool-state.json`. The one States CSV under `data/csv/` has the
  13-field header and needs one reconversion. SC-002, SC-003 (in an interactive browser), SC-004,
  FR-015, FR-016, and FR-018's resize case stay for the maintainer's scenario 5.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (T001)**: first.
- **US1 (T002–T014)**: after Setup. It changes `StatesCsvRow`, which every later phase uses.
- **US2 (T015–T034)**: after US1. The layout and header read 16-field rows.
- **US3 (T035–T042)**: after US2. It extends `TimelineLayout`, `TimelineView`, and `timeline.js`.
- **Polish (T043–T046)**: after US3.

### Within Each Phase

- **US1**: T002 (goldens) first. Then T003–T008, then T009, which confirms the failures. Then
  T010 → T011 → T012 → T013 → T014.
- **US2**: T015–T023 → T024. Then:
  - T025, T026, and T027 in any order
  - T028 after T025–T027
  - T029 after T028
  - T030, T031, and T032 any time after T024, with T032 after T031
  - T033 after T029 and T032
  - T034 last
- **US3**: T035–T037 → T038 → T039 → T040 → T041 → T042.

### Parallel Opportunities

- US1 tests T003–T008 each touch different files, so all can run in parallel once T002 has
  regenerated the goldens.
- US2 tests T015–T023 are independent. T030 and T031 are independent of the C# tasks.
- US3 tests T035–T037 are independent.

## Parallel Example: User Story 1

```text
(after T002) T003 StatesCsvFixture   T004 StatesCsvWriterTests   T005 StatesCsvReaderTests
             T006 StatesTableTests   T007 CurrentStatesLoaderTests   T008 States page tests
```

## Parallel Example: User Story 2

```text
T015 TimelineHeaderTests   T016 TimelineLayoutTests   T017 StatesNoticeTests   T018 TimelinePageTests
T019 TimelinePageEndpointTests   T020 TimelinePagePerformanceTests   T021 TimelineAssetsTests
T022 TimelineStylesTests   T023 placeholder test cleanup
(after T024) T030 app.css   T031 vendor D3   alongside T025–T029
```

## Implementation Strategy

### MVP First (User Story 1 Only)

US1 alone gives the 16-field States CSV, with set sizes and running activity totals, shown on the
States page. It is releasable on its own. The Timeline page stays a placeholder.

### Incremental Delivery

1. US1: the 16-field States CSV. Gates pass at T014.
2. US2: the Timeline page's header and bar. Gates pass at T034. The page is useful without charts.
3. US3: the three charts. Gates pass at T042.
4. Polish: CLAUDE.md, the Compose gate, the quickstart, and the final check.

## Notes

- Mark each task `[X]` when done, and add a short *done note* when the task did something the text
  did not foresee.
- `specs/*/checklists/` are read-only during `/speckit-implement`.
- `.specify/feature.json` points at `specs/007-timeline-page`.

---

## Phase 6: Convergence

- [X] T047 Make every narrow segment reachable by hover per spec Edge Cases "Very short StateSets" and FR-015 (partial): in `src/JoakimSoftware.TcxTool.Web/wwwroot/js/timeline.js` `#hover`, first look for segments under 4 px wide whose center is within ±4 px of the pointer. If there is one, the nearest wins (ties go to the earlier one), even when the bisected segment is wide. Otherwise use the bisected segment. Update the "Popup" row of `specs/007-timeline-page/contracts/timeline-data.md` to match. Check by hand that a zero-width `stopped` set between two `running` segments of the marathon opens its popup.
  **Done note**: a headless Chrome probe (DevTools protocol, 1200 px, the marathon's 541 sets) split
  the widest `running` segment (8.9 px) around a zero-width `stopped` one by rewriting `data-timeline`.
  Before the change, hovering at the zero-width segment's center, or 3 px either side, showed a
  `running` popup. After it, the center shows the zero-width set. At ±3 px a nearer narrow neighbor
  wins (a 0.21 px `stopped` set 1.57 px away, and a 1.84 px `walking` set 2.38 px away), as the rule
  says. The center of the widest segment (23.5 px) still shows that segment. Updated the contract's
  "Popup" row.
- [X] T048 Keep the drawing after an enhanced navigation to the same page per FR-018 and plan R8 (partial): first confirm in a browser that clicking the Timeline navbar link while on `/timeline` leaves the bar and charts empty. If it does, make `<tcx-timeline>` in `src/JoakimSoftware.TcxTool.Web/wwwroot/js/timeline.js` redraw when its drawn SVGs are removed: a `MutationObserver` on its own `childList`/`subtree`, disconnected in `disconnectedCallback`, and not triggered by its own draws. Record the result in a done note, and add the lesson to `CLAUDE.md` if it is confirmed.
  **Done note**: confirmed. After clicking the Timeline link on `/timeline`, `window` kept a marker,
  so it was an enhanced navigation. The 4 containers stayed, but the element went from 4 SVGs to 0.
  `data-timeline` doesn't change, so `attributeChangedCallback` doesn't run. The element now has a
  `MutationObserver` on its `childList` and `subtree`. It redraws when a removed node is or contains
  an `svg`, and `#draw` drops its own records with `takeRecords()`. After the change, the probe saw 4
  SVGs after the navigation, a working popup, and no SVG added in the next second, so there is no
  redraw loop. The lesson is in `CLAUDE.md`.
- [X] T049 Limit the popup's hover area to the 32 px bar per contracts/timeline-data.md "Popup" (partial): in `src/JoakimSoftware.TcxTool.Web/wwwroot/js/timeline.js`, attach `pointermove` to the segments' group, or ignore pointer y beyond `barHeight`, so the time-axis strip doesn't open the popup.
  **Done note**: `#hover` returns when the pointer's y is outside `[0, 32)`. Before the change, the
  probe opened the popup at y = 44, on the axis. After it, it opened at y = 16 and not at y = 44.
- [X] T050 Show the `TimelineAssetsTests.NoPageReferencesD3DirectlyAsync` guard failing for the right reason per Constitution V (partial): temporarily add `<script src="@Assets["lib/d3/d3.min.js"]"></script>` to `src/JoakimSoftware.TcxTool.Web/Components/App.razor`, see the three cases fail, revert with an edit, `touch` the file, and rerun. Then run quality gates 1–3, and the Compose gate if T047–T049 changed anything.
  **Done note**: with the temporary `<script>`, all three `NoPageReferencesD3DirectlyAsync` cases
  (`/`, `/convert`, `/timeline`) failed with "only js/timeline.js loads D3". The other 7 asset tests
  passed. After the revert and `touch`, `App.razor` had its original SHA-1 and the 10 passed. Gates 1–3
  passed (650 tests), and the Compose gate exited 0 with 650 tests. `data/` and `tcxtool-state.json`
  were unchanged.
