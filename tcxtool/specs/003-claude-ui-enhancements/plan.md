# Implementation Plan: Claude UI Enhancements

**Branch**: `003-claude-ui-enhancements` (feature id; work is on git branch `main`) | **Date**: 2026-11-24 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/003-claude-ui-enhancements/spec.md`. The plan was
reverse-engineered from the delivered code.

## Summary

Give TcxTool PLog's look by vendoring PLog's Bootstrap 5.3.0 CSS and JS into
`wwwroot/lib/bootstrap/` and linking them through `@Assets` in `App.razor`. Then:

- Rebuild `MainLayout` as PLog's shell: a dark navbar, `main.container.my-4`, and a light footer.
- Restyle Home, Convert, the conversion result, and Not Found with stock Bootstrap classes, keeping
  every element the 001 and 002 contracts require.
- Add three static placeholder pages, Table, Histograms, and Timeline, and link all five pages from
  the navbar with `NavLink`, so the current page is highlighted.

Make the vendored files committable with a scoped `.gitignore` negation, and record the styling
convention in `CLAUDE.md`. All of it was done test-first. Decisions are in
[research.md](./research.md).

## Technical Context

**Language/Version**: C# with SDK 11.0.100, `net11.0` (unchanged)

**Primary Dependencies**: ASP.NET Core / Blazor Web App (shared framework). Bootstrap 5.3.0 is a
vendored static asset, not a package.

**Storage**: N/A

**Testing**: xUnit 2.9.3, bUnit 2.15.2, FluentAssertions 7.2.2, `Microsoft.AspNetCore.Mvc.Testing`
11.0.0 (all unchanged). No new packages.

**Target Platform**: macOS host (`dotnet run`, port 5169) and the Linux `aspnet:11.0` container
(port 8080)

**Project Type**: Web application (unchanged three-project solution)

**Performance Goals**: no regression. Bootstrap adds about 313 KB on the first load, then the
fingerprinted URLs are cached.

**Constraints**:
- no CDN or network for assets
- PLog read-only
- static SSR only
- existing page contracts preserved

**Scale/Scope**: 1 layout, 4 restyled components, 3 new pages, 2 vendored files, and 21 new tests
(158 → 179).

No item is NEEDS CLARIFICATION.

## Constitution Check

| Principle / Section | Requirement | How this feature meets it | Result |
|---|---|---|---|
| I. Blazor Web App, static SSR default | interactivity only per component, when needed | No `@rendermode` added. `NavLink` works in static SSR. | ✅ |
| II. Style by toolchain | format clean, idioms, small components | Markup-only components. `dotnet format --verify-no-changes` is clean. Test methods follow the `Async` rule. | ✅ |
| III. Parser library, UI on top | parser untouched by UI | No parser changes | ✅ |
| IV. BCL XML only | — | No XML parsing added | ✅ |
| V. Test-first | tests written first and seen failing | Round 1: 12 tests failed (158 passed) before any markup or asset change. Round 2: the new tests failed to compile, and the nav test was updated, before the pages existed. | ✅ |
| VI. Docker multi-stage, non-root | — | Unchanged. `wwwroot/lib` is published into the runtime image. | ✅ |
| VII. Compose gate | gate exit 0, offline | Passed after each round (170, then 179 tests) under `network_mode: none` | ✅ |
| Technology Constraints | 3 projects, no extra stack | No project, package, or JS build tool added (FR-013) | ✅ |
| Quality Gates 1–4 | all pass | Passed (quickstart.md scenario 1) | ✅ |

Result: **PASS**. The one process deviation is recorded under Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/003-claude-ui-enhancements/
├── spec.md
├── plan.md                    # This file
├── research.md                # R1–R8
├── data-model.md              # UI structure and assets (no domain entities)
├── quickstart.md              # Scenarios 1–6
├── contracts/
│   ├── ui-shell.md            # assets, layout, navigation
│   └── pages.md               # per-page markup
├── checklists/
│   └── requirements.md
└── tasks.md                   # all tasks complete
```

### Source code touched (`tcxtool/`)

```text
.gitignore                                          # + !src/JoakimSoftware.TcxTool.Web/wwwroot/lib/
CLAUDE.md                                           # + "UI styling" convention, feature history row
src/JoakimSoftware.TcxTool.Web/
├── wwwroot/lib/bootstrap/css/bootstrap.min.css     # new, from PLog (5.3.0)
├── wwwroot/lib/bootstrap/js/bootstrap.bundle.min.js # new, from PLog (5.3.0)
└── Components/
    ├── App.razor                                   # links the CSS and JS via @Assets
    ├── Layout/MainLayout.razor                     # PLog shell + NavLinks
    ├── Conversion/ConversionResultView.razor       # alert-success / alert-danger
    └── Pages/
        ├── Home.razor                              # h1.h3, Convert card
        ├── ConvertPage.razor                       # card, form-label/-control/-text, btn-primary
        ├── NotFound.razor                          # h1.h3 (was h3)
        ├── TablePage.razor                         # new
        ├── HistogramsPage.razor                    # new
        └── TimelinePage.razor                      # new
tests/JoakimSoftware.TcxTool.Tests/Web/
├── MainLayoutTests.cs                              # new (4)
├── BootstrapAssetsTests.cs                         # new (4)
├── PlaceholderPageTests.cs                         # new (6)
├── PlaceholderPageEndpointTests.cs                 # new (3)
├── HomePageTests.cs                                # +1
├── ConvertPageTests.cs                             # +1
└── ConversionResultViewTests.cs                    # +2
```

**Structure Decision**: the feature stays inside the existing Web project and test project. Tests
go in the existing `Web/` folder.

## Implementation Order (as delivered)

1. **Research**: read PLog's `base.html`, page templates, and `static/` (read-only). Confirm the
   PLog repository's git status is clean.
2. **Round 1, tests first**: write `MainLayoutTests` and `BootstrapAssetsTests`, and add the
   Bootstrap-class assertions to the Home, Convert, and result-view tests. Run them and see 12
   failures.
3. **Round 1, implementation**: copy the assets, link them in `App.razor`, rewrite `MainLayout`,
   restyle the pages and the result view, then run gates 1–4 and check the Release container
   serves the fingerprinted CSS. Record the convention in `CLAUDE.md`.
4. **Round 2, tests first**: extend the navbar assertion, write `PlaceholderPageTests` and
   `PlaceholderPageEndpointTests`, and see them fail (compile errors, because the pages are
   missing).
5. **Round 2, implementation**: add the three pages and the three `NavLink`s, then run gates 1–4.
6. **Reverse-engineering**: write this spec set. Find and fix the root `lib/` ignore rule
   (research R7). Add the feature history row to `CLAUDE.md`.

## Complexity Tracking

| Deviation | Why it happened | Mitigation |
|-----------|-----------------|------------|
| Code was written before spec, plan, and tasks. `/speckit-specify` through `/speckit-implement` were not run. | The maintainer asked for the change directly. `CLAUDE.md` allows application code outside `/speckit-implement` when asked. | Test-first (Principle V) and all four gates were still followed. This reverse-engineered set records the result, and `/speckit-analyze` or `/speckit-converge` can be run against it. |
| `specs/002-tcx-to-csv/contracts/convert-page.md` shows the submit button without a class. | Feature 002's contract predates the styling. | `contracts/pages.md` states the addition. The 002 artifacts are left as historical records. |
