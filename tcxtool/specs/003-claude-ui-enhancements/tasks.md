---

description: "Task list for feature 003-claude-ui-enhancements (reverse-engineered; all complete)"
---

# Tasks: Claude UI Enhancements

**Input**: Design documents from `specs/003-claude-ui-enhancements/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included, and written first (Principle V). Each test task was run and seen failing
before the task that satisfies it.

**Reverse-engineered**: these tasks record work already delivered, in the order it was done. All
are complete. New work for this feature should be appended by `/speckit-converge`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 PLog look, US2 restyled pages, US3 placeholder pages and navigation
- Paths are relative to `tcxtool/`. Run commands from `tcxtool/`. The git root is the parent
  directory.

---

## Phase 1: Setup

- [X] T001 Read PLog's `templates/base.html`, `home.html`, `activity_form.html`, `activity_list.html`, `garmin.html`, and `static/` in `/Users/cjoakim/github/cj-plog/python` (read-only), and record the shell markup and Bootstrap version in research.md (5.3.0).
- [X] T002 Confirm `git -C /Users/cjoakim/github/cj-plog status --short` is empty before starting (FR-012).

---

## Phase 2: User Story 1 — The app looks like PLog (P1) 🎯 MVP

**Goal**: Bootstrap served locally, and every page inside PLog's shell.

**Independent Test**: quickstart.md scenarios 2 (step 1), 3, and 4.

### Tests for User Story 1 (write first, see them fail)

- [X] T003 [P] [US1] Write `tests/JoakimSoftware.TcxTool.Tests/Web/MainLayoutTests.cs` (bUnit, contracts/ui-shell.md "Layout"): `HasADarkNavbarWithTheBrandLinkingHome`, `NavbarLinksOnlyToPagesThatExist` (ordered `(text, href)` list, initially Home and Convert), `RendersThePageBodyInsideAContainer`, `HasAFooter`.
- [X] T004 [P] [US1] Write `tests/JoakimSoftware.TcxTool.Tests/Web/BootstrapAssetsTests.cs` (`WebApplicationFactory<Program>`, contracts/ui-shell.md "Assets"): `PageLinksTheLocalBootstrapStylesheetAsync` for `/` and `/convert`, `StylesheetIsServedLocallyAsync`, `ScriptBundleIsServedLocallyAsync`. Match the fingerprinted URL with `[GeneratedRegex]`, and never hard-code the hash (research R8).

### Implementation for User Story 1

- [X] T005 [US1] Copy PLog's `static/bootstrap/css/bootstrap.min.css` and `static/bootstrap/js/bootstrap.bundle.min.js`, unmodified, to `src/JoakimSoftware.TcxTool.Web/wwwroot/lib/bootstrap/css/` and `.../js/` (research R1).
- [X] T006 [US1] Link the assets in `src/JoakimSoftware.TcxTool.Web/Components/App.razor` via `@Assets[...]`: the CSS before `app.css`, and the JS before `_framework/blazor.web.js` (research R2, R3).
- [X] T007 [US1] Rewrite `src/JoakimSoftware.TcxTool.Web/Components/Layout/MainLayout.razor` to the PLog shell: the navbar with the "TcxTool" brand and the `NavLink`s Home (`NavLinkMatch.All`) and Convert, `main.container.my-4`, and the footer. Keep `#blazor-error-ui` (research R4).

---

## Phase 3: User Story 2 — Existing pages use the new styles (P2)

**Goal**: Home, Convert, the result view, and Not Found use stock Bootstrap components, with the
001 and 002 contracts intact.

**Independent Test**: quickstart.md scenario 5, plus the unchanged 001 and 002 page tests.

### Tests for User Story 2 (write first, see them fail)

- [X] T008 [P] [US2] Add `ConvertLinkIsAPrimaryButtonOnACard` to `tests/JoakimSoftware.TcxTool.Tests/Web/HomePageTests.cs`.
- [X] T009 [P] [US2] Add `UsesBootstrapFormStyles` to `tests/JoakimSoftware.TcxTool.Tests/Web/ConvertPageTests.cs` (`form-label`, `form-control`, `btn btn-primary`).
- [X] T010 [P] [US2] Add `ASuccessIsABootstrapSuccessAlert` and `AFailureIsABootstrapDangerAlert` to `tests/JoakimSoftware.TcxTool.Tests/Web/ConversionResultViewTests.cs`.
- [X] T011 Run `dotnet test`. **Observed**: 12 failed (T003, T004, and T008–T010), 158 passed.

