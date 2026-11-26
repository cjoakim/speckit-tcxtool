# Test fixtures

Small, purpose-built `.tcx` files for the parser, converter, and endpoint tests (constitution
Principle V). All coordinates, devices, and values are synthetic.

Never copy the real exports from `data/tcx/` here. They are large and contain real GPS positions.

## Success fixtures (each has a golden CSV in `Expected/`)

| Fixture | Purpose |
|---------|---------|
| `run-two-laps.tcx` | Run with 2 laps and 10 trackpoints, all fields, Activity Extension `TPX` values, Creator after the laps, unknown elements in unknown namespaces, one trackpoint 3725 s after the first |
| `ride-positions.tcx` | `Biking` activity with positions on every trackpoint and no extensions |
| `multi-activity.tcx` | Two activities on the same date with different start times (two golden CSVs) |
| `fractional-times.tcx` | Fractional-second times, a trackpoint before the reference, device name `Edge\|1040`, an Id with a `-05:00` offset |
| `missing-optional.tcx` | No Creator; trackpoints missing time, heart rate, position, half a position, extensions, altitude, distance; an empty element |
| `no-trackpoints.tcx` | One lap with an empty track and one with no track (header-only golden CSV) |
| `run-states.tcx` | Feature 006 states: TCX cadence 0, 1, 74, 75, 90, and missing; a one-row StateSet; an untimed row inside a set; a set with no distance; a gap of more than 1 s at a set boundary; fractional seconds; rows missing position, altitude, or heart rate |

## Error fixtures

| Fixture | Expected error |
|---------|----------------|
| `malformed.tcx` | `Malformed` |
| `not-tcx.tcx` | `NotTcx` (well-formed, `<gpx>` root) |
| `dtd.tcx` | `ProhibitedContent` (DOCTYPE with an external entity) |
| `invalid-number.tcx` | `InvalidValue` (`HeartRateBpm` at trackpoint 3) |
| `invalid-number-nan.tcx` | `InvalidValue` (`AltitudeMeters` is `NaN`) |
| `invalid-time.tcx` | `InvalidValue` (`Time` at trackpoint 2) |
| `missing-id.tcx` | `InvalidActivityId` |
| `duplicate-start.tcx` | `DuplicateActivityStart` |

## Golden files

`Expected/*.csv` hold the exact bytes the converter must write: UTF-8 without BOM, `\n` after every
line, 19 `|`-separated fields (`specs/006-activity-states/contracts/csv-format.md`, which replaces
the field list of `specs/002-tcx-to-csv/contracts/csv-format.md`).

Each `Expected/<stem>.csv` has a States golden, `Expected/<stem>.states.csv`, with 16 fields and one
line per StateSet (`specs/007-timeline-page/contracts/states-csv-format.md`, which replaces the field
list of `specs/006-activity-states/contracts/states-csv-format.md`). The script builds it from the
activity rows it has just computed, as strings: end values (including ActivityDistanceMiles and
ActivityElapsedSeconds) are copied, StateTrackpointCount is the set's row count, means are an explicit
`+=` loop (never `sum()`, which is compensated in Python 3.12 and later), and StateDistanceMiles is
one subtraction.

They were produced independently of the C# code under test. `generate_success_fixtures.py`
writes each success fixture and its golden CSVs, computing derived values with Python's shortest
round-trip `repr`, expanded to a plain decimal, and State and StateSeconds with `timedelta`
arithmetic. It reproduces the checked-in files byte for byte.
Run it from `tcxtool/`:

```zsh
python3 -I tests/JoakimSoftware.TcxTool.Tests/Fixtures/generate_success_fixtures.py \
  tests/JoakimSoftware.TcxTool.Tests/Fixtures run-two-laps ride-positions multi-activity \
  fractional-times missing-optional no-trackpoints run-states
```

To change a success fixture, edit the script and rerun it. Never regenerate a golden file by
running the converter. The script doesn't produce the error fixtures; edit those directly.

## Container

The `tests` service in `compose.test.yaml` mounts this directory, including `Expected/`, read-only
into the test container (Principle VII).
