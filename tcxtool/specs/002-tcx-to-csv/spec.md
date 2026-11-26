# Feature Specification: TCX to CSV Conversion

**Feature Branch**: `main` (no feature branch created; spec directory is `specs/002-tcx-to-csv`)

**Created**: 2026-11-22

**Status**: Draft

**Input**: User description: "Implement parsing of TCX files to CSV. See examples TCX files in the data/tcx/ directory."

## Clarifications

### Session 2026-11-22

- Q: How does the user start a conversion, given that the CSV is saved to `data/csv/` on the machine running the app? → A: A web page where the user uploads a `.tcx` file; the app saves the CSV to `data/csv/` and shows the file name and row count.
- The CSV Output Data section below is authoritative for the delimiter, fields, field order, and output file name. It resolves the earlier open questions on delivery and output scope.
- Q: Should the CSV start with a header row of field names? → A: Yes; the field names exactly as listed in CSV Output Data, in that order, joined by `|`.
- Q: What should happen when an uploaded TCX file contains more than one activity? → A: Write one CSV per activity.
- Q: How should the CSV names be made unique when a file has more than one activity? → A: Multi-activity files name each CSV `<Date>_<HHMMSS>_<ActivityId>.csv`, using the activity's start time; single-activity files keep `<Date>_<ActivityId>.csv`.
- Q: How many decimal places should the calculated DistanceMiles and AltitudeFeet fields have? → A: Full precision, no rounding.
- Q: How should the app turn the uploaded file's name into the ActivityId when the name has spaces, punctuation, or other characters? → A: Keep letters, digits, `-`, and `_`; replace every other character with `_`; reject a name that is empty after removing the `.tcx` extension.
- Q: What should the default upload size limit be? → A: 50 MB (configurable).
- Q: What should happen when a text value from the file contains a `|`, a double quote, or a line break? → A: Replace each `|` with a space and remove line breaks; never quote fields.
- Q: When trackpoint times include fractions of a second, should ElapsedSeconds drop the fraction or round? → A: Neither; ElapsedSeconds is a decimal (float) field that keeps the fraction. ElapsedTime stays whole `HH:MM:SS`.

## User Scenarios & Testing *(mandatory)*

The user is a runner or cyclist who exports an activity from Garmin Connect as a TCX file and wants
its recorded data as a CSV, one row per recorded sample, so it can be opened in a spreadsheet or
used by the analysis and charting features that come later.

The sample files in `data/tcx/` show what real input looks like. Both are recordings of the same
marathon, made on two different watches. Each has 27 laps and about 19,900 trackpoints, and each is
13 to 15 MB. One file has no heart rate at all. The other has a few trackpoints with no position and
one with no speed. Per-sample speed, cadence, and power come from the Activity Extension, not from
the core TCX elements.

### Sample Input TCX Data

#### Header 

The Activity header describes the sport, date, distance. 
```
  ...
  <Activities>
    <Activity Sport="Running">
      <Id>2026-11-01T12:04:07.000Z</Id>
      <Lap StartTime="2026-11-01T12:04:07.000Z">
        <TotalTimeSeconds>710.319</TotalTimeSeconds>
        <DistanceMeters>1609.34</DistanceMeters>
        <MaximumSpeed>3.45199990272522</MaximumSpeed>
        <Calories>115</Calories>
        <AverageHeartRateBpm>
          <Value>127</Value>
        </AverageHeartRateBpm>
        <MaximumHeartRateBpm>
          <Value>146</Value>
        </MaximumHeartRateBpm>
        <Intensity>Active</Intensity>
        <TriggerMethod>Manual</TriggerMethod>
        ...
```

#### Footer