### Implementation for User Story 2

- [X] T012 [P] [US2] Restyle `Components/Pages/Home.razor` per contracts/pages.md "Home": `h1.h3.mb-0`, `p.text-muted`, and the Convert card.
- [X] T013 [P] [US2] Restyle `Components/Pages/ConvertPage.razor` per contracts/pages.md "Convert". Keep every contract-002 attribute.
- [X] T014 [P] [US2] Add the alert classes in `Components/Conversion/ConversionResultView.razor` per contracts/pages.md "Conversion result". Keep the roles and text.
- [X] T015 [P] [US2] Change `Components/Pages/NotFound.razor` from `h3` to `h1.h3.mb-3`, plus `p.text-muted`.
- [X] T016 Run quality gates 1–4. **Observed**: build and format clean, 170/170 tests on the host and in the Compose gate, exit 0.
- [X] T017 Start the `app` container and confirm `/convert` links `lib/bootstrap/css/bootstrap.min.<hash>.css` and `lib/bootstrap/js/bootstrap.bundle.min.<hash>.js`, and the CSS returns 200 `text/css`.
- [X] T018 Add the "UI styling" convention to `CLAUDE.md` › "Code conventions established so far" (FR-014).

**Checkpoint**: US1 and US2 delivered.

---

## Phase 4: User Story 3 — Navigate to the analysis pages (P3)

**Goal**: Table, Histograms, and Timeline pages, linked from the navbar.

**Independent Test**: quickstart.md scenario 2, steps 2 and 3.

### Tests for User Story 3 (write first, see them fail)

- [X] T019 [US3] Extend `MainLayoutTests.NavbarLinksOnlyToPagesThatExist` to expect `("Home", "")`, `("Convert", "convert")`, `("Table", "table")`, `("Histograms", "histograms")`, `("Timeline", "timeline")`.
- [X] T020 [P] [US3] Write `tests/JoakimSoftware.TcxTool.Tests/Web/PlaceholderPageTests.cs` (bUnit, `DynamicComponent`, `TheoryData<Type, string>`): `PageHasOneHeadingWithItsName` and `PageRoutesToItsLowercaseName` for each page.
- [X] T021 [P] [US3] Write `tests/JoakimSoftware.TcxTool.Tests/Web/PlaceholderPageEndpointTests.cs` (`WebApplicationFactory`): `PageIsServedInsideTheLayoutAsync` for `/table`, `/histograms`, `/timeline` (200, `<title>`, `h1`, navbar).
- [X] T022 Run `dotnet build`. **Observed**: CS0246 for `TablePage`, `HistogramsPage`, and `TimelinePage` (red).

### Implementation for User Story 3

- [X] T023 [P] [US3] Create `Components/Pages/TablePage.razor`, `HistogramsPage.razor`, and `TimelinePage.razor` per contracts/pages.md "Placeholder pages" (research R6).
- [X] T024 [US3] Add the Table, Histograms, and Timeline `NavLink`s after Convert in `Components/Layout/MainLayout.razor`.
- [X] T025 Run quality gates 1–4. **Observed**: build and format clean, 179/179 tests on the host and in the Compose gate, exit 0.

**Checkpoint**: all three stories delivered.

---

## Phase 5: Polish & Cross-Cutting

- [X] T026 Write the reverse-engineered spec set in `specs/003-claude-ui-enhancements/`.
- [X] T027 Add `!src/JoakimSoftware.TcxTool.Web/wwwroot/lib/` to `tcxtool/.gitignore`, then confirm with `git check-ignore` that the Bootstrap files are no longer ignored (FR-011, research R7).
- [X] T028 [P] Add the 003 row to `CLAUDE.md` › "Feature history".
- [X] T029 Confirm `git -C /Users/cjoakim/github/cj-plog status --short` is still empty (SC-005).

---

## Dependencies & Execution Order

- T001–T002 come before everything else.
- US1's tests (T003, T004) come before T005–T007.
- US2's tests (T008–T010) and the observed red run (T011) come before T012–T015. US2 uses US1's
  stylesheet.
- US3 (T019–T025) depends on US1's layout. It is independent of US2.
- Phase 5 comes after all stories.

## Notes

- Never edit the PLog tree. Copying from it is reading.
- The navbar link table in contracts/ui-shell.md, `MainLayoutTests`, and `MainLayout.razor` must
  change together.
- Test methods that return `Task` end in `Async` (IDE1006).
