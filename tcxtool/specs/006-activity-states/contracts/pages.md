# Contract: Pages

**Requirements**: FR-011, FR-017 to FR-025 | **Stories**: US1, US2, US3

The rendered HTML that tests check. Pages are static SSR, with no `@rendermode` (FR-019): a test
checks that the body contains no `<!--Blazor:{` (feature 005 lesson). If this contract and the spec
disagree, the spec wins.

## Navbar (changed)

`MainLayout` links, in order: Home, Convert, Table, **States** (`href="states"`), Histograms,
Timeline, About. On `/states` the States link is the active one.

## Convert page result (changed, FR-011)

For each saved activity, in order, the success list has two items:

```html
<li>2026-11-01_run-two-laps.csv: 10 rows</li>
<li>2026-11-01_run-two-laps.states.csv: 1 StateSet</li>
```

- The first item is unchanged from feature 002, including `contains no trackpoints` for 0 rows.
- The second reads `<StatesFileName>: 1 StateSet` when the count is 1,
  `<StatesFileName>: <StateSetCount> StateSets` when it is 2 or more, and
  `<StatesFileName>: contains no StateSets` when it is 0. (`run-two-laps.tcx`'s cadence, 59–68,
  doubles to 118–136, all `walking`, so it gives 1 StateSet.)

## Table page (changed)

- **Cadence bands** (FR-018): `TrackpointBands.RunCadenceClass` compares the value as written.

  | Class | Range (steps per minute) |
  |-------|--------------------------|
  | `cadence-band-1` | below 20 |
  | `cadence-band-2` | 20 to below 150 |
  | `cadence-band-3` | 150 to below 180 |
  | `cadence-band-4` | 180 or more |

  The colors and the heart-rate bands are unchanged from feature 005.
- **Wrong format** (FR-017): the notice becomes

  ```html
  <div class="alert alert-warning" id="table-status" role="alert">
      <p class="mb-0">The current CSV is not in the format this app writes: <strong>NAME</strong>. <a href="convert">Convert its TCX file again</a>.</p>
  </div>
  ```

  `Unreadable` keeps its feature 005 notice, with no link.
- Everything else, including the 10 columns, is as feature 005's `contracts/pages.md`.

## States page (new)

`GET /states` returns 200, `<title>States</title>`, and `<h1 class="h3 mb-3">States</h1>`. The
content depends on `CurrentStatesLoader.LoadAsync`:

| Status | How it is reached | Content after the `h1` |
|--------|-------------------|------------------------|
| `NoCurrentCsv` | `CurrentCsvStore` returns `None` | `div.alert.alert-info#states-status[role=status]`: `No CSV is selected yet.` with `<a href="">Select a CSV file</a> or <a href="convert">convert a TCX file</a>.` |
| `NoStatesCsv` | `CurrentCsv.StatesFileName` is null | `div.alert.alert-warning#states-status[role=alert]`: `The current CSV has no States CSV: <strong>CSV</strong>. <a href="convert">Convert its TCX file again</a>.` |
| `Missing` | opening the States CSV throws `FileNotFoundException` or `DirectoryNotFoundException` | `div.alert.alert-warning#states-status[role=alert]`: `The States CSV is no longer in the CSV folder: <strong>STATES</strong>. <a href="convert">Convert its TCX file again</a>.` |
| `Unreadable` | any other `IOException`, or `UnauthorizedAccessException`; a warning is logged | `div.alert.alert-warning#states-status[role=alert]`: `The States CSV could not be read: <strong>STATES</strong>.` |
| `WrongFormat` | `StatesCsvReader` returns an error; a warning is logged | `div.alert.alert-warning#states-status[role=alert]`: `The States CSV is not in the format this app writes: <strong>STATES</strong>. <a href="convert">Convert its TCX file again</a>.` |
| `NoStateSets` | the file has only its header | `p#states-file` (below), then `div.alert.alert-info#states-status[role=status]`: `This activity has no state changes.` |
| `Loaded` | one or more rows | `p#states-file`, then the table (below) |

- `p#states-file` is `<p class="mb-2" id="states-file">States CSV: <strong>STATES</strong></p>`.
- A current CSV whose own file is missing does not stop the States page (research R9).
- Logs carry the file name and the exception type or error kind and line number, never file
  content (feature 005 R11).
- Serving the page changes no file (FR-024). A test snapshots the temp directory's bytes and
  modification times across two requests.

### Table

```html
<div class="table-responsive states-table-scroll">
  <table class="table table-sm table-bordered small mb-0" id="states">
    <thead class="table-light">
      <tr><th scope="col">ActivityId</th> … 13 headings in States CSV order … </tr>
    </thead>
    <tbody>
      <tr><td>…</td> … 13 cells … </tr>
    </tbody>
  </table>
</div>
```

- One `tr` per States CSV data row, in file order. Each cell holds its field exactly as written;
  empty fields are empty cells (FR-020).
- The AverageHeartRateBpm cell (column 9) has `class="<HeartRateClass>"`, and the AverageRunCadence
  cell (column 10) has `class="<RunCadenceClass>"`. A null class leaves the attribute out, with no
  empty `class=""`. Every other cell has no attributes (FR-021).
- No `table-striped` and no `table-hover`, which would shade over the band colors (feature 005
  lesson).

### Styles (`wwwroot/app.css`)

```css
.states-table-scroll { max-height: 75vh; overflow-y: auto; }
.states-table-scroll thead th { position: sticky; top: 0; z-index: 1; }
```

Each selector has exactly one rule, under a comment that names feature 006 and research R11. The
band classes are feature 005's, reused unchanged.
