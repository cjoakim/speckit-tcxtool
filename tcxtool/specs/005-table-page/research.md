# Research: Table Page for the Current CSV

**Feature**: `005-table-page` | **Date**: 2026-11-25 | **Spec**: [spec.md](./spec.md)

The spec has no open clarification markers. The RunCadence scale (FR-015) was settled in its
Clarifications session: bands compare the CSV value doubled. This file records the design
decisions the spec leaves to the plan, and the facts checked on this machine.

## Facts checked on this machine

| Fact | How checked | Result |
|------|-------------|--------|
| Real CSV size and value ranges | `awk` over `data/csv/2026-11-01_24932322230.csv` (counts only; no rows copied here) | 19,908 data rows, 4.27 MB. HeartRateBpm is 70–189, all integers, never empty. RunCadence is 0–129, all integers, never empty; running values are mostly 80–99. 9 rows have an empty Time, position, distance, or altitude. |
| Static SSR cost of a 20,000-row table | Throwaway `dotnet new blazor --interactivity None --empty` app in the scratchpad, SDK 11.0.100, Release, Production environment. One page loops over 20,000 synthetic rows of 10 cells, with a band class on two cells, no child component per row. Timed with `curl`. | 20,000 rows: **0.18–0.26 s** total, 4.6 MB of HTML. 70,000 rows: 0.92 s, 16.2 MB. No response compression is configured, so the size on the wire is the same. |
| Forcing an unreadable CSV without `chmod` | Throwaway console app: hold the file open with `FileShare.None`, then open it again with `FileShare.ReadWrite \| FileShare.Delete`, as the loader will. Run on macOS and as root in `mcr.microsoft.com/dotnet/sdk:11.0`. | `File.Exists` is still true, and the second open throws `IOException` on both. .NET takes an advisory lock for `FileShare.None` on Unix, and root does not bypass it. So tests can force FR-006 with a lock (R9). |
| Forcing "deleted after the status check" | Same console app: a symbolic link named `gone.csv` pointing at a file that does not exist. Run on macOS and as root in `sdk:11.0`. | `File.Exists` returns true (so `CurrentCsvStore` reports `Present`), and the open throws `FileNotFoundException` on both. So tests can reach the loader's race branch (R2) with no seam. |
| Bootstrap 5.3.0 table colors | The vendored `wwwroot/lib/bootstrap/css/bootstrap.min.css` | Cells paint `background-color: var(--bs-table-bg)`, plus an inset `box-shadow` of `--bs-table-bg-state` (hover) or `--bs-table-bg-type` (striped) over it. `.table-info`, `.table-warning`, `.table-success`, and `.table-danger` work by setting `--bs-table-bg` and `--bs-table-color`. `.table-responsive` is only `overflow-x: auto`. |
| Contrast of the band colors | WCAG 2.x relative luminance, computed with Python | See R5: every band is at least 6.4:1 with its text color. |

## R1. Where the CSV is read

- **Decision**: a new `ActivityCsvReader` in the parser library, next to `ActivityCsvWriter`
  (namespace `JoakimSoftware.TcxTool.Parser.Csv`). It reads a `TextReader` and returns a typed
  `ActivityCsvReadResult`, either the rows or an `ActivityCsvError`. It never throws for bad
  content. It checks the header line against the writer's own `ActivityCsvWriter.Header` constant.
- **Rationale**:
  - The CSV contract (`specs/002-tcx-to-csv/contracts/csv-format.md`) already belongs to the
    library. Reading it next to the writer keeps the header and field count defined in one place.
  - Principle III: components do not open files or catch parse failures inline, and a typed result
    crosses the boundary. The reader needs no web host to test.
  - Histograms and Timeline will read the same CSV later, so the reader is not tied to this page.
- **Alternatives considered**:
  - A reader in the Web project: rejected. The header would be written twice, or the Web project
    would reach into the writer for it, and the reader could not be reused cleanly.
  - A CSV library: rejected. The format is one fixed header and `|` with no quoting, so
    `string.Split('|')` is enough, and the project avoids new packages.

## R2. How the page gets the current CSV's rows

- **Decision**: a singleton `CurrentActivityLoader` in a new Web folder `Analysis/` (namespace
  `JoakimSoftware.TcxTool.Web.Analysis`). `LoadAsync(CancellationToken)` does the following:
  1. Calls `CurrentCsvStore.ReadAsync` (FR-001). `None` and `Missing` return at once.
  2. Opens `<CSV directory>/<FileName>`, using `TcxToolPaths.CsvDirectory` to find the directory.
  3. Reads the file with `ActivityCsvReader`.
  4. Returns a `CurrentActivity` whose status is one of `NoCurrentCsv`, `Missing`, `Unreadable`,
     `WrongFormat`, `NoTrackpoints`, or `Loaded`.
