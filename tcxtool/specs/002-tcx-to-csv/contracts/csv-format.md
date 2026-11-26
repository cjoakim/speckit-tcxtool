# Contract: CSV Output Format

This is the file contract that the later analysis and charting features read. It restates the
spec's CSV Output Data section in testable form. If the two ever disagree, the spec wins.

## File

| Property | Value |
|----------|-------|
| Location | `TcxTool:CsvOutputDirectory` (host: `tcxtool/data/csv/`, container: `/data/csv`) |
| Name, one activity | `<Date>_<ActivityId>.csv`, for example `2026-11-01_24932322230.csv` |
| Name, several activities | `<Date>_<HHmmss>_<ActivityId>.csv`, for example `2026-11-01_120407_24932322230.csv` |
| Encoding | UTF-8, no byte order mark |
| Line ending | `\n` after every line, including the last |
| Delimiter | `\|`, never quoted, never escaped |
| Lines | 1 header line plus 1 line per trackpoint, in document order |
| Existing file | Replaced |

## Fields

Every line has exactly 17 fields, which means 16 `|` characters.

| # | Header | Value | Empty when |
|---|--------|-------|------------|
| 1 | `ActivityId` | Sanitized upload name (data-model) | never |
| 2 | `Date` | `yyyy-MM-dd` of the activity `Id` | never |
| 3 | `DeviceName` | `Creator/Name` text | no Creator or Name |
| 4 | `DeviceId` | `Creator/UnitId` text | no Creator or UnitId |
| 5 | `Sport` | `Activity/@Sport` text | attribute missing |
| 6 | `TrackpointNum` | 1, 2, 3, … per activity | never |
| 7 | `Time` | `Trackpoint/Time` text as written | no Time |
| 8 | `ElapsedSeconds` | seconds since the first timed trackpoint, shortest decimal (`3725`, `3725.6`) | no Time |
| 9 | `ElapsedTime` | `HH:MM:SS`, whole seconds, hours ≥ 2 digits (`01:02:05`, `100:00:00`) | no Time |
| 10 | `Latitude` | `LatitudeDegrees` text | no Position |
| 11 | `Longitude` | `LongitudeDegrees` text | no Position |
| 12 | `HeartRateBpm` | `HeartRateBpm/Value` text | no HeartRateBpm |
| 13 | `RunCadence` | `Extensions/TPX/RunCadence` text | no extension RunCadence |
| 14 | `DistanceMeters` | `DistanceMeters` text | no DistanceMeters |
| 15 | `DistanceMiles` | DistanceMeters ÷ 1609.344, shortest decimal | DistanceMeters empty |
| 16 | `AltitudeMeters` | `AltitudeMeters` text | no AltitudeMeters |
| 17 | `AltitudeFeet` | AltitudeMeters ÷ 0.3048, shortest decimal | AltitudeMeters empty |

**Shortest decimal**:
- the fewest digits that parse back to the same `double`
- no exponent, period as the decimal separator
- no trailing `.0`, and `-0` written as `0`
- examples: `0.0009444842003490327`, `0.000006213711922373339`

**Text cleaning** applies to every field: each `|` becomes a space, and each CR or LF is removed.

## Header line (exact)

```text
ActivityId|Date|DeviceName|DeviceId|Sport|TrackpointNum|Time|ElapsedSeconds|ElapsedTime|Latitude|Longitude|HeartRateBpm|RunCadence|DistanceMeters|DistanceMiles|AltitudeMeters|AltitudeFeet
```

## Example row (first trackpoint of `24932322230.tcx`)

```text
24932322230|2026-11-01|Forerunner 265S|3628219234|Running|1|2026-11-01T12:04:07.000Z|0|00:00:00|35.785001115873456|-78.64722914993763|83|59|0.0|0|103.5999984741211|339.8950081171952
```

`DistanceMeters` keeps the file's `0.0`, while the derived `DistanceMiles` is `0`.

## Verification

- `Fixtures/Expected/*.csv` golden files are compared byte for byte (SC-003, SC-006).
- Field count per line is checked on every fixture (SC-004).
