# Contract: Application State File (changes)

**Requirements**: FR-013 to FR-016 | **Stories**: US2

This contract adds to `specs/004-about-and-csv-state/contracts/state-file.md`. Everything not named
here (file location, size limit, write procedure, BOM handling, and the `csvFilename` read rules) is
unchanged. If this contract and the spec disagree, the spec wins.

## Content written (exact)

With a States CSV, 2-space indentation, UTF-8 without a BOM, and `\n` after every line, including the
closing brace:

```json
{
  "csvFilename": "2026-11-01_24932322230.csv",
  "statesCsvFilename": "2026-11-01_24932322230.states.csv"
}
```

Without one, the content is exactly feature 004's, with no `statesCsvFilename` line.

## When each property is written

| Event | `csvFilename` | `statesCsvFilename` |
|-------|---------------|---------------------|
| A conversion succeeds | the first saved CSV (feature 004) | its States CSV, which the conversion has just written (FR-013) |
| The developer selects a CSV on the home page | the selected CSV | `StatesCsvFileName.For(selected)` when that file exists in the CSV folder; otherwise left out (FR-014) |

The two are always written together, so the app never writes a `statesCsvFilename` that belongs to
another CSV.

## Name rule (changed)

`CsvFileNameRule.IsPlainCsvName` now also returns false for any name that ends in `.states.csv`,
ignoring case (research R4). So a States CSV:

- is not in the home page's Select CSV File list,
- is rejected by the selection form as `InvalidName`, and
- read from `csvFilename`, gives `None` with the existing warning `csvFilename is not a plain CSV
  name`.

## Read rules (added)

These run only after `csvFilename` has given `Present` or `Missing`. They never change that result.

| Situation | `CurrentCsv.StatesFileName` | Log |
|-----------|-----------------------------|-----|
| no `statesCsvFilename` property | null | none |
| `statesCsvFilename` is a string equal (ordinal) to `StatesCsvFileName.For(csvFilename)` | that name | none |
| any other value: not a string, another name, a path | null | warning: `statesCsvFilename does not match csvFilename` |

Whether the named file exists is not checked here. `CurrentStatesLoader` reports a missing file
(contracts/pages.md). As before, warnings carry the full state file path and a fixed phrase, never
the file's content.
