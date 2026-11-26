# Contract: States CSV Format

**Requirements**: FR-007 to FR-012 | **Stories**: US2

The file the converter writes next to each activity CSV, and that the States page reads. If this
contract and the spec disagree, the spec wins.

## File

| Property | Value |
|----------|-------|
| Location | the activity CSV's folder, `TcxTool:CsvOutputDirectory` |
| Name | the activity CSV's name with the final `.csv` replaced by `.states.csv`, for example `2026-11-01_24932322230.states.csv` or `2026-11-01_120407_24932322230.states.csv` |
| Encoding, line ending, delimiter, text cleaning, replacement | as the activity CSV (contracts/csv-format.md) |
| Lines | 1 header line plus 1 line per StateSet, in file order; header only when the activity has no trackpoints |
| Written | in the same conversion as its activity CSV, with the same temp-then-move procedure; a failure leaves neither file (research R6) |
| Listed or selectable on the home page | never (FR-015) |

## Fields

Every line has exactly 13 fields, which means 12 `|` characters. "The set" is the StateSet the row
summarizes, and "before the set" means the activity CSV rows above its first row. Every value comes
from the activity CSV's written values (FR-010).

| # | Header | Value | Empty when |
|---|--------|-------|------------|
| 1 | `ActivityId` | the set's ActivityId | never |
| 2 | `State` | the set's State | never |
| 3 | `StateEndDate` | `Time` of the set's last row with a Time, as written | no row in the set has a Time |
| 4 | `StateEndTrackpointNum` | `TrackpointNum` of the set's last row | never |
| 5 | `StateElapsedSeconds` | `StateSeconds` of the set's last row with one, as written | no row in the set has one |
| 6 | `ActivityElapsedTime` | `ElapsedTime` of the set's last row with one, as written | no row in the set has one |
| 7 | `AverageLatitude` | mean of the set's `Latitude` values | no row in the set has one |
| 8 | `AverageLongitude` | mean of the set's `Longitude` values | no row in the set has one |
| 9 | `AverageHeartRateBpm` | mean of the set's `HeartRateBpm` values | no row in the set has one |
| 10 | `AverageRunCadence` | mean of the set's (doubled) `RunCadence` values | no row in the set has one |
| 11 | `DistanceMiles` | the set's last `DistanceMiles` minus the last `DistanceMiles` before the set (0 when none) | no row in the set has one |
| 12 | `AverageAltitudeMeters` | mean of the set's `AltitudeMeters` values | no row in the set has one |
| 13 | `AverageAltitudeFeet` | mean of the set's `AltitudeFeet` values | no row in the set has one |

- **Mean**: the values parsed as `double` (invariant culture, finite only), added left to right,
  divided by their count, and written as a shortest decimal.
- **Difference**: one `double` subtraction of the two parsed values, written as a shortest decimal.
- Because each set starts where the previous one ended, the sets' StateElapsedSeconds add up to the
  activity's last ElapsedSeconds, and their DistanceMiles add up to its last DistanceMiles, apart
  from the last-digit rounding of each subtraction.

## Header line (exact)

```text
ActivityId|State|StateEndDate|StateEndTrackpointNum|StateElapsedSeconds|ActivityElapsedTime|AverageLatitude|AverageLongitude|AverageHeartRateBpm|AverageRunCadence|DistanceMiles|AverageAltitudeMeters|AverageAltitudeFeet
```

## Worked example (the spec's US2 scenario, synthetic values)

38 walking rows (ElapsedSeconds 0–37), then 113 running rows (38–150), one per second:

| State | StateEndTrackpointNum | StateElapsedSeconds | ActivityElapsedTime |
|-------|-----------------------|---------------------|---------------------|
| walking | 38 | 37 | 00:00:37 |
| running | 151 | 113 | 00:02:30 |

37 + 113 = 150, the activity's last ElapsedSeconds.

## Verification

- `Fixtures/Expected/*.states.csv` are compared byte for byte. `generate_success_fixtures.py`
  calculates them with explicit `+=` loops and `timedelta` arithmetic, never Python's compensated
  `sum()` (research R12).
- `StatesCsvReader` reads every golden back to the same field values.
- Converting twice gives byte-identical files (FR-012).
