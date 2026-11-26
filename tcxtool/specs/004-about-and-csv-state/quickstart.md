# Quickstart: About Page and Current CSV State

**Feature**: `004-about-and-csv-state` | **Contracts**: [state-file.md](./contracts/state-file.md),
[pages.md](./contracts/pages.md)

Run every command from `tcxtool/`.

## Prerequisites

- .NET SDK 11.0.100 (`global.json`)
- Docker Desktop running (`docker info` succeeds) for scenario 5
- At least one sample TCX in `data/tcx/`. Do not read it into context or publish it.

Before you start, note whether `tcxtool-state.json` exists. Scenario 6 checks that the tests did
not touch it.

```zsh
ls -l tcxtool-state.json 2>/dev/null && shasum tcxtool-state.json
```

## 1. Quality gates 1–3 on the host

```zsh
dotnet build JoakimSoftware.TcxTool.slnx
dotnet format JoakimSoftware.TcxTool.slnx --verify-no-changes
dotnet test JoakimSoftware.TcxTool.slnx
```

**Expected**: no warnings or errors, no format changes, and every test passing.

## 2. Convert, and the CSV becomes current (US1)

```zsh
dotnet run --project src/JoakimSoftware.TcxTool.Web   # http://localhost:5169
```

1. Open `/convert` and upload a file from `data/tcx/`.
2. **Expected**: the green result lists the CSV, followed by `Current CSV: <name>`.
3. Check the state file:

   ```zsh
   cat tcxtool-state.json
   ```

   **Expected**: exactly `{ "csvFilename": "<name>" }`, in the format of contract
   [state-file.md](./contracts/state-file.md#content-written-exact).
4. Upload `tests/JoakimSoftware.TcxTool.Tests/Fixtures/malformed.tcx`. **Expected**: a red error,
   no "Current CSV" notice, and `tcxtool-state.json` unchanged.
5. Stop and restart the app, then open `/`. **Expected**: the Select CSV File card still shows the
   same current CSV (SC-005).

## 3. Select an earlier CSV on the home page (US2)

You need at least two CSVs in `data/csv/`. Convert a second sample, or copy the first one to a name
with an older date:

```zsh
cp data/csv/<name>.csv data/csv/2020-01-01_copy.csv
```

1. Open `/`. **Expected**: two cards side by side. The Select CSV File card lists the CSVs newest
   first, with the current one preselected.
2. Choose `2020-01-01_copy.csv` and click **Select**. **Expected**: `Current CSV: 2020-01-01_copy.csv`,
   and `cat tcxtool-state.json` names it.
3. `rm data/csv/2020-01-01_copy.csv`, then reload `/`. **Expected**: `Current CSV:
   2020-01-01_copy.csv (file missing)`, and no error page.
4. Corrupt the state file (`echo 'nope' > tcxtool-state.json`) and reload `/`. **Expected**: `No
   CSV selected yet.`, and a warning in the console naming the path and `not valid JSON`, not the
   content.
5. Narrow the window below the `sm` breakpoint. **Expected**: the cards stack.

## 4. About page (US3)

1. **Expected**: the navbar reads Home, Convert, Table, Histograms, Timeline, About.
2. Click **About**. **Expected**: the FR-003 content, formatted, with About highlighted in the
   navbar. The five links match the About table in [pages.md](./contracts/pages.md).

## 5. Quality gate 4: Compose

```zsh
docker compose -f compose.yaml -f compose.test.yaml up \
  --build --abort-on-container-exit --exit-code-from tests
echo $?
```

**Expected**: `0`. If port 8080 is busy, prefix the command with `TCXTOOL_HOST_PORT=8081`.

Optional container check: `docker compose up --build app`, convert a file at
`http://localhost:8080/convert`, then run:

```zsh
docker compose exec app cat /tmp/tcxtool/tcxtool-state.json
```

**Expected**: the state JSON.

## 6. Tests left the real state file alone (FR-020, SC-007)

```zsh
ls -l tcxtool-state.json 2>/dev/null && shasum tcxtool-state.json
git status --short                 # tcxtool-state.json must not appear (git-ignored)
```

**Expected**: the same result as before scenario 1, apart from changes you made by hand in
scenarios 2 and 3.

## Clean up

```zsh
docker compose down
rm -f tcxtool-state.json
rm -f data/csv/<each CSV you converted>.csv   # real GPS positions; leave files git already tracks
```