```
        ...
        </Track>
        <Extensions>
          <ns3:LX>
            <ns3:AvgSpeed>2.502000093460083</ns3:AvgSpeed>
            <ns3:AvgRunCadence>79</ns3:AvgRunCadence>
            <ns3:MaxRunCadence>94</ns3:MaxRunCadence>
            <ns3:AvgWatts>225</ns3:AvgWatts>
            <ns3:MaxWatts>339</ns3:MaxWatts>
          </ns3:LX>
        </Extensions>
      </Lap>
      <Creator xsi:type="Device_t">
        <Name>Forerunner 265S</Name>
        <UnitId>3628219234</UnitId>
        <ProductID>4258</ProductID>
        <Version>
          <VersionMajor>29</VersionMajor>
          <VersionMinor>5</VersionMinor>
          <BuildMajor>0</BuildMajor>
          <BuildMinor>0</BuildMinor>
        </Version>
      </Creator>
    </Activity>
  </Activities>
  <Author xsi:type="Application_t">
    <Name>Connect Api</Name>
    <Build>
      <Version>
        <VersionMajor>26</VersionMajor>
        <VersionMinor>22</VersionMinor>
        <BuildMajor>0</BuildMajor>
        <BuildMinor>0</BuildMinor>
      </Version>
    </Build>
    <LangID>en</LangID>
    <PartNumber>006-D2449-00</PartNumber>
  </Author>
</TrainingCenterDatabase>
```

#### Trackpoints 

```
        ...
        <Track>
          <Trackpoint>
            <Time>2026-11-01T12:06:55.000Z</Time>
            <Position>
              <LatitudeDegrees>35.781945157796144</LatitudeDegrees>
              <LongitudeDegrees>-78.64747817628086</LongitudeDegrees>
            </Position>
            <AltitudeMeters>114.5999984741211</AltitudeMeters>
            <DistanceMeters>344.3500061035156</DistanceMeters>
            <HeartRateBpm>
              <Value>122</Value>
            </HeartRateBpm>
            <Extensions>
              <ns3:TPX>
                <ns3:Speed>1.8849999904632568</ns3:Speed>
                <ns3:RunCadence>90</ns3:RunCadence>
                <ns3:Watts>253</ns3:Watts>
              </ns3:TPX>
            </Extensions>
          </Trackpoint>
          ...
```

### CSV Output Data

For each TCX Trackpoint, produce one CSV line that contains
the following fields, delimited by a vertical bar (i.e. '|').

- ActivityId      (the basename of the TCX file, i.e. - '24932322230')
- Date            (the YYYY-MM-DD portion of ID in Activity)
- DeviceName      (from Creator Name in the Footer)
- DeviceId        (from Creator UnitId in the Footer)
- Sport           (from Header Activity @Sport)
- TrackpointNum   (sequential Trackpoint number, calculated)
- Time            (from Trackpoint)
- ElapsedSeconds  (decimal value, calculated from elapsed time from the first Trackpoint Time)
- ElapsedTime     ('HH:MM:SS' value, calculated ElapsedSeconds)
- Latitude        (from Trackpoint Position LatitudeDegrees)
- Longitude       (from Trackpoint Position LongitudeDegrees)
- HeartRateBpm    (from Trackpoint HeartRateBpm Value)
- RunCadence      (from Trackpoint Extensions ns3:TPX)
- DistanceMeters  (from Trackpoint)
- DistanceMiles   (calculated from DistanceMeters)
- AltitudeMeters  (from Trackpoint)
- AltitudeFeet    (calculated from AltitudeMeters)

If the TCX element is not found, populate the CSV field with an empty value.

Note that you have to traverse the entire TCX file to obtain some of these
fields, such as DeviceName and DeviceId.

The parsed CSV file should be saved to file `<Date>_<ActivityId>.csv` in the
data/csv/ directory. For example, `data/csv/2026-11-01_24932322230.csv`.

---

### User Story 1 - Convert one TCX file to a CSV (Priority: P1)

The user opens the conversion page, uploads a TCX file, and the app saves a CSV to `data/csv/`
with one row per trackpoint, using the fields listed in CSV Output Data. The page shows the saved
file name and the number of rows written.

**Why this priority**: This is the feature. Every later feature (analysis, charts) reads its data
from this CSV.

**Independent Test**: Convert a small fixture with known values and compare the CSV to the expected
output, row by row and field by field.

**Acceptance Scenarios**:

