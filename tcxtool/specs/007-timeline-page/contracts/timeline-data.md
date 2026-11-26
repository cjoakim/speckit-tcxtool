# Contract: Timeline Data (server to browser)

**Requirements**: FR-012 to FR-021 | **Stories**: US2, US3 | **Research**: R4 to R8

The JSON the Timeline page puts in `<tcx-timeline data-timeline="…">`, and the only input
`wwwroot/js/timeline.js` draws from. It is `TimelineLayout` (data-model.md), serialized by
`System.Text.Json` with camelCase property names and default number handling (shortest round-trip).
Blazor HTML-encodes the attribute. The script reads it back with
`JSON.parse(element.dataset.timeline)`.

## Shape

```json
{
  "totalSeconds": 150,
  "fieldNames": ["ActivityId", "ActivityDistanceMiles", "…", "AverageAltitudeFeet"],
  "segments": [
    { "state": "walking", "start": 0, "end": 0.24666666666666667, "center": 0.12333333333333334,
      "values": ["run-states", "0.0712", "…16 strings in fieldNames order…"] }
  ],
  "charts": [
    { "label": "Run Cadence", "field": "AverageRunCadence", "min": 99.3, "max": 172.5,
      "points": [ { "center": 0.12333333333333334, "value": 99.3 } ] },
    { "label": "Heart Rate", "field": "AverageHeartRateBpm", "min": null, "max": null, "points": [] },
    { "label": "Altitude", "field": "AverageAltitudeMeters", "min": 30, "max": 32, "points": [] }
  ]
}
```

The numbers are illustrative.

## Rules

| Rule | Detail |
|------|--------|
| Segments | one per StateSet, in file order. `segments[0].start` is `0`, `segments[i].end` equals `segments[i+1].start`, and the last `end` is exactly `1` |
| Widths | `end − start` is the set's seconds over `totalSeconds`, or `1/n` when `totalSeconds` is 0 (R5) |
| Values | exactly `fieldNames.length` (16) strings per segment, as written in the file, never parsed or reformatted |
| Charts | exactly three, in the order Run Cadence, Heart Rate, Altitude |
| Points | one per set with a finite value, in set order, at the set's `center` |
| Domain | `min` and `max` of the points, `v ± 1` when all are equal, both `null` when there are no points (R6) |

## What the script does with it

| Step | Rule |
|------|------|
| Width | `w` = the element's width. Every SVG is `w` wide, with margin left 56 px, right 16 px, and plot width `p = w − 72` |
| Bar | in `div.timeline-bar`: one `rect.timeline-segment.state-<state>` per segment, at `x = 56 + start × p`, with width `(end − start) × p`. An unknown state gets class `state-unknown` |
| Time axis | under the bar: `d3.scaleLinear([0, totalSeconds], [56, 56 + p])`, ticks formatted `h:mm:ss` |
| Charts | in each server-rendered `div.timeline-chart[data-chart=i]`: an SVG 140 px tall with `d3.axisLeft` over `[min, max]` and 4 ticks, a `circle` per point at `x = 56 + center × p`, and one `path.timeline-line` through the points. The labels and "No data." are rendered by the server (contracts/pages.md) |
| Popup | `pointermove` over the 32 px bar (not the time axis under it) first looks for segments under 4 px wide whose center is within ±4 px of the pointer. If there is one, the nearest wins (ties go to the earlier one), even when the pointer is over a wide segment, so zero-width segments can be reached. Otherwise it picks the segment whose `[start, end)` holds the pointer's fraction, by bisecting the starts. It fills `div.timeline-popup` with `fieldNames[i]` and `values[i]`, through `textContent` |
| Close | a `click` on the document outside the popup hides it. Leaving the bar does not |
| Resize | a `ResizeObserver` redraws when `w` changes |
| Forbidden | no color literals (the colors come from CSS classes, R12), no network URLs, and no data rules beyond the steps above |
