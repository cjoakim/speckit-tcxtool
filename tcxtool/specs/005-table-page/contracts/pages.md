# Contract: Table Page

**Requirements**: FR-001 to FR-003, FR-005 to FR-021 | **Stories**: US1, US2, US3

This contract replaces the Table row of
[003 pages.md](../../003-claude-ui-enhancements/contracts/pages.md) ("`<h1>` only"). Everything else
in the 001 to 004 contracts still holds, including the navbar list in
[004 pages.md](../../004-about-and-csv-state/contracts/pages.md). The page stays static SSR, with no
`@rendermode` (FR-020).

## Page (`/table`, `Pages/TablePage.razor` + `TablePage.razor.cs`)

| Element | Requirement |
|---------|-------------|
| Route | `/table` only (FR-018) |
| `<PageTitle>` | `Table` |
| Heading | exactly one `h1.h3.mb-3`: `Table`, first in the page body, in every state |
| Body | after the heading, the content for the `CurrentActivityStatus`, as below. Nothing else. |

### Content by status

`<name>` is the current CSV's file name, HTML-encoded by Razor. It is never interpolated into a
message string.

| Status | Markup after the `h1` |
|--------|----------------------|
| `NoCurrentCsv` | `div.alert.alert-info#table-status[role=status]` > `p.mb-0`: message, then ` ` + `a[href=""]` `Select a CSV file` + ` or ` + `a[href=convert]` `convert a TCX file` + `.` |
| `Missing` | `div.alert.alert-warning#table-status[role=alert]` > `p.mb-0`: message, then ` ` + `strong` `<name>` + `. ` + `a[href=""]` `Select another CSV file` + `.` |
| `Unreadable` | `div.alert.alert-warning#table-status[role=alert]` > `p.mb-0`: message, then ` ` + `strong` `<name>` + `.` |
| `WrongFormat` | `div.alert.alert-warning#table-status[role=alert]` > `p.mb-0`: message, then ` ` + `strong` `<name>` + `.` |
| `NoTrackpoints` | `ActivitySummaryCard`, then `div.alert.alert-info#table-status[role=status]` > `p.mb-0`: message |
| `Loaded` | `ActivitySummaryCard`, then `TrackpointTable` |

There is no `table` element in any state except `Loaded` (FR-002, FR-005, FR-011). There is no
`#activity-summary` in the first four states.

## Header box (`Table/ActivitySummaryCard.razor`)

| Element | Requirement |
|---------|-------------|
| Card | `div.card.mb-3` > `div.card-body#activity-summary`: the Home page's card classes (FR-008) |
| Title | `h2.card-title.h5`: `Activity` |
| Values | `dl.row.mb-0` with six pairs, in this order. Each pair is `dt.col-sm-3` + `dd.col-sm-9`. |

| `dt` text | `dd` content |
|-----------|--------------|
| `CSV file` | `Summary.FileName` |
| `ActivityId` | `Summary.ActivityId` |
| `Date` | `Summary.Date` |
| `DeviceName` | `Summary.DeviceName` |
| `DeviceId` | `Summary.DeviceId` |
| `Sport` | `Summary.Sport` |

An empty value is shown as `—` (U+2014) in a `span.text-muted` (FR-010). Each `dt` is always
present.

## Table (`Table/TrackpointTable.razor`)

| Element | Requirement |
|---------|-------------|
| Scroll container | `div.table-responsive.trackpoint-table-scroll` (research R7) |
| Table | `table.table.table-sm.table-bordered.small.mb-0#trackpoints`. Not `table-striped` and not `table-hover` (research R5). |
| Head | `thead.table-light` > one `tr` > 10 `th[scope=col]`, text exactly, in order: `TrackpointNum`, `Time`, `ElapsedSeconds`, `ElapsedTime`, `HeartRateBpm`, `RunCadence`, `DistanceMiles`, `Latitude`, `Longitude`, `AltitudeFeet` |
| Body | `tbody` > one `tr` per row, in file order. Each `tr` has 10 `td` in the same column order. |
| Cell text | the row's field, exactly as written, with no added whitespace. An empty field gives an empty `td` (FR-013). |
| HeartRateBpm `td` | `class` = `TrackpointBands.HeartRateClass(value)`. No `class` attribute when that is null. |
| RunCadence `td` | `class` = `TrackpointBands.RunCadenceClass(value)`. No `class` attribute when that is null. |
| Other `td` | no `class` attribute (FR-016) |

The cells use no scoped CSS attribute and no inline `style` (research R5).

## Styles (`wwwroot/app.css`)

```css
.trackpoint-table-scroll { max-height: 75vh; overflow-y: auto; }
.trackpoint-table-scroll thead th { position: sticky; top: 0; z-index: 1; }
```

These are the ten band classes. Each sets exactly these two variables, with these values:

| Class | `--bs-table-bg` | `--bs-table-color` |
|-------|-----------------|--------------------|
| `.hr-band-1` | `#cff4fc` | `#000` |
| `.hr-band-2` | `#fff3cd` | `#000` |
| `.hr-band-3` | `#ffe5d0` | `#000` |
| `.hr-band-4` | `#fd7e14` | `#000` |
| `.hr-band-5` | `#f8d7da` | `#000` |
| `.hr-band-6` | `#b02a37` | `#fff` |
| `.cadence-band-1` | `#cff4fc` | `#000` |
| `.cadence-band-2` | `#fff3cd` | `#000` |
| `.cadence-band-3` | `#d1e7dd` | `#000` |
| `.cadence-band-4` | `#146c43` | `#fff` |

Each pair has a WCAG contrast of at least 4.5:1 (FR-017).

## Messages

`CurrentActivityMessages.For` is the only place these strings appear:

| Status | Text |
|--------|------|
| `NoCurrentCsv` | `No CSV is selected yet.` |
| `Missing` | `The current CSV is no longer in the CSV folder:` |
| `Unreadable` | `The current CSV could not be read:` |
| `WrongFormat` | `The current CSV is not in the format this app writes:` |
| `NoTrackpoints` | `This activity has no trackpoints.` |
| `Loaded` | `""` |

No message includes file content (FR-006). "CSV folder" follows the wording in 004.

## HTTP

- `GET /table` returns `200` with the page, in every status.
- The request changes no file: the current CSV and `tcxtool-state.json` are byte for byte the same
  afterwards, and no file is created (FR-007).
- There is no POST handler and no form.

## Verified by

- `Web/Table/TrackpointBandsTests`: every edge, decimals, and the no-band inputs
- `Web/Analysis/CurrentActivityLoaderTests`: every status, logging, no writes
- `Web/Analysis/CurrentActivityMessagesTests`
- `Web/Table/ActivitySummaryCardTests` (bUnit)
- `Web/Table/TrackpointTableTests` (bUnit)
- `Web/TablePageTests` (bUnit): heading, route, every status
- `Web/TablePageEndpointTests` (HTTP): `Loaded` markup, no-state page, files unchanged
- `Web/TablePagePerformanceTests` (HTTP): 20,000 rows (SC-002, server side)
- `Web/TableStylesTests`: `app.css` band classes and contrast
- `Web/PlaceholderPageEndpointTests`: `/table` still served inside the layout