1. **Given** a valid TCX file `12345.tcx` with one running activity whose Id is
   `2026-11-01T12:04:07.000Z`, 2 laps, and 10 trackpoints, **When** the user uploads it, **Then**
   `data/csv/2026-11-01_12345.csv` is written with a header row and 10 data rows in the order the trackpoints appear
   in the file, numbered 1 to 10 across both laps, and the page shows that file name and "10 rows".
2. **Given** a trackpoint with time, position, altitude, distance, heart rate, and extension run
   cadence, **When** it is converted, **Then** each value read from the file appears in its field
   exactly as written in the file, separated by `|`.
3. **Given** that the activity's Creator element comes after all its laps, **When** the file is
   converted, **Then** every row, including the first, carries the DeviceName and DeviceId.
4. **Given** a trackpoint 3,725 seconds after the first trackpoint, **When** it is converted,
   **Then** its ElapsedSeconds is `3725` and its ElapsedTime is `01:02:05`.
5. **Given** a trackpoint 3,725.6 seconds after the first trackpoint, **When** it is converted,
   **Then** its ElapsedSeconds is `3725.6` and its ElapsedTime is `01:02:05`.
6. **Given** one of the sample files in `data/tcx/`, **When** the user uploads it, **Then** the CSV
   has one data row per trackpoint in the file (19,908 rows for `24932322230.tcx` and 19,914 rows
   for `24964064020.tcx`).

---

### User Story 2 - Convert files that are missing optional data (Priority: P2)

Devices record different things. The user converts a file in which some or all trackpoints lack
position, heart rate, cadence, altitude, or extension data, and still gets a complete CSV.

**Why this priority**: Both sample files are missing some data. Without this, real files fail to
convert or produce shifted fields.

**Independent Test**: Convert fixtures that each omit one optional element, and check that the
matching fields are empty while all other fields are filled.

**Acceptance Scenarios**:

1. **Given** a file whose trackpoints have no heart rate, **When** it is converted, **Then**
   HeartRateBpm is empty in every row and every row still has all 17 fields.
2. **Given** a trackpoint with no position, **When** it is converted, **Then** its Latitude and
   Longitude are empty and the row is still written.
3. **Given** a trackpoint with no Activity Extension data, **When** it is converted, **Then** its
   RunCadence is empty and the conversion succeeds.
4. **Given** a trackpoint with no altitude or no distance, **When** it is converted, **Then** both the
   source field and its derived field (AltitudeFeet or DistanceMiles) are empty.
5. **Given** an activity with no Creator element, **When** it is converted, **Then** DeviceName and
   DeviceId are empty in every row.
6. **Given** an activity with no trackpoints, **When** it is converted, **Then** a CSV with only the
   header row is written and the page says that the file contains no trackpoints.

---

### User Story 3 - Get a clear error for a file that cannot be converted (Priority: P3)

The user uploads a file that is not a TCX file, is malformed, or is too large, and the page says why
it was rejected. No CSV is written.

**Why this priority**: Wrong files will be selected. A clear error is better than an empty or broken
CSV, but it matters less than converting good files.

**Independent Test**: Submit a malformed fixture, a well-formed non-TCX XML file, and a file over the
size limit, and check that each gives its own error and no file appears in `data/csv/`.

**Acceptance Scenarios**:

1. **Given** a file that is not well-formed XML, **When** the user uploads it, **Then** the page shows
   an error that says the file is malformed, and no CSV is written.
2. **Given** a well-formed XML file whose root is not a Training Center Database, **When** the user
   uploads it, **Then** the page shows an error that says the file is not a TCX file.
3. **Given** a file larger than the configured size limit (50 MB by default), **When** the user
   uploads it, **Then** it is rejected before any parsing starts, and the error states the limit.
4. **Given** a file that contains a document type definition or external entity references, **When**
   the user uploads it, **Then** the conversion fails and nothing outside the file is read.
5. **Given** a file that fails partway through parsing, **When** the conversion stops, **Then** no
   partial CSV is left in `data/csv/`.

---

### Edge Cases

