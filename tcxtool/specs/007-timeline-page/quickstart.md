# Quickstart: Timeline Page

**Feature**: `specs/007-timeline-page/spec.md`

Validation scenarios that show the feature works end to end. Run every command from `tcxtool/`.
The details of each value are in [contracts/](./contracts/) and [data-model.md](./data-model.md).

## Prerequisites

- .NET SDK 11.0.100 (`global.json`). Docker Desktop is running (`docker info`) for scenario 1's
  Compose gate.
- D3 is vendored under `src/JoakimSoftware.TcxTool.Web/wwwroot/lib/d3/`. No network is needed to
  run the app or the tests.
- The manual runs use a scratchpad folder, so the real `data/csv/` and `tcxtool-state.json` are
  never touched:

  ```zsh
  QS=<scratchpad>/qs007; mkdir -p $QS/csv
  shasum tcxtool-state.json data/csv/*.csv data/tcx/*.tcx > $QS/before.sha 2>/dev/null
  TcxTool__CsvOutputDirectory=$QS/csv TcxTool__StateFilePath=$QS/state.json \
    dotnet run --project src/JoakimSoftware.TcxTool.Web --no-launch-profile --urls http://127.0.0.1:5199
  ```

## Scenario 1: Quality gates

The four gates in CLAUDE.md all pass, and the Compose gate exits 0.

## Scenario 2: The 16-field States CSV (US1, SC-001)

Convert `data/tcx/24932322230.tcx` on `/convert`, or post it with `curl -F`. Then read only the
columns needed:

```zsh
S=$QS/csv/2026-11-01_24932322230.states.csv; C=$QS/csv/2026-11-01_24932322230.csv
head -1 $S                                                  # the exact 16-field header
awk -F'[|]' 'NF != 16' $S | wc -l                           # 0
awk -F'[|]' 'NR>1 {n+=$8} END {print n}' $S                 # 19908, the activity CSV's row count
tail -1 $S | cut -d'|' -f2,4; tail -1 $C | cut -d'|' -f17,8 # the same two values (cut prints the second in field order)
```

**Expected**:
- 541 StateSets.
- The counts add up to 19,908.
- The last row's ActivityDistanceMiles is `26.427414388253847`, and its ActivityElapsedSeconds is
  `19881`.

## Scenario 3: The States page (FR-005, FR-006)

`/states` shows 16 columns in the new order, with the band colors on the average heart rate and
cadence columns.

Then copy a feature 006 States CSV into the folder (the 13-field header with one made-up row is
enough), and make it current. Both `/states` and `/timeline` show
`The States CSV is not in the format this app writes: … Convert its TCX file again.`

## Scenario 4: Timeline notices (FR-008)

Each of these returns 200 with a `#timeline-status` notice and no `tcx-timeline` element, with the
same text as the States page:
- no state file
- the States CSV moved away
- `hello` in the States CSV
- a header-only States CSV

## Scenario 5: The Timeline page in a browser (US2, US3; maintainer)

Open `http://127.0.0.1:5199/timeline` with the marathon current:

1. **Header**: the card shows the CSV name, `24932322230`, `2026-11-01`, `26.427414388253847`, and
   the last ActivityElapsedTime.
2. **Bar**:
   - 541 segments, colored gray, amber, and green. The stops are visible at their real length.
   - The legend matches.
   - The time axis ends at about 5:31:21 (19,881 s).
3. **Popup**:
   - Hovering a segment shows its 16 labeled values, the same as that row on `/states`.
   - Moving to another segment replaces them.
   - Moving off the bar leaves the popup open. Clicking elsewhere closes it.
4. **Charts**: Run Cadence, Heart Rate, and Altitude, each with its own y-range and thin gray lines.
5. **Alignment (SC-003)**: at three window widths (about 800, 1200, and 1800 px), run this in the
   devtools console. It must print `true true`:

   ```js
   const s=[...document.querySelectorAll('tcx-timeline svg')].map(e=>e.getBoundingClientRect());
   console.log(s.every(r=>r.width===s[0].width && r.left===s[0].left),
     [...document.querySelectorAll('tcx-timeline svg circle')].length > 0);
   ```

6. **Speed (SC-002, SC-004)**:
   - After a hard reload, the bar and charts appear in under 3 s. Check this in the Network or
     Performance panel.
   - A popup appears with no visible delay.
7. **Enhanced navigation**: go to `/states`, then click the Timeline navbar link. The drawing appears
   without a full reload.
8. **No script**: with JavaScript turned off, the header card and the `noscript` note show.

## Scenario 6: Nothing real changed

```zsh
shasum tcxtool-state.json data/csv/*.csv data/tcx/*.tcx 2>/dev/null | diff $QS/before.sha - && echo unchanged
```

Stop the app and delete `$QS`.

## Note for the maintainer

After this feature, every existing States CSV is in the 13-field format. That includes any made by
feature 006 under `data/csv/`. Convert each TCX file once more to see it on the States and Timeline
pages.