- **Rationale**:
  - `CLAUDE.md` says later pages get the current CSV from `CurrentCsvStore.ReadAsync` and never read
    files themselves. The loader is that one step, shared by every later page.
  - `CurrentCsvStore` already rejects any name that is not a plain CSV name inside the CSV
    directory. So the loader joins a name that has already been checked, and does not repeat the
    name rule.
  - `CurrentCsvStore` stays the only reader and writer of `tcxtool-state.json`. The loader only
    reads CSV files, and never writes anything (FR-007).
- **Details**:
  - The file is opened with `FileShare.ReadWrite | FileShare.Delete` and `useAsync: true`, the same
    options the store uses for the state file.
  - `FileNotFoundException` or `DirectoryNotFoundException` after the status said `Present` means
    the file was deleted in between, so the result is `Missing`.
  - Any other `IOException`, or an `UnauthorizedAccessException`, gives `Unreadable`.
  - `OperationCanceledException` is not caught: an aborted request ends the render.
- **Naming**: the folder is `Analysis/`, not `Activity/`. `Components/Pages/Error.razor` calls
  `Activity.Current` (`System.Diagnostics.Activity`). C# looks up the enclosing namespaces before
  the file's `@using` directives, so a namespace `JoakimSoftware.TcxTool.Web.Activity` would capture
  that name and break the build. This is the same trap as `Convert` in feature 002. `Analysis` also
  names what the folder serves: the analysis pages (Table, Histograms, Timeline).
- **Alternatives considered**:
  - A `CurrentCsvStore.ReadRowsAsync` method: rejected. The store would grow a second job, and
    reading CSVs is not about the state file.
  - Reading in `TablePage.razor.cs`: rejected by Principle III and by `CLAUDE.md`.

## R3. Reader rules

- **Decision**:
  - The input is read line by line with `ReadLineAsync(cancellationToken)`. So `\n` and `\r\n` both
    end a line, and a final `\n` adds no empty row.
  - **Header**: line 1 MUST equal `ActivityCsvWriter.Header`, ordinal. An empty input, or any other
    first line, gives `HeaderMismatch` at line 1.
  - **Rows**: every later line is split on `|` and MUST have exactly 17 fields. A blank line counts
    as 1 field. The first line with a different count gives `FieldCount` with its 1-based line
    number, and no rows are returned (FR-005: no partial table).
  - **Values**: fields are kept exactly as written, with no trimming and no parsing (FR-013). An
    empty field is `""`, never null.
  - **Encoding**: the caller passes a `StreamReader` over UTF-8 that detects a byte order mark. The
    contract says there is none, but a file with a BOM is read the same as one without.
- **Rationale**: these rules are the CSV contract read back exactly. Every file the converter writes
  passes, and anything else is reported, never guessed at. Stopping at the first bad line keeps the
  error message about the file, not a list of lines.
- **Alternatives considered**:
  - Skipping bad lines and showing the rest: rejected by FR-005.
  - Loose header matching (case or order): rejected. Only TcxTool CSVs are in scope.

## R4. Rendering 20,000 rows

- **Decision**: one `TrackpointTable` component loops over all rows and writes the `<tr>` and `<td>`
  markup inline. There is no component per row, and no `Virtualize` or paging.
- **Rationale**:
  - The probe rendered 20,000 rows in under 0.3 s on the server. That leaves almost all of the
    SC-002 budget of 5 s for the browser to parse and lay out 4.6 MB of HTML.
  - A component per row would add 20,000 component instances to each render, for no benefit.
  - `Virtualize` needs an interactive render mode. Principle I and FR-020 keep the page static SSR,
    and the spec asks for every row (FR-012).
- **Risk noted**: Blazor's enhanced navigation (on by default through `blazor.web.js`) applies a
  navbar click to `/table` by patching the DOM, not by a full page load. The probe measured the
  server, not a browser. Quickstart scenario 4 checks SC-002 by hand in a browser, both through a
  navbar click and through a direct load. If the navbar route is noticeably slower, the fix is
  `data-enhance-nav="false"` on the Table link, which falls back to a normal page load. That fix is
  not planned up front, because it changes a contract from feature 003.

## R5. Band colors and how they are applied

