---

description: "Task list for feature 008-remove-histograms-page"
---

# Tasks: Remove the Histograms Page

**Input**: Design documents from `specs/008-remove-histograms-page/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/pages.md, quickstart.md

**Tests**: Included. The constitution requires test-first (Principle V, NON-NEGOTIABLE).

- Every test task comes before the code it covers, and must be run and seen failing first.
- A test that guards existing behavior passes from the start (a guard). Show it failing with a
  temporary break. Revert the break in the same task with an edit and a `touch`, not by restoring a
  backup (CLAUDE.md, lessons from 002 and 005).
- **Moved coverage**: a test moved from a deleted file keeps its assertions, and passes from the
  start, like feature 005's tests "carried over from PlaceholderPageTests" (research.md R3).
- **No new test names the removed page** (research.md R2). After the feature, no file under
  `src/` or `tests/` contains "histogram", apart from vendored files under `wwwroot/lib/` (FR-006).

**Organization**:
- Setup records the baseline. There is no foundational phase.
- US1 removes the page and its tests.
- US2 removes what is left in the guidance and checks for leftovers. It needs US1 done first.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task belongs to (US1, US2)
- All paths are relative to `tcxtool/`. Run every command from `tcxtool/`. The git root is the
  parent directory.

## Path Conventions

These come from plan.md → Project Structure:

- Web: `src/JoakimSoftware.TcxTool.Web/{Components/Layout,Components/Pages,Program.cs}`
- Tests: `tests/JoakimSoftware.TcxTool.Tests/Web/`

**Conventions for every task**:

- **Code rules**:
  - one public type per file, file-scoped namespaces
  - every `async Task` test name ends in `Async` (IDE1006)
  - no relaxed settings or suppressed rules
- **Test isolation**:
  - HTTP tests get their host only from `Support/TcxToolWebFactory`.
  - No test reads `data/`, `specs/`, `.gitignore`, or the real `tcxtool-state.json`.
- **Gates**: check `dotnet format`'s exit code directly, never through a pipe.

---

## Phase 1: Setup

**Purpose**: a green baseline to compare against.

- [X] T001 Record the baseline:
  **Done note**: build with 0 warnings, format exit 0, and 650 tests passed. The checksums of the 7 data and state files are in the scratchpad.
  - Run quality gates 1–3 (CLAUDE.md, "Quality gates"), and record the test count. It is 650 after
    feature 007.
  - Record `shasum` of `tcxtool-state.json`, `data/csv/*`, and `data/tcx/*` in the scratchpad, to
    compare in T012.

**Checkpoint**: the build, format, and tests are green before any change.

---

## Phase 2: User Story 1 - The app no longer offers a Histograms page (Priority: P1) 🎯 MVP

**Goal**:
- The navbar has 6 links.
- No page is routed at the removed address, which now gets the standard 404 Not Found page.
- Every other page is unchanged (FR-001–FR-005, FR-008).

**Independent Test**: the new route-set and navbar tests pass, and an unknown address gets 404 with
the Not Found page in the layout (contracts/pages.md).

### Tests for User Story 1 (write first, see them fail) ⚠️

- [X] T002 [P] [US1] Add `tests/JoakimSoftware.TcxTool.Tests/Web/PageRoutesTests.cs` (contracts/pages.md "Routes" and "Unknown addresses", research.md R2):
  **Done note**: it uses `Microsoft.AspNetCore.Components.RouteAttribute` (analysis U1). The route-set test failed, listing 9 templates including the removed one. The unknown-address test passed.
  - **Route set**: `TheAppRoutesExactlyItsPages` reflects over `typeof(Program).Assembly.GetTypes()`,
    collects the `Template` of every `RouteAttribute` (`inherit: false`), and checks
    `BeEquivalentTo` the 8 templates `/`, `/convert`, `/table`, `/states`, `/timeline`, `/about`,
    `/not-found`, and `/Error`.
  - **Unknown address**: `AnUnknownAddressGetsTheNotFoundPageInTheLayoutAsync` uses
    `TcxToolWebFactory` to `GET /no-such-page`, and checks status 404 and a body containing
    `>Not Found</h1>` and `navbar`. Dispose the factory as the other endpoint tests do
    (`IDisposable`).
  - **Naming**: don't name the removed page in the file.
  - **Run it**: the route-set test fails, because the actual set also has the removed route. The
    unknown-address test passes. That is a guard, shown failing in T005.
- [X] T003 [P] [US1] In `tests/JoakimSoftware.TcxTool.Tests/Web/MainLayoutTests.cs`, change `NavbarLinksOnlyToPagesThatExist` to expect exactly `("Home", "")`, `("Convert", "convert")`, `("Table", "table")`, `("States", "states")`, `("Timeline", "timeline")`, and `("About", "about")` (contracts/pages.md "Navbar"). Run it, and see it fail on the extra link.
  **Done note**: it failed on the extra link.
- [X] T004 [P] [US1] Move the Table layout check (research.md R3): add `ThePageIsServedInsideTheLayoutAsync` to `tests/JoakimSoftware.TcxTool.Tests/Web/TablePageEndpointTests.cs`.
  **Done note**: it passed from the start. Per analysis C1, it was also shown failing in T005.
  - It does `GET /table` on a host with no state file.
  - It checks status 200, and a body containing `<title>Table</title>`, `>Table</h1>`, and
    `navbar`. Those are the assertions of `PlaceholderPageEndpointTests`' `/table` case.
  - Run it: it passes from the start (moved coverage).
- [X] T005 [US1] Show the unknown-address guard failing (Constitution V):
  **Done note**: both breaks were applied together.
  - **The re-execute line removed from `Program.cs`**: the guard failed with `Expected body "" to
    contain ">Not Found</h1>"`.
  - **Analysis C1, `<PageTitle>Table</PageTitle>` removed from `TablePage.razor`**: T004's test
    failed with `to contain "<title>Table</title>"`.
  - **Restored**: both lines went back with edits and the files were touched. `shasum -c` matched
    the pre-break checksums, and both tests passed.
  - **Break**: temporarily delete the `app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);`
    line from `src/JoakimSoftware.TcxTool.Web/Program.cs`.
  - **Run**: run `AnUnknownAddressGetsTheNotFoundPageInTheLayoutAsync`, and see it fail because the
    404 body has no Not Found heading.
  - **Restore**: put the line back with an edit, `touch` the file, and check its `shasum` matches
    the one before the break.
  - **Rerun**: run the test again and see it pass.

### Implementation for User Story 1

- [X] T006 [US1] Remove the page (data-model.md "Removed" and "Changed"):
  **Done note**: deleted the page and the `NavLink`. The other links are unchanged and in order.
  - Delete `src/JoakimSoftware.TcxTool.Web/Components/Pages/HistogramsPage.razor`.
  - Delete the Histograms `NavLink` line from
    `src/JoakimSoftware.TcxTool.Web/Components/Layout/MainLayout.razor`.
  - Leave the other links in their order.
- [X] T007 [US1] Delete the tests that existed only for the page, or whose other case moved in T004 (data-model.md "Removed", research.md R3):
  **Done note**: deleted both files. `git rm` staged the deletions, so they were unstaged again with `git restore --staged`. The index is unchanged, and nothing is staged.
  - Delete `tests/JoakimSoftware.TcxTool.Tests/Web/PlaceholderPageTests.cs`. It references
    `HistogramsPage`, so the test project doesn't build until it's gone.
  - Delete `tests/JoakimSoftware.TcxTool.Tests/Web/PlaceholderPageEndpointTests.cs`.
  - Leave the "Carried over from PlaceholderPageTests" comment in `TablePageTests.cs`. It records
    where those tests came from.
- [X] T008 [US1] Run gates 1–3:
  **Done note**: build with 0 warnings, format exit 0, and 649 tests passed (650 − 4 + 3), as predicted.
  - Everything must be green, including T002 and T003.
  - The test count is the T001 count, minus the 4 removed cases (2 bUnit theory cases and 2
    endpoint cases), plus the 3 added in T002 and T004. From 650 that is 649. Explain any other
    difference in a done note.

**Checkpoint**: US1 is complete. The app has no Histograms page, and the removed address gets the
standard Not Found response.

---

## Phase 3: User Story 2 - Nothing is left behind for Histograms (Priority: P2)

**Goal**: no code, test, or current guidance mentions the page. History still says feature 003
added it, and 008 removed it (FR-006, FR-007, FR-009).

**Independent Test**: quickstart.md step 2 prints nothing for `src/` and `tests/`, and only the
history rows for `CLAUDE.md`.

**Depends on**: US1. The search can't come up empty while the page exists.

- [X] T009 [US2] Update `CLAUDE.md` (research.md R4):
  **Done note**: replaced the `Analysis/` sentence, and kept the Timeline sentence. Added the 008 history row. The 003 row is unchanged.
  - In the `Analysis/` notes of "Feature 005 added two Web namespaces", replace "Histograms should
    use it, never open the CSV itself." with a sentence that analysis pages get the current CSV's
    rows from it and never open the CSV themselves.
  - Keep the sentence about Timeline and `CurrentStatesLoader`.
  - Add a history row: `specs/008-remove-histograms-page` | Complete | Removed the feature 003
    Histograms placeholder page, its navbar link, and its tests. The address now gets the Not Found
    page.
  - Leave the 003 row and `specs/001`–`specs/007` unchanged.
- [X] T010 [US2] Check for leftovers (FR-004, FR-006, research.md R5):
  **Done note**: the first search found one leftover, the `PageRoutesTests` doc comment, which cited
  the feature folder by name. It now says "feature 008 contracts/pages.md", and the search prints
  nothing. `CLAUDE.md` matches only the 003 and 008 rows. `TablePage.razor.cs` still uses
  `CurrentActivityLoader`, and `NotFound.razor` is still there. `git diff` and `git status` show no
  change under `wwwroot/lib/` or `specs/001`–`specs/007`.
  - Run quickstart.md step 2. The `src/` and `tests/` search must print nothing. The `CLAUDE.md`
    search must print only the 003 and 008 history rows.
  - Confirm with `grep` that `CurrentActivityLoader` is still used by
    `Components/Pages/TablePage.razor.cs`, that `NotFound.razor` is still there, and that
    `wwwroot/lib/d3/d3.min.js` is unchanged (`git diff --stat` shows nothing for it).
  - Confirm with `git status` that nothing under `specs/001`–`specs/007` changed in this feature.

**Checkpoint**: US2 is complete. Nothing about the page is left outside the history.

---

## Phase 4: Polish & Cross-Cutting Concerns

- [X] T011 Run quickstart.md step 3 on a scratch host (`TcxTool__CsvOutputDirectory` and `TcxTool__StateFilePath` in the scratchpad, a spare port):
  **Done note**:
  - **Removed address**: `/histograms`, `/Histograms`, `/histograms/`, and `/no-such-page` each
    gave `404 1`.
  - **Navbar**: Home, Convert, Table, States, Timeline, About.
  - **Active link (analysis G1)**: each of the 6 pages returned 200, with exactly its own
    `nav-link active`.
  - **Clean-up**: the host was stopped, and the scratch folder deleted.
  - **Left to the user**: step 4, how the pages look in a browser.
  - `/histograms`, `/Histograms`, `/histograms/`, and `/no-such-page` each give `404 1`.
  - The navbar lists the 6 links in order.
  - Stop the host, and delete the scratch folder.
  - Leave step 4, the browser look, to the user, and say so in the report.
- [X] T012 Final gates (Constitution VII):
  **Done note**:
  - **Gates**: build with 0 warnings, format exit 0, and 649 host tests. The Compose gate exited 0
    with 649 tests.
  - **Data**: the 7 data and state checksums match T001.
  - **Lessons**: added "Lessons from feature 008" to `CLAUDE.md`: the route-set test, citing a
    feature's documents without its folder name, and `git rm` staging. They don't name the page,
    so quickstart step 2 still matches only the history rows (analysis I1).
  - Run all 4 quality gates, including the Compose gate (`docker info` first). It must exit 0
    with the T008 test count.
  - Compare `tcxtool-state.json`, `data/csv/*`, and `data/tcx/*` against the T001 checksums.
  - Add a "Lessons from feature 008" section to `CLAUDE.md` only if the work taught something not
    already recorded. For example: test a removal through the route set, so that no test has to
    name the removed page.

---

## Dependencies & Execution Order

- **Setup (T001)**: first.
- **US1**:
  - T002, T003, and T004 are in different files, so they can run in parallel.
  - T005 needs T002.
  - T006 needs T002–T005: the tests must fail first, and the guard must be shown failing.
  - T007 goes with T006. The test project doesn't build between them.
  - T008 needs T006 and T007.
- **US2**: needs US1. T009 and T010 run in order.
- **Polish**: T011 and T012 need US2. T012 is last.

### Parallel Example: User Story 1

```text
T002  Web/PageRoutesTests.cs        (new file)
T003  Web/MainLayoutTests.cs        (one expectation)
T004  Web/TablePageEndpointTests.cs (one new fact)
```

## Implementation Strategy

- **MVP**: US1 (T001–T008) is the user-visible change. The app no longer offers the page, and every
  gate but Compose is green.
- **Then**: US2 (T009–T010) cleans up the guidance, and T011–T012 validate the feature end to end,
  including the Compose gate.
- **Size**: 12 tasks. The whole feature is one small change, so it is best done in one pass,
  without stopping after the MVP.
