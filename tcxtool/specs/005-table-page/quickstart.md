# Quickstart: Table Page for the Current CSV

**Feature**: `005-table-page` | **Contracts**: [pages.md](./contracts/pages.md),
[activity-csv-reader.md](./contracts/activity-csv-reader.md)

Run every command from `tcxtool/`.

## Prerequisites

- .NET SDK 11.0.100 (`global.json`)
- Docker Desktop running (`docker info` succeeds) for scenario 6
- At least one converted CSV in `data/csv/`, for example from a TCX in `data/tcx/`. These hold real
  GPS positions: do not paste their rows anywhere or publish them.

Before you start, record the state file and the CSVs. Scenario 7 checks that neither the page nor
the tests changed them.

```zsh
shasum tcxtool-state.json data/csv/*.csv 2>/dev/null > /tmp/tcxtool-before.sha
```

## 1. Quality gates 1–3 on the host

```zsh
dotnet build JoakimSoftware.TcxTool.slnx
dotnet format JoakimSoftware.TcxTool.slnx --verify-no-changes
dotnet test JoakimSoftware.TcxTool.slnx
```

**Expected**: no warnings or errors, no format changes, and every test passing. That includes
`TablePagePerformanceTests` (20,000 rows in under 5 s, server side).

## 2. The header box (US1)

```zsh
dotnet run --project src/JoakimSoftware.TcxTool.Web   # http://localhost:5169
```

1. On `/`, select a CSV in the Select CSV File card.
2. Click **Table** in the navbar.
3. **Expected**: the heading `Table`, then a card titled `Activity` that looks like the home page
   cards. The card lists `CSV file`, `ActivityId`, `Date`, `DeviceName`, `DeviceId`, and `Sport`.
   The values match the file name and the first data row:

   ```zsh
   sed -n 2p data/csv/<name>.csv | cut -d'|' -f1-5
   ```

4. You can identify the activity without scrolling (SC-005).

## 3. The table and its colors (US2, US3)

On the same page:

1. **Expected**: the table headings are, in order, `TrackpointNum`, `Time`, `ElapsedSeconds`,
   `ElapsedTime`, `HeartRateBpm`, `RunCadence`, `DistanceMiles`, `Latitude`, `Longitude`,
   `AltitudeFeet`.
2. Scroll inside the table. **Expected**: the heading row stays at the top of the table box
   (FR-019).
3. Scroll to the last row. **Expected**: its `TrackpointNum` equals the file's data row count:

   ```zsh
   echo $(( $(wc -l < data/csv/<name>.csv) - 1 ))
   ```

4. **Expected**: HeartRateBpm cells are colored by FR-014, from light blue below 90 up to dark red
   at 180 or more.
5. **Expected**: RunCadence cells while running (CSV values of about 80 to 89) are light green, and
   values of 90 or more are darker green. Walking values (about 50 to 69) are light yellow.
6. **Expected**: numbers on dark orange, dark red, and darker green are easy to read (FR-017). No
   other column is colored.
7. Narrow the window. **Expected**: the table scrolls sideways inside its box, and the page itself
   does not overflow.

## 4. Speed with the real marathon CSV (SC-002)

Use a CSV of about 19,900 rows (the full marathon), or the largest one you have.

1. In the browser, load `/table` directly (type the address and press Enter). **Expected**: the
   whole table is shown in under 5 seconds.
2. Go to `/`, then click **Table** in the navbar (Blazor enhanced navigation). **Expected**: also
   under 5 seconds. If this route is noticeably slower than step 1, record it. The fix is in
   research R4 (`data-enhance-nav="false"` on the Table link), and it needs a contract change.
3. Optional: time the server side alone:

   ```zsh
   curl -s -o /dev/null -w '%{time_total}s %{size_download} bytes\n' http://localhost:5169/table
   ```

   **Expected**: well under 1 s, and about 4–5 MB.

## 5. States without a table (US1, edge cases)

Stop the app between steps when you change files. Restore your files afterwards.

| Setup | Expected on `/table` |
|-------|---------------------|
| `mv tcxtool-state.json /tmp/` | blue notice `No CSV is selected yet.`, with links to the home page and to `convert`; no card and no table |
| state names a CSV, then `mv data/csv/<name>.csv /tmp/` | yellow notice with the name and a link to the home page |
| a CSV with the header line only, selected on `/` (copy the fixture `tests/JoakimSoftware.TcxTool.Tests/Fixtures/Expected/no-trackpoints.csv` into `data/csv/`) | card with the file name and dashes, then `This activity has no trackpoints.` |
| a `.csv` in `data/csv/` whose first line is `hello`, selected on `/` | yellow notice: not in the format this app writes, with the name; no table |

None of these show an error page (SC-004). Delete the extra CSVs afterwards.

## 6. Compose gate (gate 4)

```zsh
docker info > /dev/null && docker compose -f compose.yaml -f compose.test.yaml up \
  --build --abort-on-container-exit --exit-code-from tests; echo "exit $?"
docker compose -f compose.yaml -f compose.test.yaml down
```

**Expected**: `exit 0`. If port 8080 is busy, prefix the first command with
`TCXTOOL_HOST_PORT=8081`.

## 7. Nothing was changed

```zsh
shasum tcxtool-state.json data/csv/*.csv 2>/dev/null | diff /tmp/tcxtool-before.sha - && echo unchanged
```

**Expected**: `unchanged`, apart from files you changed on purpose in scenario 5 and restored. The
Table page never writes (FR-007), and the tests never touch these files (FR-022).
