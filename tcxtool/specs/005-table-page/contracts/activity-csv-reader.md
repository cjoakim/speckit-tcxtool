# Contract: Reading an Activity CSV

**Requirements**: FR-004, FR-005, FR-013 | **Builds on**:
[002 csv-format.md](../../002-tcx-to-csv/contracts/csv-format.md)

`ActivityCsvReader.ReadAsync(TextReader, CancellationToken)` in the parser library reads back
exactly the format `ActivityCsvWriter` writes. The writer's contract stays the authority on the
format. This contract says what the reader accepts and how it reports the rest. The types are in
[data-model.md](../data-model.md#parser-library-srcjoakimsoftwaretcxtoolparsercsv).

## Accepted input

| Rule | Detail |
|------|--------|
| Lines | Read with `TextReader.ReadLineAsync`, so `\n`, `\r\n`, and `\r` all end a line. A final line ending adds no line. |
| Line 1 | Exactly `ActivityCsvWriter.Header`, compared ordinally |
| Lines 2 to n | Each is split on `\|` into exactly 17 fields, in contract order |
| Fields | Kept exactly as written. No trimming, unquoting, or parsing. An empty field is `""`. |
| Byte order mark | Handled by the caller's `StreamReader` (`detectEncodingFromByteOrderMarks: true`). A BOM is not part of line 1. |

## Results

| Input | Result |
|-------|--------|
| Header, then n ≥ 0 valid lines | `Success`, with n rows in file order |
| Empty input (no line at all) | `Failure(HeaderMismatch, line 1)` |
| Line 1 differs from the header in any way (case, order, missing or extra field, trailing space) | `Failure(HeaderMismatch, line 1)` |
| First data line k with a field count other than 17 (including a blank line, which is 1 field) | `Failure(FieldCount, line k)`, with no rows, even when the earlier lines were valid |

The reader stops at the first error.

## Never

- It never throws for content. Bad content is a `Failure`.
- It never reads past the first error.
- It never logs. The caller logs the file name and the error kind (research R11).
- It has no reference to ASP.NET Core or Blazor (Principle III; `ParserIndependenceTests`).

## Round trip

Every golden CSV under `tests/JoakimSoftware.TcxTool.Tests/Fixtures/Expected/` reads as `Success`.
Its row count is the file's line count minus 1, and each row's fields joined with `|` equal that
line of the file. This makes the reader agree with the writer's verified output.

| Golden file | Rows |
|-------------|------|
| `2026-11-01_071500_multi-activity.csv` | 3 |
| `2026-11-01_183005_multi-activity.csv` | 3 |
| `fractional-times.csv` | 5 |
| `missing-optional.csv` | 8 |
| `no-trackpoints.csv` | 0 |
| `ride-positions.csv` | 4 |
| `run-two-laps.csv` | 10 |

## Verified by

`Parser/ActivityCsvReaderTests`
