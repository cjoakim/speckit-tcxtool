# Research: Claude UI Enhancements

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Date**: 2026-11-24

Facts below were checked on the development machine on 2026-11-24, during implementation:

- PLog (`/Users/cjoakim/github/cj-plog/python`, Django) renders every page from
  `templates/base.html`:
  - a navbar (`<nav class="navbar navbar-expand-lg navbar-dark bg-dark">`), whose `.container`
    holds a `navbar-brand` and a `navbar-nav` of `nav-link`s, with no collapse toggle
  - `<main class="container my-4">`
  - `<footer class="bg-light text-center py-3"><p>&copy; 2026 Chris Joakim</p></footer>`
- PLog's page titles are `<h3 class="mb-3">` (`mb-0` on the home page). Its pages use cards,
  `form-label` and `form-control`, `btn btn-primary` and `btn btn-outline-secondary`, and
  `table table-striped table-hover`.
- PLog vendors Bootstrap **v5.3.0** in `static/bootstrap/css/bootstrap.min.css` (232,914 bytes) and
  `static/bootstrap/js/bootstrap.bundle.min.js` (80,421 bytes). Both files keep their MIT licence
  header. PLog also loads `static/chartjs/js/chart.umd.min.js`.
- In the TcxTool Release container, `@Assets["lib/bootstrap/css/bootstrap.min.css"]` rendered as
  `lib/bootstrap/css/bootstrap.min.7y3e6zuski.css`, served with status 200 and `text/css`. The JS
  bundle rendered as `lib/bootstrap/js/bootstrap.bundle.min.y58g3z9xkb.js`.
- `git check-ignore -v` showed the repository-root `.gitignore:61: lib/` rule ignoring
  `wwwroot/lib/bootstrap/...`.

## R1. Bootstrap source and version

- **Decision**: Copy PLog's two Bootstrap 5.3.0 files, unmodified, into
  `src/JoakimSoftware.TcxTool.Web/wwwroot/lib/bootstrap/{css,js}/`. PLog is only read.
- **Rationale**: The same files give an identical rendering (FR-002). Vendoring keeps the app and
  the Compose gate's `network_mode: none` tests offline (FR-001). `lib/` is the Blazor template's
  conventional location for client libraries.
- **Alternatives considered**:
  - A CDN `<link>` was rejected. It needs network access, which breaks offline use and SC-002.
  - Downloading the latest 5.3.x was rejected for now. The rendering could drift from PLog, and it
    would need network access during implementation. Upgrading both apps together is a later,
    separate change.
  - LibMan or npm was rejected. It adds a client-side build tool (FR-013).

## R2. Linking the assets

- **Decision**: Link both files in `Components/App.razor` through `@Assets[...]`. Load the
  stylesheet before `app.css` and the JS bundle before `_framework/blazor.web.js`.
- **Rationale**: `MapStaticAssets` plus `@Assets` fingerprints the URLs and sets cache headers, so
  a later upgrade needs no hard refresh. Loading `app.css` second lets app rules override Bootstrap.
- **Alternatives considered**: A plain `href="lib/..."` would work, but it gives up the
  fingerprinting the template already uses for `app.css`.

## R3. Include the JS bundle

- **Decision**: Ship `bootstrap.bundle.min.js`, even though no current page uses a JS component.
- **Rationale**: PLog ships it, so the look and behaviour match. Bootstrap's data API binds
  through delegated `document` listeners, so it keeps working after Blazor enhanced navigation
  replaces the page DOM. Later features can use dropdowns, collapse, or dismissable alerts without
  another asset change.
- **Alternatives considered**: CSS only would save about 80 KB per first load. It was rejected to
  stay identical to PLog.

## R4. Layout and navigation

- **Decision**: `MainLayout.razor` reproduces PLog's shell markup. The brand is a plain `<a
  class="navbar-brand" href="">TcxTool</a>`. The links are `<NavLink class="nav-link">`, with
  `Match="NavLinkMatch.All"` on Home only. The `#blazor-error-ui` block stays.
- **Rationale**: `NavLink` adds the `active` class that Bootstrap styles for the current page
  (US3 scenario 3). `NavLinkMatch.All` stops Home from matching every route. Relative `href`s work
  with the `<BasePath />` the template already renders.
- **Alternatives considered**: Adding a `navbar-toggler` with a collapse menu was rejected because
  PLog has none (spec Edge Cases).

## R5. Page headings

- **Decision**: Keep a semantic `h1` on each page, styled with `class="h3 mb-3"` (`h3 mb-0` on
  Home, where the description follows directly). Card titles are `h2.card-title.h5`.
- **Rationale**: Contracts 001 and 002 require exactly one `h1`, and `Routes.razor` focuses `h1`
  after navigation. The `h3` utility class gives PLog's visual size without breaking either rule.

## R6. Placeholder page names

- **Decision**: The components are `TablePage`, `HistogramsPage`, and `TimelinePage`, at `/table`,
  `/histograms`, and `/timeline`.
- **Rationale**: The `…Page` suffix follows `ConvertPage`. A component class named `Table` would
  be an easy name collision, the same issue the 002 lesson describes for `Convert`.

## R7. Source control for vendored files

- **Decision**: Add `!src/JoakimSoftware.TcxTool.Web/wwwroot/lib/` to `tcxtool/.gitignore`. Leave
  the repository-root `.gitignore` alone.
- **Rationale**: The root file is generated by the maintainer's `gitignore-here` alias, and it
  ignores `lib/` everywhere. A deeper `.gitignore` takes precedence, so a scoped negation re-includes
  only this directory (FR-011). Without it, local builds and the Compose gate pass, because Docker
  copies the working tree, but a clean clone would fail `BootstrapAssetsTests`.

## R8. How the styling is tested

- **Decision**:
  - bUnit tests assert on Bootstrap classes and structure: the navbar's classes, the brand, the
    ordered `(text, href)` list of `nav-link`s, the body inside `main.container`, and the footer
    text. They also check the `form-control` and `btn-primary` classes and the `alert-success` and
    `alert-danger` classes.
  - `WebApplicationFactory<Program>` tests fetch `/` and `/convert`, find the fingerprinted
    `<link>` and `<script>` with a regex, then request each asset and expect 200, the right media
    type, and the text "Bootstrap".
  - Each placeholder route is fetched and checked for 200, its `<title>`, its `h1`, and the navbar.
- **Rationale**: Visual look cannot be unit-tested, but the classes that produce it can. The
  endpoint tests prove the assets are really served by the app (FR-001), and they ignore the
  fingerprint value, which changes with the file content.
- **Alternatives considered**: Browser screenshots were rejected, because the constitution forbids
  live-browser tests (Principle V). The PLog comparison is the manual SC-006.
