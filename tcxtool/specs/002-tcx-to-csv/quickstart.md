# Quickstart: TCX to CSV Conversion

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

These scenarios check that the feature works end to end. Run every command from `tcxtool/`. The
formats are defined in [contracts/csv-format.md](./contracts/csv-format.md) and
[contracts/convert-page.md](./contracts/convert-page.md).

## Prerequisites

- .NET SDK 11.0.100 (`dotnet --version`)
- Docker Desktop running (`docker info` succeeds) for scenario 4
- The sample files `data/tcx/24932322230.tcx` and `data/tcx/24964064020.tcx`

## Scenario 1: Quality gates

```zsh
dotnet build JoakimSoftware.TcxTool.slnx
dotnet format JoakimSoftware.TcxTool.slnx --verify-no-changes
dotnet test JoakimSoftware.TcxTool.slnx
```

**Expected**: no warnings and no format changes. All tests pass, including the parser, converter,
bUnit, and `ConvertEndpointTests` suites.

## Scenario 2: Convert the sample files on the host (SC-001, SC-002, SC-004)

```zsh
dotnet run --project src/JoakimSoftware.TcxTool.Web      # http://localhost:5169
```

1. Open `http://localhost:5169`, follow the link to `/convert`, and upload
   `data/tcx/24932322230.tcx`.
2. **Expected**: within 10 seconds the page lists `2026-11-01_24932322230.csv: 19908 rows`.
3. Repeat with `24964064020.tcx`. **Expected**: `2026-11-01_24964064020.csv: 19914 rows`.
4. Check the files:

```zsh
ls data/csv/
for f in data/csv/2026-11-01_*.csv; do
  echo "$f: $(($(wc -l < $f) - 1)) data rows"
  awk -F'|' 'NF != 17 { bad++ } END { print (bad ? bad " bad lines" : "all lines have 17 fields") }' "$f"
done
head -2 data/csv/2026-11-01_24932322230.csv
cut -d'|' -f12 data/csv/2026-11-01_24964064020.csv | sort -u | head -3   # header + empty: no heart rate
```

**Expected**:
- 19908 and 19914 data rows, and every line has 17 fields.
- The header line matches the contract.
- The first data row matches the example row in the CSV format contract.
- `24964064020`'s HeartRateBpm field holds only the header and empty values.
- Converting the same file again produces an identical file (`shasum` before and after).

Open one CSV in a spreadsheet, choosing `|` as the delimiter. Every column should line up with no
import warning (SC-004).

## Scenario 3: Rejections

On `/convert`:

1. Click **Convert** with no file. **Expected**: `Choose a .tcx file to convert.`
2. Upload a text file renamed to `notes.txt`. **Expected**: the not-a-TCX-file-name error.
3. Upload a 60 MB file named `big.tcx`, made with `head -c 62914560 /dev/zero >
   /tmp/big.tcx`. **Expected**: an error stating the 50 MB limit, and nothing new in `data/csv/`.

## Scenario 4: Container and Compose gate

```zsh
docker compose -f compose.yaml -f compose.test.yaml up \
  --build --abort-on-container-exit --exit-code-from tests; echo $?
```

**Expected**: exit code `0`.

```zsh
docker compose up --build app        # http://localhost:8080
```

Upload `data/tcx/24932322230.tcx` at `http://localhost:8080/convert`. **Expected**: the CSV appears
in the host's `data/csv/`, written through the `/data/csv` bind mount by the non-root user. Stop
with `docker compose down`.

## Cleanup

`data/csv/` is git-ignored. Delete its CSVs when you are finished. They contain real GPS positions.