- A file holds more than one activity: each activity is written to its own CSV, named
  `<Date>_<HHMMSS>_<ActivityId>.csv` from that activity's Id (for example
  `data/csv/2026-11-01_120407_12345.csv`). TrackpointNum and ElapsedSeconds restart for each
  activity. If any activity fails to convert, none of the CSVs is written.
- Two activities in one file have the same start time to the second: the conversion fails with an
  error that says the activities cannot be told apart, and no CSV is written.
- A lap has no track, or a track has no trackpoints: the lap adds no rows and the conversion
  continues.
- A trackpoint has no time: Time, ElapsedSeconds, and ElapsedTime are empty and the row is still
  written.
- An activity lasts 100 hours or more: ElapsedTime keeps counting hours past 99 (for example
  `100:00:00`).
- A trackpoint is timed before the activity's first timed trackpoint: ElapsedSeconds and
  ElapsedTime are written as negative values (for example `-3.5` and `-00:00:03`).
- An activity has no Id, or its Id is not a valid time: the conversion fails and no CSV is written.
- A numeric element holds text that is not a number: the conversion fails with an error that names
  the element and the trackpoint number, rather than writing a bad value.
- A text value contains `|` or a line break (for example a device name `Edge|1040`): the `|` becomes
  a space (`Edge 1040`), line breaks are removed, and the row keeps all 17 fields.
- Elements from other namespaces or unknown extensions appear: they are ignored.
- The uploaded file name has characters other than letters, digits, `-`, and `_`: each is replaced
  with `_` (`Morning Run (2).tcx` → ActivityId `Morning_Run__2_`), and the same value is used in the
  rows and in the output file name.
- The uploaded file name is empty once the `.tcx` extension is removed (for example `.tcx`): the
  upload is rejected with an error that says the file needs a name, and no CSV is written.
- The same file is converted twice: the output is identical both times, and the second conversion
  replaces the first CSV of the same name.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The app MUST provide a page where the user uploads a single `.tcx` file for conversion.
- **FR-002**: The system MUST reject a file larger than a configured size limit before parsing
  begins. The default limit MUST be 50 MB (52,428,800 bytes); a file of exactly the limit is
  accepted. The limit MUST be changeable through configuration without a code change.
- **FR-003**: The system MUST read TCX files in the Training Center Database v2 namespace and MUST
  read per-trackpoint run cadence from the Activity Extension v2 namespace.
- **FR-004**: The system MUST NOT process document type definitions or resolve external entities.
- **FR-005**: The system MUST produce one CSV data row per trackpoint, in the order the trackpoints
  appear in the file.
- **FR-006**: Each row MUST contain the 17 fields listed in CSV Output Data, in that order,
  separated by `|`. The first line MUST be a header row of those field names, exactly as listed and
  in the same order, separated by `|`.
- **FR-007**: ActivityId MUST be the uploaded file's name without any directory part and without its
  `.tcx` extension (in any letter case), with every character other than an ASCII letter, digit,
  `-`, or `_` replaced by `_`. If nothing remains, the upload MUST be rejected with an error.
- **FR-008**: Date MUST be the `YYYY-MM-DD` part of the activity's Id. DeviceName and DeviceId MUST
  come from the activity's Creator `Name` and `UnitId`, wherever Creator appears in the activity.
- **FR-009**: TrackpointNum MUST start at 1 for each activity and increase by 1 for each trackpoint
  across all of that activity's laps.
- **FR-010**: ElapsedSeconds MUST be the seconds from the activity's first trackpoint Time to the
  row's Time, as a decimal that keeps any fraction of a second. It is written as the shortest
  decimal that reads back as the same value, so whole seconds have no decimal point (`3725`) and
  fractions keep their digits (`3725.6`). ElapsedTime MUST be the same duration as `HH:MM:SS`,
  with the fraction of a second dropped (`01:02:05` for 3725.6 seconds).
- **FR-011**: Values copied from the file MUST be written exactly as they appear in the file, except
  as FR-011b requires. Derived
  values MUST use a period as the decimal separator regardless of the machine's regional settings.
- **FR-011a**: DistanceMiles MUST be DistanceMeters divided by 1609.344, and AltitudeFeet MUST be
  AltitudeMeters divided by 0.3048 (both exact definitions). Neither is rounded: each is written as
  the shortest decimal that reads back as the same calculated double-precision value.
