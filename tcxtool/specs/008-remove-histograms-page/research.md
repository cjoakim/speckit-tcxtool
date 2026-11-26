# Research: Remove the Histograms Page

**Feature**: `specs/008-remove-histograms-page/spec.md` | **Plan**: [plan.md](./plan.md)

The Technical Context has no unknowns. These are the decisions the removal needs, checked against
the code and a running host on 2026-11-26.

## Inventory (what "related to it" is)

A case-insensitive search for "histogram" in `src/`, `tests/`, `CLAUDE.md`, the root `README.md`,
and `docs/` (excluding `bin/` and `obj/`) found:

| Where | What | Only for Histograms? |
|-------|------|----------------------|
| `src/JoakimSoftware.TcxTool.Web/Components/Pages/HistogramsPage.razor` | the page: `@page "/histograms"`, a `PageTitle`, and an `h1`. No code-behind, no `.razor.css` | yes |
| `src/JoakimSoftware.TcxTool.Web/Components/Layout/MainLayout.razor` | the `NavLink` to `histograms` | yes, that one line |
| `tests/…/Web/PlaceholderPageTests.cs` | bUnit heading and route tests. Its only `Pages` entry is `HistogramsPage` | yes, the whole file |
| `tests/…/Web/PlaceholderPageEndpointTests.cs` | an HTTP theory over `/table` and `/histograms`: status 200, `<title>`, `h1`, and `navbar` | no, it also covers `/table` |
| `tests/…/Web/MainLayoutTests.cs` | the expected navbar link list | no, one tuple in the list |
| `CLAUDE.md` | the feature 003 history row, and "Histograms should use it" in the `Analysis/` notes | the second is current guidance. The first is history |
| `src/JoakimSoftware.TcxTool.Web/wwwroot/lib/d3/d3.min.js` | D3's own `bin`/`histogram` code | no, it's vendored third-party code used by Timeline |

Nothing else matched. There is no Histograms style in `app.css`, no service, no state, and no
fixture. `../README.md` and `docs/` don't mention it, apart from `docs/spec-008-draft.md`.

## R1. What `/histograms` returns after removal

**Decision**: nothing new. With no component routed there, the request goes through the existing
pipeline: no endpoint matches, and `UseStatusCodePagesWithReExecute("/not-found")` renders
`Pages/NotFound.razor` inside `MainLayout`, with status 404.

**Evidence**: on a scratch host, `/no-such-page` returned 404 with `>Not Found</h1>` and the navbar.
`/histograms`, `/Histograms`, and `/histograms/` all returned 200 with the Histograms page, so all
three will become the same 404.

**Alternatives considered**:
- *A redirect to Home, or a "this page was removed" page*: the spec says a removed address behaves
  like any unknown one (FR-002, Assumptions).

## R2. Test-first for a removal (Constitution V)

**Decision**: write two tests that fail while the page exists, then remove it:
- **The navbar list**: `MainLayoutTests.NavbarLinksOnlyToPagesThatExist` expects the 6 remaining
  links. It fails while the Histograms link is there.
- **The route list**: a new `Web/PageRoutesTests.cs` reflects over every component in the Web
  assembly with a `RouteAttribute`, and expects exactly `/`, `/about`, `/convert`, `/Error`,
  `/not-found`, `/states`, `/table`, and `/timeline`, ignoring order. It fails while
  `/histograms` is routed. It also stops a later feature adding a page without updating the list,
  which is the same rule as the navbar test.

One guard test covers FR-002:
- **Unknown addresses**: a new endpoint test checks that an address no page has (`/no-such-page`)
  gets 404 with the "Not Found" heading inside the layout. This already passes, so it is shown
  failing with a temporary break, removing the `UseStatusCodePagesWithReExecute` line from
  `Program.cs`, reverted with an edit and a `touch` (CLAUDE.md, lessons from 002 and 005).
- **Together with the route list**, it shows that `/histograms` gets the standard Not Found
  response.

**Rationale**: neither test names the removed page, so after the feature no test contains the
word, as FR-006 requires.

**Alternatives considered**:
- *An endpoint test that requests `/histograms` and expects 404*: the test would itself contain the
  word, and FR-006 forbids that.
- *An automated "no file contains histogram" test*: the same problem, unless the word is built from
  pieces to hide it, which is obscure. FR-006 is checked with a search in the quickstart and
  in the final task instead.

## R3. The tests that covered Histograms together with Table

**Decision**:
- Delete `PlaceholderPageTests.cs`. With Histograms gone its `Pages` data would be empty, and an
  empty `MemberData` theory fails in xUnit. `TablePageTests` already has its own route and heading
  tests ("Carried over from PlaceholderPageTests"). That comment records where they came from, so
  it stays.
- Delete `PlaceholderPageEndpointTests.cs`, and move its `/table` case into
  `TablePageEndpointTests` as a new `[Fact]` with the same assertions (200, `<title>Table</title>`,
  `>Table</h1>`, `navbar`). The Table endpoint tests don't check `navbar` today, so this keeps the
  coverage (FR-005), and no existing test changes what it checks (SC-004).

**Alternatives considered**:
- *Keep `PlaceholderPageEndpointTests` with one case*: after this feature there are no placeholder
  pages, so the name would mislead.
- *Add `navbar` to an existing Table endpoint test*: that changes what an existing test checks.

## R4. Developer guidance and history

**Decision**:
- In `CLAUDE.md`'s `Analysis/` notes, change "Histograms should use it, never open the CSV itself"
  to say that analysis pages use `CurrentActivityLoader` and never open the CSV themselves.
- Add a feature 008 row to the history table that records the removal.
- Leave the 003 row and `specs/001`–`specs/007` unchanged (FR-009).

**Rationale**: the 003 row is history. It says what 003 added, and the 008 row says what was
removed, so the table stays true without rewriting a past entry (FR-007, US2/AC2).

## R5. What stays

**Decision**: keep `CurrentActivityLoader`, `CurrentActivity`, and `CurrentActivityMessages`
(used by `TablePage`), `NotFound.razor`, and the vendored D3 file. Search for each type that
Histograms is said to use, to confirm that another page uses it (FR-004).

## R6. Build output and the Compose gate

**Decision**: after deleting a `.razor` file, run the gates as usual. The Razor source generator
drops the deleted component on the next build, and the Compose gate builds from a clean context, so
a stale `HistogramsPage` type can't hide in the test run. The route test (R2) would catch one if it
did.
