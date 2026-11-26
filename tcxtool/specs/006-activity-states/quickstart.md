# Quickstart: Activity States

How to check by hand that the feature works end to end. Contracts:
[csv-format.md](contracts/csv-format.md), [states-csv-format.md](contracts/states-csv-format.md),
[state-file.md](contracts/state-file.md), and [pages.md](contracts/pages.md).

Run every command from `tcxtool/`.

**Real data stays local.** The marathon export and its CSVs hold real GPS positions. These steps
read only the cadence, time, distance, and state columns, and they write to a scratchpad folder,
never to `data/csv/` or `tcxtool-state.json`.

## Prerequisites

- .NET SDK 11.0.100 (`global.json`). Docker Desktop is needed for scenario 1 only.
- A scratchpad folder outside the repository:

  ```zsh
  export QS=$(mktemp -d)
  mkdir -p $QS/csv
  shasum tcxtool-state.json data/csv/*.csv > $QS/before.sha 2>/dev/null
  ```

- The app, run against that folder:

  ```zsh
  TcxTool__CsvOutputDirectory=$QS/csv TcxTool__StateFilePath=$QS/state.json \
    dotnet run --project src/JoakimSoftware.TcxTool.Web
  ```

## Scenario 1: quality gates (SC-006)

Run the four gates listed in CLAUDE.md, including the Compose gate (`echo $?` prints 0).

## Scenario 2: convert the marathon (US1, US2, SC-003)

1. Open `/convert` and upload `data/tcx/24932322230.tcx`. Note the time it takes.
2. The result lists `2026-11-01_24932322230.csv: 19908 rows` and
   `2026-11-01_24932322230.states.csv: 541 StateSets`.
3. Check the activity CSV's header and states:

   ```zsh
   head -1 $QS/csv/2026-11-01_24932322230.csv | tr '|' '\n' | sed -n '13,15p'   # RunCadence State StateSeconds
   awk -F'[|]' 'NR>1 {n[$14]++} END {for (s in n) print s, n[s]}' $QS/csv/2026-11-01_24932322230.csv
   ```

   **Expected**: running 11206, walking 7982, stopped 720, and no `unknown`. Every line has 19
   fields:

   ```zsh
   awk -F'[|]' 'NF != 19' $QS/csv/2026-11-01_24932322230.csv | wc -l   # 0
   ```

4. Check that the States CSV adds up:

   ```zsh
   awk -F'[|]' 'NR>1 {n++; s+=$5; d+=$11} END {print n, s, d}' $QS/csv/2026-11-01_24932322230.states.csv
   ```

   **Expected**: `541 19881` and a distance within 0.001 of 26.427, which is the activity CSV's last
   `DistanceMiles`.

5. **Expected**: the conversion took under 10 seconds.

## Scenario 3: the state file and the home page (US2)

1. `cat $QS/state.json` shows both `csvFilename` and `statesCsvFilename`, exactly as in
   contracts/state-file.md.
2. On the home page, the Select CSV File list has `2026-11-01_24932322230.csv` and no
   `.states.csv` file.
3. Copy an old 17-field CSV in, select it, and check the state file:

   ```zsh
   cp data/csv/2026-11-01_24932322230.csv $QS/csv/2026-11-01_old.csv
   ```

   Select `2026-11-01_old.csv` on the home page. **Expected**: `state.json` has no
   `statesCsvFilename`.

## Scenario 4: old CSVs (FR-017)

With `2026-11-01_old.csv` still current:

- `/table` shows the wrong-format notice naming the file, with a "Convert its TCX file again" link.
- `/states` says the current CSV has no States CSV, with the same link.

## Scenario 5: the States page (US3, SC-004)

1. Select `2026-11-01_24932322230.csv` again. In the navbar, "States" sits straight after "Table".
2. Open `/states` directly, and time it in the browser's network panel.
   **Expected**: the page is fully shown in under 5 seconds, with 541 rows and 13 column headings.
3. Scroll the table. **Expected**: the heading row stays visible.
4. **Expected**: AverageHeartRateBpm and AverageRunCadence cells are colored as the Table page colors
   those values. Every other cell is uncolored.
5. On `/table`, a RunCadence cell showing 180 or more gets the darkest cadence color.

## Scenario 6: States page notices (FR-022, SC-005)

Use the scratchpad state file. After each step, reload `/states`. **Expected**: a notice, never an
error page.

| Step | Expected notice |
|------|-----------------|
| `rm $QS/state.json` | No CSV is selected yet |
| select the marathon CSV again, then `mv $QS/csv/2026-11-01_24932322230.states.csv $QS/` | the States CSV is no longer in the CSV folder |
| `printf 'hello\n' > $QS/csv/2026-11-01_24932322230.states.csv` | not in the format this app writes |
| `head -1 $QS/2026-11-01_24932322230.states.csv > $QS/csv/2026-11-01_24932322230.states.csv` | This activity has no state changes |

## Scenario 7: nothing real changed (FR-024, FR-026)

```zsh
shasum tcxtool-state.json data/csv/*.csv 2>/dev/null | diff $QS/before.sha - && echo unchanged
rm -rf $QS
```

**Expected**: `unchanged`. Then stop the app.