- **FR-011b**: Fields MUST never be quoted. In any value written to a field, each `|` MUST be
  replaced with a space, and each carriage return and line feed MUST be removed. Double quotes are
  written unchanged.
- **FR-012**: A value missing from the file MUST be written as an empty field, and any field derived
  from it MUST also be empty. The row MUST still have all 17 fields.
- **FR-013**: RunCadence MUST be written as recorded, without conversion.
- **FR-014**: The system MUST fail with a typed, user-readable error, and write no CSV, when:
  - the file is malformed XML
  - its root is not the Training Center Database
  - it contains a document type definition
  - a numeric or time element holds an invalid value
  - an activity's Id is missing or not a valid time
  - two activities start in the same second
  - the uploaded name does not end in `.tcx`
  - writing the output fails
- **FR-015**: Each activity MUST be saved to its own CSV in `data/csv/`. A file with one activity is
  saved as `<Date>_<ActivityId>.csv`. A file with more than one activity saves each as
  `<Date>_<HHMMSS>_<ActivityId>.csv`, where `HHMMSS` is the time part of that activity's Id. A failed
  conversion MUST NOT leave any file, partial or complete, from that upload.
- **FR-016**: After a successful conversion, the page MUST show each saved file name and its number of
  data rows. After a failure, it MUST show the error.
- **FR-017**: An activity with no trackpoints MUST produce a CSV with only the header row, and the page MUST
  say that the file has no trackpoints.
- **FR-018**: Conversion failures MUST be logged with the file name and error type, never the file
  contents.
- **FR-019**: The system MUST NOT keep the uploaded TCX file after the conversion finishes.

### Key Entities *(include if feature involves data)*

- **Activity**: One recorded workout. Has an Id (its start time), a sport, the device that recorded
  it (Creator name and unit id), and an ordered list of laps.
- **Lap**: A segment of an activity. Has a start time and summary values (total time, distance,
  calories, heart rate, extension averages) and an ordered list of trackpoints.
- **Trackpoint**: One recorded sample. Has a time and, when recorded, a position, altitude,
  cumulative distance, heart rate, and run cadence.
- **CSV row**: The 17 fields in CSV Output Data for one trackpoint: values copied from the activity
  and trackpoint, plus the calculated TrackpointNum, ElapsedSeconds (decimal), ElapsedTime, DistanceMiles,
  and AltitudeFeet.
- **Conversion result**: Either one saved CSV per activity (file name, row count) or a typed error (malformed file,
  not a TCX file, too large, prohibited content, invalid value).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Each sample file in `data/tcx/` converts to a CSV in under 10 seconds on the
  developer's machine.
- **SC-002**: For every sample file and fixture, the CSV data row count equals the trackpoint count in
  the file (100%).
- **SC-003**: For every fixture, every field in the CSV matches the expected value exactly.
- **SC-004**: A CSV from a sample file opens in a common spreadsheet application with `|` chosen as the
  delimiter, with no import warnings, and every field lines up with the others in its column.
- **SC-005**: Each invalid fixture (malformed, not TCX, too large, prohibited content) gives its own
  error message, and none leaves a file in `data/csv/`.
- **SC-006**: Converting the same file twice gives byte-identical CSVs.

## Assumptions

- The user runs the tool locally, so the files and CSVs stay on their own machine. When the app runs
  in a container, `data/csv/` is a mounted directory.
- Garmin TCX run cadence counts one foot, so steps per minute are twice the value. The analysis
  feature can derive that.
- Lap summary values, speed, and power are parsed into the activity model where present but are not
  written to the CSV in this feature.
- Multisport sessions, courses, workouts, and history folders in TCX are out of scope. Only
  `Activities/Activity` is converted.
- The CSV uses UTF-8 without a byte order mark.
- Test fixtures are small, purpose-built files. The sample files in `data/tcx/` are used for manual
  runs and for the row-count and timing checks, not copied into the tests.
- Displaying the activity in the UI (summary, laps, charts) is a later feature.
