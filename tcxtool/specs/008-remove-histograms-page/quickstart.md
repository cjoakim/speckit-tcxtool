# Quickstart: Validate Feature 008 (Remove the Histograms Page)

**Feature**: `specs/008-remove-histograms-page/spec.md` | **Contract**: [contracts/pages.md](./contracts/pages.md)

Run every command from `tcxtool/`. None of these steps reads or writes `data/` or
`tcxtool-state.json`.

## 1. Quality gates (SC-004)

```zsh
dotnet build JoakimSoftware.TcxTool.slnx
dotnet format JoakimSoftware.TcxTool.slnx --verify-no-changes
dotnet test JoakimSoftware.TcxTool.slnx
docker info > /dev/null && docker compose -f compose.yaml -f compose.test.yaml up \
  --build --abort-on-container-exit --exit-code-from tests; echo $?
```

**Expected**: all pass, and the last line is `0`. The test count is the feature 007 count, minus
the removed Histograms cases, plus the new route and unknown-address tests.

## 2. No leftovers (FR-006, SC-003)

```zsh
grep -rniI histogram src tests --exclude-dir=bin --exclude-dir=obj | grep -v '^src/JoakimSoftware.TcxTool.Web/wwwroot/lib/'
grep -ni histogram CLAUDE.md
```

**Expected**: the first command prints nothing. The second prints only the feature 003 and 008
rows of the history table.

## 3. The removed address and the navbar (FR-002, FR-003, SC-001, SC-002)

Start the app with scratch paths so your real files aren't touched:

```zsh
QS=$(mktemp -d)
TcxTool__CsvOutputDirectory=$QS TcxTool__StateFilePath=$QS/state.json \
  dotnet run --project src/JoakimSoftware.TcxTool.Web --no-launch-profile --urls http://127.0.0.1:5199
```

In a second terminal:

```zsh
for r in /histograms /Histograms /histograms/ /no-such-page; do
  curl -s -o /tmp/r.html -w "$r %{http_code} " http://127.0.0.1:5199$r
  grep -c '>Not Found</h1>' /tmp/r.html
done
curl -s http://127.0.0.1:5199/ | grep -o 'class="nav-link[^"]*"[^>]*>[^<]*<' | sed 's/.*>//; s/<$//'
```

**Expected**:
- Each of the four addresses prints `404 1`.
- The navbar prints `Home`, `Convert`, `Table`, `States`, `Timeline`, `About`, one per line.

Stop the app, and `rm -rf "$QS" /tmp/r.html`.

## 4. The other pages (FR-008)

Open `http://127.0.0.1:5199` in a browser, with the app from step 3 running. Visit each navbar
link.

**Expected**: each page looks as it did after feature 007. Only the link of the page you're on is
active, and no navbar link leads to a Not Found page.
