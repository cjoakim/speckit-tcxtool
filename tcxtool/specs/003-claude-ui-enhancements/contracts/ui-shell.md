# Contract: UI Shell (layout, assets, navigation)

**Requirement**: FR-001 to FR-005, FR-010, FR-011 | **Stories**: US1, US3

## Assets

| Asset | Source path | Linked in | Order |
|-------|-------------|-----------|-------|
| Bootstrap 5.3.0 CSS | `wwwroot/lib/bootstrap/css/bootstrap.min.css` | `App.razor` `<head>`, via `@Assets[...]` | before `app.css` |
| Bootstrap 5.3.0 JS bundle | `wwwroot/lib/bootstrap/js/bootstrap.bundle.min.js` | `App.razor` end of `<body>`, via `@Assets[...]` | before `_framework/blazor.web.js` |

- Both files are byte-for-byte copies of PLog's `static/bootstrap/` files, licence headers
  included.
- The rendered URLs are fingerprinted (`bootstrap.min.<hash>.css`). Tests must not hard-code the
  hash.
- No page references a non-local stylesheet or script.
- `tcxtool/.gitignore` contains `!src/JoakimSoftware.TcxTool.Web/wwwroot/lib/`.

## Layout (`Components/Layout/MainLayout.razor`)

In this order:

```html
<nav class="navbar navbar-expand-lg navbar-dark bg-dark">
  <div class="container">
    <a class="navbar-brand" href="">TcxTool</a>
    <div class="navbar-nav"> …nav links… </div>
  </div>
</nav>
<main class="container my-4"> @Body </main>
<footer class="bg-light text-center py-3"><p class="mb-0">&copy; 2026 Chris Joakim</p></footer>
<div id="blazor-error-ui" data-nosnippet> …unchanged template block… </div>
```

## Navigation links

Rendered with `NavLink` and `class="nav-link"`. The current page's link gets `active`.

| # | Text | `href` | Match |
|---|------|--------|-------|
| 1 | Home | `""` | `NavLinkMatch.All` |
| 2 | Convert | `convert` | prefix (default) |
| 3 | Table | `table` | prefix |
| 4 | Histograms | `histograms` | prefix |
| 5 | Timeline | `timeline` | prefix |

Add a link only when its page exists. Keep this table, `MainLayoutTests`, and the layout in step.

## Render mode

The layout and every page in this feature are static SSR. There is no `@rendermode`.

## Verified by

- `MainLayoutTests` (bUnit): navbar classes and brand, ordered link list, body in
  `main.container`, footer.
- `BootstrapAssetsTests` (`WebApplicationFactory`): `/` and `/convert` link the local CSS, and the
  CSS and JS are served with 200, `text/css` or `text/javascript`, and contain "Bootstrap".
- Manual: quickstart.md scenarios 2 and 3.