- **Decision**: ten global classes in `wwwroot/app.css`, `hr-band-1` to `hr-band-6` and
  `cadence-band-1` to `cadence-band-4`. Each sets `--bs-table-bg` and `--bs-table-color`, the same
  mechanism Bootstrap's own `.table-info` and `.table-success` classes use. The table uses `table
  table-sm table-bordered`, with no `table-striped` and no `table-hover`.

  | Class | Band | Background | Text | Contrast |
  |-------|------|------------|------|----------|
  | `hr-band-1` | HR below 90, light blue | `#cff4fc` (Bootstrap info-subtle) | `#000` | 18.0:1 |
  | `hr-band-2` | HR 90 to below 120, light yellow | `#fff3cd` (warning-subtle) | `#000` | 19.0:1 |
  | `hr-band-3` | HR 120 to below 140, light orange | `#ffe5d0` (orange-100) | `#000` | 17.4:1 |
  | `hr-band-4` | HR 140 to below 160, dark orange | `#fd7e14` (orange) | `#000` | 8.2:1 |
  | `hr-band-5` | HR 160 to below 180, light red | `#f8d7da` (danger-subtle) | `#000` | 15.7:1 |
  | `hr-band-6` | HR 180 or more, dark red | `#b02a37` (red-600) | `#fff` | 6.5:1 |
  | `cadence-band-1` | below 20 spm, light blue | `#cff4fc` | `#000` | 18.0:1 |
  | `cadence-band-2` | 20 to below 150 spm, light yellow | `#fff3cd` | `#000` | 19.0:1 |
  | `cadence-band-3` | 150 to below 180 spm, light green | `#d1e7dd` (success-subtle) | `#000` | 16.2:1 |
  | `cadence-band-4` | 180 spm or more, darker green | `#146c43` (green-600) | `#fff` | 6.4:1 |

- **Rationale**:
  - Setting the table variables keeps the cell painted by Bootstrap's own rule, so borders and
    padding stay consistent and no `!important` is needed.
  - Striping and hover paint an inset shadow over `--bs-table-bg`, which would change the band
    colors. So the table uses neither, and the colors are exactly the ones above.
  - Every pair passes WCAG AA for normal text (4.5:1) with margin (FR-017). The colors come from
    Bootstrap 5.3's palette, so the page matches the app's look.
  - The two dark bands use white text. Black on them would be 3.2:1, which fails.
- **Alternatives considered**:
  - Scoped CSS (`TrackpointTable.razor.css`): rejected. Blazor adds a `b-xxxxxxxxxx` attribute to
    every element that component renders. With about 220,000 cells and rows, that adds about 2.6 MB
    of HTML for nothing.
  - Inline `style` attributes: rejected. They repeat the color on every cell and can't be checked in
    one place.
  - Bootstrap's `table-info` and similar classes: they cover only four of the eight colors, and the
    names don't say which band a cell is in.

## R6. Band rules in code

- **Decision**: a static `TrackpointBands` class in a new Web folder `Table/` (namespace
  `JoakimSoftware.TcxTool.Web.Table`):
  - `HeartRateClass(string value)` returns `hr-band-1` to `hr-band-6`, or null.
  - `RunCadenceClass(string value)` returns `cadence-band-1` to `cadence-band-4`, or null.
- **Rules**:
  - The value is parsed with `double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture)`.
  - A value that is empty, does not parse, or is not finite (`NaN`, `Infinity`, which
    `NumberStyles.Float` accepts) gets no band (FR-016).
  - RunCadence is multiplied by 2 before it is compared (FR-015). Each band is `lower <= v < upper`
    (FR-014). Negative values fall in band 1.
- **Rationale**: this is a pure function with no state, so every band edge can be tested without
  rendering. The thresholds are written once, as constants. The page only calls the class methods.
- **Alternatives considered**:
  - Thresholds in configuration: rejected. The spec fixes them, and nothing asks for them to change.
  - The rule in the parser library: rejected. Colors are about display, and the library has no
    display concerns.

## R7. Scroll container and sticky headings (FR-019)

- **Decision**: the table sits in `div.table-responsive.trackpoint-table-scroll`. `app.css` gives
  that div `max-height: 75vh` and `overflow-y: auto`, so it scrolls both ways. `thead th` gets
  `position: sticky; top: 0; z-index: 1`. Bootstrap's `table-light` on `thead` gives the headings
  an opaque background.
- **Rationale**:
  - `position: sticky` sticks to the nearest scrolling ancestor. `.table-responsive` is a scrolling
    ancestor (`overflow-x: auto`), so a header made sticky to the window would never stick.
  - Making the div scroll vertically as well gives the header a box to stick to. It also keeps the
    page's summary card and navbar in view while the rows scroll.
  - A narrow window scrolls the same div sideways (FR-019).
- **Alternatives considered**:
  - Repeating the header row every N rows: rejected. It adds markup, and the headings are still out
    of view between repeats.
  - JavaScript: rejected. CSS is enough, and the page needs no script.

## R8. Page states and messages

