# Implementation Plan: Remove the Histograms Page

**Branch**: `008-remove-histograms-page` (feature id only; no git branch is created) | **Date**: 2026-11-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/008-remove-histograms-page/spec.md`

## Summary

Remove the feature 003 placeholder page at `/histograms`, its navbar link, and its tests. After
that, the address gets the app's existing 404 Not Found page, like any unknown address.

- **Test-first** (research.md R2): two tests fail while the page exists, the navbar list and a new
  test of the app's whole route set. A guard test for unknown addresses is shown failing with a
  temporary break. None of these tests names the removed page, so no test contains "histogram"
  afterwards (FR-006).
- **The `/table` case**: it moves out of the deleted placeholder endpoint test into
  `TablePageEndpointTests` (R3).
- **`CLAUDE.md`**: it loses the one line of current guidance about Histograms and gains an 008
  history row (R4).
- **Unchanged**: no data, CSV, or state changes.

## Technical Context

**Language/Version**: C# on .NET 11 (SDK 11.0.100, pinned by `global.json`)

**Primary Dependencies**: ASP.NET Core Blazor Web App (static SSR), Bootstrap 5.3.0 (vendored). No package changes

**Storage**: N/A. Nothing about `data/csv/` or `tcxtool-state.json` changes

**Testing**: xUnit 2.9.3, bUnit 2.15.2, FluentAssertions 7.2.2, `Microsoft.AspNetCore.Mvc.Testing` through `Support/TcxToolWebFactory`

**Target Platform**: Linux container (`aspnet:11.0`, non-root, port 8080), and macOS for development

**Project Type**: Web application: Parser library, Blazor Web App, and one test project

**Performance Goals**: N/A. Removing a static page changes no page's work

**Constraints**:
- Static SSR only.
- After the feature, no "histogram" in `src/` or `tests/`, apart from vendored files under
  `wwwroot/lib/`.
- Past feature records are not edited.

**Scale/Scope**:
- Deletes 1 component and 2 test files.
- Edits 1 layout, 2 test files, and `CLAUDE.md`.
- Adds 1 test file.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | How this plan complies | Status |
|-----------|------------------------|--------|
| I. .NET 11 Blazor Web App | Static SSR is unchanged, and no `@rendermode` is added. The navbar stays plain Bootstrap. The Not Found page is the template's existing one | Pass |
| II. Style enforced by the toolchain | No project setting changes. The new test class has its own file, and its `async` tests end in `Async`. `dotnet format` runs | Pass |
| III. Parser library, UI on top | The parser is untouched. Only a Web component and the layout change | Pass |
| IV. Built-in XML parsing only | No XML is read | Pass |
| V. Test-first | The navbar and route-set tests fail before the page is removed. The unknown-address guard is shown failing with a temporary break, reverted with an edit and `touch`. The removed tests either covered only Histograms, or their `/table` coverage moves first (R3) | Pass |
| VI. Docker multi-stage, non-root | Unchanged | Pass |
| VII. Compose is the test gate | The Compose gate runs at the end | Pass |
| Technology Constraints / Logging | No logging change | Pass |
| Governance: three projects | No project change | Pass |

**Post-design re-check** (after research.md, data-model.md, contracts/, and quickstart.md): still
Pass on every row.
- The route-set test is a new structural rule, like `NavbarLinksOnlyToPagesThatExist`, and needs
  no complexity entry.
- The FR-006 search is a quickstart and task step, not a test, so no test has to hide the word
  (R2).

## Project Structure

### Documentation (this feature)

```text
specs/008-remove-histograms-page/
├── plan.md              # this file
├── research.md          # Phase 0: inventory and decisions R1–R6
├── data-model.md        # Phase 1: no entities; units removed, changed, added, and kept
├── quickstart.md        # Phase 1: validation steps
├── contracts/
│   └── pages.md         # routes, unknown addresses, navbar
├── checklists/
│   └── requirements.md  # spec quality checklist (/speckit-specify)
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
src/JoakimSoftware.TcxTool.Web/
├── Components/
│   ├── Layout/MainLayout.razor           # changed: no Histograms NavLink
│   └── Pages/HistogramsPage.razor        # deleted
└── Program.cs                            # unchanged (temporary break only, in the guard task)

tests/JoakimSoftware.TcxTool.Tests/Web/
├── MainLayoutTests.cs                    # changed: 6 navbar links
├── PageRoutesTests.cs                    # new: route set, unknown address → 404
├── TablePageEndpointTests.cs             # changed: + served inside the layout
├── PlaceholderPageTests.cs               # deleted
└── PlaceholderPageEndpointTests.cs       # deleted

CLAUDE.md                                 # changed: Analysis/ note, 008 history row
```

**Structure Decision**: the existing three-project layout. Nothing moves between projects. The new
test file goes in `Web/` with the other endpoint and layout tests.

## Implementation Order

1. **Tests, red**:
   - Add `PageRoutesTests` (route set, and unknown address).
   - Change `MainLayoutTests` to the 6 links.
   - Add the Table layout test to `TablePageEndpointTests`.
   - Run them: the route-set and navbar tests fail. The Table layout test and the unknown-address
     test pass.
2. **Guard shown failing**: remove `UseStatusCodePagesWithReExecute` from `Program.cs` and see the
   unknown-address test fail. Restore it with an edit, `touch` it, and see it pass.
3. **Remove**:
   - Delete `HistogramsPage.razor` and its `NavLink`.
   - Delete the two placeholder test files.
   - Run the tests: all green.
4. **Guidance**: update `CLAUDE.md` (R4).
5. **Validate**: run quickstart steps 1–3, including the Compose gate and the "histogram" search.

## Complexity Tracking

No violations. Nothing to justify.
