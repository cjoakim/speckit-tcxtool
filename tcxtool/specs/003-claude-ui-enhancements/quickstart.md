# Quickstart: Claude UI Enhancements

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

Run every command from `tcxtool/`. The markup is defined in
[contracts/ui-shell.md](./contracts/ui-shell.md) and [contracts/pages.md](./contracts/pages.md).

## Prerequisites

- .NET SDK 11.0.100 (`dotnet --version`)
- Docker Desktop running (`docker info` succeeds) for scenario 1's Compose gate
- PLog running at `http://localhost:8000` for scenario 3 (optional)

## Scenario 1: Quality gates (SC-001, SC-004)

```zsh
dotnet build JoakimSoftware.TcxTool.slnx
dotnet format JoakimSoftware.TcxTool.slnx --verify-no-changes
dotnet test JoakimSoftware.TcxTool.slnx
docker compose -f compose.yaml -f compose.test.yaml up \
  --build --abort-on-container-exit --exit-code-from tests; echo $?
docker compose -f compose.yaml -f compose.test.yaml down
```

**Expected**: no warnings and no format changes, 179 tests passing on the host and in the
container, and the gate prints `0`.

## Scenario 2: Navigate the app (US1, US3, SC-003)

```zsh
dotnet run --project src/JoakimSoftware.TcxTool.Web      # http://localhost:5169
```

1. Open `http://localhost:5169`. **Expected**: a dark navbar reading "TcxTool | Home Convert Table
   Histograms Timeline", the title and description, a "Convert TCX to CSV" card, and the light
   footer.
2. Click each navbar link. **Expected**: each page loads, its link is highlighted, and Table,
   Histograms, and Timeline each show only their name.
3. Open `http://localhost:5169/nope`. **Expected**: a styled "Not Found" page inside the shell.

## Scenario 3: Compare with PLog (SC-006)

Open `http://localhost:8000` next to `http://localhost:5169`. **Expected**: the same navbar
colour and height, container width, fonts, and footer.

## Scenario 4: Assets are local and tracked (FR-001, FR-011, SC-002)

```zsh
curl -s http://localhost:5169/convert | grep -oE '(href|src)="[^"]*bootstrap[^"]*"'
git -C .. check-ignore -v tcxtool/src/JoakimSoftware.TcxTool.Web/wwwroot/lib/bootstrap/css/bootstrap.min.css \
  || echo "not ignored"
git -C .. ls-files --others --cached tcxtool/src/JoakimSoftware.TcxTool.Web/wwwroot/lib
```

**Expected**: two relative `lib/bootstrap/...` URLs with fingerprints, "not ignored", and both
Bootstrap files listed.

## Scenario 5: Styled conversion result (US2)

On `/convert`, upload `data/tcx/24932322230.tcx`. **Expected**: a green alert listing
`2026-11-01_24932322230.csv: 19908 rows`. Submit with no file. **Expected**: a red alert. Delete
the generated CSV from `data/csv/` afterwards.

## Scenario 6: PLog untouched (FR-012, SC-005)

```zsh
git -C /Users/cjoakim/github/cj-plog status --short
```

**Expected**: no output.