- **Decision**: `TablePage` renders its `h1` and then exactly one of the following, by
  `CurrentActivityStatus`. The fixed texts live in a `CurrentActivityMessages` class, following
  `CsvSelectionMessages`. The pages contract has the exact markup.

  | Status | Shows |
  |--------|-------|
  | `NoCurrentCsv` | `alert-info`, links to the home page and to `convert` |
  | `Missing` | `alert-warning` with the recorded name, link to the home page |
  | `Unreadable` | `alert-warning` with the name |
  | `WrongFormat` | `alert-warning` with the name |
  | `NoTrackpoints` | summary card (file name, dashes for the other values), then `alert-info` |
  | `Loaded` | summary card, then the table |

- **Rationale**:
  - The alert style matches `CurrentCsvNotice` and the convert page.
  - Showing the file name in every state with a file lets the developer tell which file to fix.
  - Error texts never include file content (FR-006).
- **Alternatives considered**: showing the summary card with dashes for `WrongFormat` and
  `Unreadable`. Rejected, because those dashes would look like real but empty activity values.

## R9. Testing approach

- **Decision**:
  - **Reader**: tests read every golden CSV in `Fixtures/Expected/`. Those are converter output
    whose values were computed independently, with Python, in feature 002. Each must read with the
    expected row count and with fields equal to the file's text. Bad inputs are written inline in
    the test (wrong header, 16 or 18 fields, a blank line, `\r\n` endings, empty input).
  - **Loader**: tests run against a `TempDirectory`, with a state file and CSVs written by a new
    `Support/ActivityCsvFixture` helper. The helper writes the contract header and rows with made-up
    coordinates, never real GPS positions. `Unreadable` is forced by holding the CSV open with
    `FileShare.None` for the duration of the call (the lock fact above), not with `chmod`. The
    race branch (`Missing` after `Present`) is forced with a dangling symbolic link (the link fact
    above).
  - **Bands**: a theory with every edge for both columns, plus decimals, empty, non-numeric, `NaN`,
    `Infinity`, and negative values.
  - **Components**: bUnit tests for `ActivitySummaryCard` and `TrackpointTable`, and for
    `TablePage` in every state. `TablePage` tests get their services through
    `BunitTcxToolServices`, which gains `CurrentActivityLoader`.
  - **HTTP**: through `TcxToolWebFactory`, write the state file and a CSV, then GET `/table`.
    Assert the markup, and that the CSV and the state file are unchanged byte for byte afterwards
    (FR-007).
  - **Performance**: an HTTP test renders a 20,000-row synthetic CSV and asserts every row is
    present in under 5 s (server side of SC-002; the probe measured 0.3 s). The browser side of
    SC-002 is quickstart scenario 4, because tests never drive a browser (Principle V).
  - **Styles**: a test reads `wwwroot/app.css` and checks that each of the ten band classes sets
    both variables, and that each pair's computed contrast is at least 4.5:1 (FR-017).
- **Rationale**: every requirement gets a test that fails before its code exists. No test touches
  the real `tcxtool/` files (FR-022). No large fixture is checked in.
- **Change to existing tests**:
  - `PlaceholderPageTests` drops `TablePage` from its theories, because the page now injects
    services. The new `TablePageTests` covers its heading and route instead.
  - `PlaceholderPageEndpointTests` keeps `/table`, because with no state file the page still serves
    `<title>Table</title>` and `>Table</h1>` (FR-018).

## R10. Size limits

- **Decision**: no new size limit for reading a CSV.
- **Rationale**:
  - The spec's assumption: CSVs come from the converter, whose 50 MiB upload limit bounds them.
  - Real data converts at about 0.3 bytes of CSV per byte of TCX (14 MB to 4.3 MB), so the largest
    real upload gives about 15 MB of CSV, or about 70,000 rows.
  - The probe rendered 70,000 rows in 0.9 s. The reader streams line by line and keeps only the
    rows, so memory grows with the row count, not with a whole-file string.
- **Alternatives considered**: a byte ceiling with a "too large" state. Deferred, because nothing in
  the spec asks for it. If a hand-copied, huge `.csv` becomes a real case, it would be a new status
  in `CurrentActivityStatus` and one check in the loader.

## R11. Logging

- **Decision**: `CurrentActivityLoader` logs through `ILogger<CurrentActivityLoader>`, using
  `LoggerMessage` source generation (as `CurrentCsvStore` does):
  - `Unreadable`: Warning, with the file name and the exception type name.
  - `WrongFormat`: Warning, with the file name, the error kind, and the line number.
  - The other statuses are not logged.
- **Rationale**: the constitution says parse failures log the file name and error type, and never
  the raw body. The CSVs hold real GPS positions, so no field text is logged, and neither is the
  exception message, which could echo a path or content.
