# Contract: Application State File

**Requirements**: FR-005 to FR-010, FR-020, FR-021 | **Stories**: US1, US2

This is the file contract later analysis features rely on, through `CurrentCsvStore` (FR-009). If
it and the spec ever disagree, the spec wins.

## File

| Property | Value |
|----------|-------|
| Name | `tcxtool-state.json` |
| Setting | `TcxTool:StateFilePath`; a relative path resolves against the content root |
| Host (`dotnet run`, Development) | `../../tcxtool-state.json` → `tcxtool/tcxtool-state.json` |
| Default (`appsettings.json`) | `tcxtool-state.json` |
| Container (`compose.yaml`) | `/tmp/tcxtool/tcxtool-state.json`, lost when the container is removed |
| Tests | inside a `TempDirectory`, never the paths above |
| Source control | git-ignored (`tcxtool/.gitignore`) |
| Docker build context | excluded (`.dockerignore`) |

## Content written (exact)

The app writes exactly this, with 2-space indentation, UTF-8 without a BOM, and `\n` line endings,
including after the closing brace:

```json
{
  "csvFilename": "2026-11-01_24932322230.csv"
}
```

- `csvFilename` is a file name only: no directory, and always a name that passes the rule below.
- The value refers to a file in `TcxTool:CsvOutputDirectory`.
- Characters outside ASCII are written as `System.Text.Json`'s default escapes. Converter output
  names are ASCII, so in practice none occur.

## Write procedure

1. Create the parent directory if it does not exist.
2. Write the content to `<path>.<32 hex chars>.tmp` in the same directory.
3. `File.Move(temp, path, overwrite: true)`.
4. If step 1, 2, or 3 fails, delete the temp file if it exists, log a warning, and return
   `WriteFailed`. The previous state file is untouched.

## Read rules

| Situation | Result | Log |
|-----------|--------|-----|
| file does not exist | `None` | none |
| file is larger than 65,536 bytes | `None` | warning: `larger than 64 KiB` |
| not valid JSON, or the root is not an object | `None` | warning: `not valid JSON` |
| no `csvFilename` property, or its value is not a string | `None` | warning: `no csvFilename string` |
| `csvFilename` fails the name rule | `None` | warning: `csvFilename is not a plain CSV name` |
| valid name, and the file exists in the CSV directory | `Present(name)` | none |
| valid name, and the file is missing | `Missing(name)` | none |
| the file cannot be opened (permissions, I/O) | `None` | warning: `could not be read` |

Extra properties are ignored on read. Warnings carry the full path and the fixed reason phrase
only, never the file's content.

## Name rule (`CsvFileNameRule.IsPlainCsvName`)

A name is accepted only when all of these hold:

1. It is not null, empty, or whitespace only.
2. `Path.GetFileName(name) == name`, and it contains neither `/` nor `\`.
3. It is not `.` or `..`, and `Path.IsPathRooted(name)` is false.
4. It contains no character from `Path.GetInvalidFileNameChars()`.
5. It ends with `.csv` (ordinal, case-sensitive) and has at least one character before it.

When a name is selected, `Path.GetFullPath(Path.Combine(csvDirectory, name))` must also start with
the CSV directory's full path plus a separator.

| Input | Accepted |
|-------|----------|
| `2026-11-01_24932322230.csv` | yes |
| `2026-11-01_120407_24932322230.csv` | yes |
| `notes.txt` | no (extension) |
| `REPORT.CSV` | no (case) |
| `.csv` | no (nothing before the extension) |
| `../secrets.csv` | no (directory part) |
| `/etc/passwd.csv` | no (rooted) |
| `sub/x.csv` or `sub\x.csv` | no (separator) |
| empty or whitespace | no (`NoneChosen` when selecting) |

## Verified by

- `Web/State/CsvFileNameRuleTests`: the table above
- `Web/State/CurrentCsvStoreTests`: every read rule, the exact bytes written, the temp-file cleanup on
  failure, and that failed selections leave the state unchanged
- `Web/AppSettingsTests`: the `StateFilePath` defaults
- `Architecture/TestIsolationTests`: no test builds a factory that could reach the real file
