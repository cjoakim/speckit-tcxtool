# Data Model: Timeline Page

**Feature**: `specs/007-timeline-page/spec.md` | **Research**: [research.md](./research.md)

Types that change or are new in feature 007. Every record is immutable. Everything not listed here
is unchanged from feature 006.

## Parser library (`JoakimSoftware.TcxTool.Parser.Csv`)

### `StatesCsvRow` (changed)

One StateSet. There are 16 positional `string` properties, in file order (FR-002), and each holds
the field exactly as written.

| # | Property | Rule | Empty when |
|---|----------|------|------------|
| 1 | `ActivityId` | the set's ActivityId | never |
| 2 | `ActivityDistanceMiles` | **new**: `DistanceMiles` of the set's last row with one, as written (R1) | no row in the set has one |
| 3 | `ActivityElapsedTime` | unchanged | no row in the set has one |
| 4 | `ActivityElapsedSeconds` | **new**: `ElapsedSeconds` of the set's last row with one, as written (R1) | no row in the set has one |
| 5 | `State` | unchanged | never |
| 6 | `StateEndDate` | unchanged | no row in the set has a Time |
| 7 | `StateEndTrackpointNum` | unchanged | never |
| 8 | `StateTrackpointCount` | **new**: the number of rows in the set, as an invariant integer (R1) | never |
| 9 | `StateElapsedSeconds` | unchanged | no row in the set has one |
| 10 | `StateDistanceMiles` | **renamed** from `DistanceMiles`, with the same rule (FR-003) | no row in the set has one |
| 11 | `AverageHeartRateBpm` | unchanged | no row in the set has one |
| 12 | `AverageRunCadence` | unchanged | no row in the set has one |
| 13 | `AverageLatitude` | unchanged | no row in the set has one |
| 14 | `AverageLongitude` | unchanged | no row in the set has one |
| 15 | `AverageAltitudeMeters` | unchanged | no row in the set has one |
| 16 | `AverageAltitudeFeet` | unchanged | no row in the set has one |

**Invariants**:
- Over one activity, the `StateTrackpointCount` values add up to the activity CSV's row count.
- The last row's `ActivityDistanceMiles` and `ActivityElapsedSeconds` equal the activity CSV's last
  non-empty `DistanceMiles` and `ElapsedSeconds`, byte for byte.

### `StatesCsvWriter` (changed)

- `Header`: the 16 names joined by `|` (contracts/states-csv-format.md).
- `FieldCount`: 16.
- `Rows`: adds the three values.
- `Write`: writes in record order.

`StatesCsvReader` maps the 16 fields in the same order. It needs no other change: a 13-field file is
a `HeaderMismatch` (R3).

## Web project (`JoakimSoftware.TcxTool.Web.Timeline`, new namespace)

### `TimelineHeader`

| Property | Value |
|----------|-------|
| `FileName` | the current CSV's file name |
| `ActivityId` | the first row's ActivityId |
| `Date` | the file name's first 10 characters when they parse as `yyyy-MM-dd` (invariant, exact), otherwise `""` |
| `ActivityDistanceMiles` | the last row's non-empty ActivityDistanceMiles, otherwise `""` |
| `ActivityElapsedTime` | the last row's non-empty ActivityElapsedTime, otherwise `""` |

`static TimelineHeader From(string csvFileName, IReadOnlyList<StatesCsvRow> rows)` throws
`ArgumentException` for an empty `rows`. The page only builds a header when the status is `Loaded`.

### `TimelineLayout`

The whole drawing as numbers. It is serialized to the page as JSON (contracts/timeline-data.md).

| Property | Type | Value |
|----------|------|-------|
| `TotalSeconds` | `double` | the left-to-right sum of every set's seconds (R5) |
| `FieldNames` | `IReadOnlyList<string>` | the 16 header names, in file order |
| `Segments` | `IReadOnlyList<TimelineSegment>` | one per set, in file order |
| `Charts` | `IReadOnlyList<TimelineChart>` | exactly 3, in the order Run Cadence, Heart Rate, Altitude |

`static TimelineLayout From(IReadOnlyList<StatesCsvRow> rows)` throws `ArgumentException` for an
empty `rows`.

### `TimelineSegment`

| Property | Type | Value |
|----------|------|-------|
| `State` | `string` | the set's State, as written |
| `Start` | `double` | fraction of the bar where the segment begins, 0 for the first segment |
| `End` | `double` | fraction where it ends; equal to the next segment's `Start`, and exactly 1 for the last |
| `Center` | `double` | `(Start + End) / 2` |
| `Values` | `IReadOnlyList<string>` | the set's 16 fields as written, in `FieldNames` order |

**Seconds of a set**: StateElapsedSeconds parsed as invariant `double`. It is used when finite and
≥ 0, and is 0 otherwise.

**Widths**: proportional to seconds when `TotalSeconds > 0`, otherwise `1/n` each (R5).

**Invariants**: `Start ≤ Center ≤ End`, and the segments are contiguous, with no gaps or overlaps.

### `TimelineChart`

| Property | Type | Value |
|----------|------|-------|
| `Label` | `string` | `"Run Cadence"`, `"Heart Rate"`, or `"Altitude"` |
| `Field` | `string` | `"AverageRunCadence"`, `"AverageHeartRateBpm"`, or `"AverageAltitudeMeters"` |
| `Min` | `double?` | the smallest value, or `v − 1` when all values equal `v`; null when there are no values |
| `Max` | `double?` | the largest value, or `v + 1` when all values equal `v`; null when there are no values |
| `Points` | `IReadOnlyList<TimelinePoint>` | one per set with a finite value, in set order |

### `TimelinePoint`

| Property | Type | Value |
|----------|------|-------|
| `Center` | `double` | the set's segment `Center` |
| `Value` | `double` | the parsed value |

## Web components

| Component | Role |
|-----------|------|
| `Components/States/StatesNotice.razor` (new) | The notice `switch` moved out of `StatesPage` (R14). Parameters: `Current` (`CurrentStates`) and `StatusId` (`string`). It renders nothing for `Loaded` |
| `Components/States/StatesTable.razor` (changed) | 16 columns in the FR-002 order. The band classes stay on AverageHeartRateBpm and AverageRunCadence |
| `Components/Timeline/TimelineHeaderCard.razor` (new) | The header box (R13) |
| `Components/Timeline/TimelineView.razor` (new) | `<tcx-timeline data-timeline="…">` with its server-rendered containers (the bar `div`, and per chart a label and a chart `div` or "No data."), the legend, and the `noscript` note |
| `Components/Pages/TimelinePage.razor` and `.razor.cs` (replaced) | Loads `CurrentStatesLoader`, then shows `StatesNotice`, or the header card and `TimelineView` |

## Browser assets

| File | Role |
|------|------|
| `wwwroot/js/timeline.js` (new) | ES module. Defines `tcx-timeline` and draws from the JSON (R8, R10, R11). It holds no data rules and no colors |
| `wwwroot/lib/d3/d3.min.js`, `LICENSE`, `README.md` (new) | D3 7.9.2, vendored (R9). The UMD bundle, loaded by `timeline.js` on demand (R8, as implemented) |
| `wwwroot/app.css` (changed) | The state color properties and classes, and the `.timeline-*` rules (R10, R12) |
| `Components/App.razor` (changed) | Adds `<script type="module" src="@Assets["js/timeline.js"]"></script>` |
