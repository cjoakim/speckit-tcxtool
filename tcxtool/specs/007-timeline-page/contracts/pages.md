# Contract: Pages

**Requirements**: FR-005, FR-007 to FR-024 | **Stories**: US1, US2, US3

The rendered HTML that tests check. Pages stay static SSR, with no `@rendermode`: a test checks
that the body contains no `<!--Blazor:{` (feature 005 lesson). If this contract and the spec
disagree, the spec wins. Anything not listed here is as in
`specs/006-activity-states/contracts/pages.md`.

## Shared: `App.razor` (changed)

After `blazor.web.js`, the body loads the Timeline module on every page:

```html
<script type="module" src="@Assets["js/timeline.js"]"></script>
```

The module only defines the `tcx-timeline` element. It loads the vendored `lib/d3/d3.min.js` (a
classic script that defines the `d3` global) when such an element first connects, so no page has its
own `<script>` or `<link>` for D3 (research R8, as implemented).

## States page (changed, FR-005)

- The notices are rendered by `StatesNotice` with `StatusId="states-status"`, with the same markup
  and text as in feature 006 (research R14).
- The table has **16** headings and 16 cells per row, in the order of
  `contracts/states-csv-format.md`.
- The band classes move with their columns: `HeartRateClass` is on AverageHeartRateBpm (column 11),
  and `RunCadenceClass` is on AverageRunCadence (column 12). Every other cell has no attributes, as
  before.
- A 13-field States CSV shows the `WrongFormat` notice (FR-006).

## `StatesNotice` (new component)

| Parameter | Meaning |
|-----------|---------|
| `Current` | the loader's `CurrentStates` |
| `StatusId` | the `id` of the alert `div`: `states-status` or `timeline-status` |

For every status except `Loaded`, it renders the feature 006 alert for that status (same classes,
role, text, `<strong>` name, and links), with `id="@StatusId"`. For `Loaded` it renders nothing.
Each page renders its own content before and after it.

## Timeline page (replaced)

`GET /timeline` returns 200, `<title>Timeline</title>`, and `<h1 class="h3 mb-3">Timeline</h1>`. The
Timeline navbar link is active. The content depends on `CurrentStatesLoader.LoadAsync` (FR-007,
FR-008):

| Status | Content after the `h1` |
|--------|------------------------|
| `NoCurrentCsv`, `NoStatesCsv`, `Missing`, `Unreadable`, `WrongFormat`, `NoStateSets` | `StatesNotice` with `StatusId="timeline-status"`, and nothing else: no card, no `tcx-timeline` |
| `Loaded` | the header card, then the timeline view (below) |

Serving the page changes no file (FR-009). A test snapshots the temp directory's bytes and
modification times across two requests.

### Header card (FR-010, FR-011)

```html
<div class="card mb-3">
  <div class="card-body" id="timeline-summary">
    <h2 class="card-title h5">Activity</h2>
    <dl class="row mb-0">
      <dt class="col-sm-3">CSV file</dt><dd class="col-sm-9">NAME</dd>
      <dt class="col-sm-3">ActivityId</dt><dd class="col-sm-9">…</dd>
      <dt class="col-sm-3">Date</dt><dd class="col-sm-9">yyyy-MM-dd</dd>
      <dt class="col-sm-3">ActivityDistanceMiles</dt><dd class="col-sm-9">…</dd>
      <dt class="col-sm-3">ActivityElapsedTime</dt><dd class="col-sm-9">…</dd>
    </dl>
  </div>
</div>
```

The values come from `TimelineHeader` (data-model.md). An empty value is
`<span class="text-muted">—</span>`, as on the Table page.

### Timeline view (FR-012 to FR-021)

```html
<section id="timeline" aria-label="Timeline">
  <ul class="list-inline small mb-2" id="timeline-legend">
    <li class="list-inline-item"><span class="timeline-swatch state-stopped"></span> stopped</li>
    <li class="list-inline-item"><span class="timeline-swatch state-walking"></span> walking</li>
    <li class="list-inline-item"><span class="timeline-swatch state-running"></span> running</li>
    <li class="list-inline-item"><span class="timeline-swatch state-unknown"></span> unknown</li>
  </ul>
  <tcx-timeline data-timeline="{…JSON, HTML-encoded…}">
    <div class="timeline-bar"></div>
    <h2 class="h6 mt-3 mb-1">Run Cadence</h2><div class="timeline-chart" data-chart="0"></div>
    <h2 class="h6 mt-3 mb-1">Heart Rate</h2><div class="timeline-chart" data-chart="1"></div>
    <h2 class="h6 mt-3 mb-1">Altitude</h2><p class="text-muted small timeline-no-data">No data.</p>
  </tcx-timeline>
  <noscript><div class="alert alert-secondary mt-2" id="timeline-noscript">The timeline and charts need JavaScript.</div></noscript>
</section>
```

- `data-timeline` holds the JSON of `contracts/timeline-data.md`. Decoded and parsed, it has 16
  `fieldNames`, one segment per States CSV row, and 3 charts.
- A CSV value containing `"`, `<`, `>`, `&`, or `</script>` is encoded in the attribute, and parses
  back to the exact text (research R7).
- The server renders the structure, so tests can check it:
  - the `div.timeline-bar`
  - for each chart, in order, an `h2.h6` label ("Run Cadence", "Heart Rate", "Altitude")
  - then either `div.timeline-chart[data-chart=i]`, when the chart has points, or
    `p.timeline-no-data` "No data.", when it has none. The example above shows an Altitude chart
    with no points.
- The script draws into those containers:
  - the bar SVG, with `aria-label="States over time"`, and its time axis, in `div.timeline-bar`
  - each chart's SVG in its `div.timeline-chart`
  - the popup, `div.timeline-popup`, appended to the element and hidden until a segment is hovered
- In User Story 2, before the charts exist, the element holds only `div.timeline-bar`.

### Styles (`wwwroot/app.css`)

Under a comment naming feature 007 and research R10 and R12:

```css
:root {
    --tcx-state-stopped: #6c757d;
    --tcx-state-walking: #ffc107;
    --tcx-state-running: #198754;
    --tcx-state-unknown: #dee2e6;
}
.state-stopped { fill: var(--tcx-state-stopped); background-color: var(--tcx-state-stopped); }
.state-walking { fill: var(--tcx-state-walking); background-color: var(--tcx-state-walking); }
.state-running { fill: var(--tcx-state-running); background-color: var(--tcx-state-running); }
.state-unknown { fill: var(--tcx-state-unknown); background-color: var(--tcx-state-unknown); }
.timeline-swatch { display: inline-block; width: 0.9em; height: 0.9em; vertical-align: -0.1em; }
.timeline-line { fill: none; stroke: #adb5bd; stroke-width: 1px; }
tcx-timeline { display: block; position: relative; }
.timeline-popup { position: absolute; z-index: 10; max-width: 22rem; }
```

- The four hex state colors appear in `app.css` only, once each.
- `timeline.js` has no hex color literal (a guard test).
- These are global rules, not scoped CSS, because the script creates the elements (feature 005
  lesson).

## Unchanged (FR-022)

The navbar, Home, Convert, Table, and About pages, the activity CSV, and the state file. The
Histograms page stays a placeholder.
