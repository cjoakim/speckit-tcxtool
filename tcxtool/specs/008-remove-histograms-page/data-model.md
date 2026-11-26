# Data Model: Remove the Histograms Page

**Feature**: `specs/008-remove-histograms-page/spec.md` | **Research**: [research.md](./research.md)

The feature has no data entities. No record, CSV format, state file field, or service changes
(spec, Key Entities). This file lists the code units the feature removes, changes, or adds, so the
tasks can name them.

## Removed

| Unit | Kind | Why it can go |
|------|------|---------------|
| `Components/Pages/HistogramsPage.razor` (`HistogramsPage`) | routable component, `/histograms` | the page itself (FR-001) |
| `tests/…/Web/PlaceholderPageTests.cs` | bUnit tests | its only page is `HistogramsPage` (R3) |
| `tests/…/Web/PlaceholderPageEndpointTests.cs` | HTTP tests | its `/table` case moves to `TablePageEndpointTests` (R3) |

## Changed

| Unit | Change |
|------|--------|
| `Components/Layout/MainLayout.razor` | drops the Histograms `NavLink`. The navbar is Home, Convert, Table, States, Timeline, About (FR-003) |
| `tests/…/Web/MainLayoutTests.cs` | `NavbarLinksOnlyToPagesThatExist` expects those 6 links |
| `tests/…/Web/TablePageEndpointTests.cs` | gains `ThePageIsServedInsideTheLayoutAsync`: 200, `<title>Table</title>`, `>Table</h1>`, `navbar` |
| `CLAUDE.md` | the `Analysis/` note no longer names Histograms, and the history table has an 008 row (R4) |

## Added

| Unit | What it checks |
|------|----------------|
| `tests/…/Web/PageRoutesTests.cs` | the set of route templates in the Web assembly is exactly the 8 in [contracts/pages.md](./contracts/pages.md) |
| an unknown-address test, in `PageRoutesTests` | `GET /no-such-page` gives 404, `>Not Found</h1>`, and `navbar` |

## Kept (FR-004)

| Unit | Still used by |
|------|---------------|
| `Analysis/CurrentActivityLoader`, `CurrentActivity`, `CurrentActivityMessages` | `TablePage` |
| `Components/Pages/NotFound.razor`, `UseStatusCodePagesWithReExecute("/not-found")` | every unknown address, now including `/histograms` |
| `wwwroot/lib/d3/d3.min.js` | the Timeline page. It contains "histogram" in D3's own code, unchanged |
