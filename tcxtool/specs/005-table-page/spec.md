# Feature Specification: Table Page for the Current CSV

**Feature Branch**: `005-table-page` (feature id only; no git branch is created by `/speckit-specify`)

**Created**: 2026-11-25

**Status**: Draft

**Input**: User description, from the draft file `docs/spec-005-draft.md` (passed to
`/speckit-specify` as `@../docs/spec-005-draft.md`). The draft asks for the "Table" page to be
built out:

1. **Data source**: the current CSV file (feature 004) is the page's data source.
2. **Header Section**: a box at the top of the page, like the boxes on the Home page, showing the
   CSV file name, ActivityId, Date, DeviceName, DeviceId, and Sport.
3. **Data Table Section**: every CSV data row in a table with the columns TrackpointNum, Time,
   ElapsedSeconds, ElapsedTime, HeartRateBpm, RunCadence, DistanceMiles, Latitude, Longitude, and
   AltitudeFeet.
4. **RunCadence color coding**: light blue below 20; light yellow from 21 to 149; light green from
   150 to 179; darker green at 180 or more.
5. **HeartRateBpm color coding**: light blue below 90; light yellow from 90 to 119; light orange
   from 120 to 139; dark orange from 140 to 159; light red from 160 to 179; dark red at 180 or more.

## Clarifications

### Session 2026-11-25

- Q: The draft's RunCadence thresholds are in full steps per minute, but the CSV's RunCadence is
  Garmin's per-foot value (about half). Which number do the bands compare? → A: The CSV value
  doubled (steps per minute), against the draft's thresholds. The cell still shows the CSV value as
  written (FR-013, FR-015).

## User Scenarios & Testing *(mandatory)*

The user is the developer who maintains `JoakimSoftware.TcxTool` and runs it on their own machine.
They have converted a Garmin activity, or selected an earlier CSV on the home page, so the app has
a current CSV. They want to read that activity's trackpoints one by one, and to see at a glance
where their heart rate and cadence were high or low.

### User Story 1 - See which activity the Table page is showing (Priority: P1)

The developer opens the Table page from the navbar. A box at the top names the current CSV and
the activity it holds: the activity id, date, device name, device id, and sport.

**Why this priority**: It confirms that the page reads the current CSV and shows the developer
which activity they are looking at. It is the smallest useful slice, and every later slice sits
under it.

**Independent Test**: Select a known CSV on the home page, then open the Table page. The box shows
that CSV's file name and the activity values from its first data row.

**Acceptance Scenarios**:

1. **Given** a current CSV whose file exists, **When** the developer opens the Table page, **Then**
   a box at the top shows the CSV file name, ActivityId, Date, DeviceName, DeviceId, and Sport, each
   with a label, and the box has the same card look as the boxes on the Home page.
2. **Given** a CSV whose first data row has an empty DeviceName or DeviceId, **When** the page
   renders, **Then** the label is still shown and its value is visibly blank (a dash), not missing.
3. **Given** no current CSV, **When** the developer opens the Table page, **Then** the page says no
   CSV is selected and links to the home page to select one or to the Convert page to create one.
   No table is shown.
4. **Given** a current CSV whose file has been deleted, **When** the developer opens the Table page,
   **Then** the page shows the recorded name, says the file is missing, and links to the home page.
   No table is shown.

---

### User Story 2 - Read every trackpoint in a table (Priority: P2)

Below the box, the developer reads every trackpoint of the activity as a table row, in the order
the trackpoints were recorded, with the ten columns the draft lists.

**Why this priority**: It is the page's main content, and it depends on Story 1's reading of the
current CSV.

**Independent Test**: Open the Table page for a small known CSV. The table has one row per data
row, in file order, and each cell shows the CSV's value for that column.

**Acceptance Scenarios**:

1. **Given** a current CSV with N data rows, **When** the page renders, **Then** the table has
   exactly N body rows, in the same order as the file.
2. **Given** the table, **When** the developer reads its header, **Then** the columns are, in this
   order: TrackpointNum, Time, ElapsedSeconds, ElapsedTime, HeartRateBpm, RunCadence,
   DistanceMiles, Latitude, Longitude, AltitudeFeet.
3. **Given** a data row, **When** it is shown, **Then** each cell shows that column's value exactly
   as written in the CSV, and a value that is empty in the CSV is an empty cell.
4. **Given** the full real marathon CSV (about 19,900 rows), **When** the developer opens the page,
   **Then** every row is shown and the page stays usable (SC-002).
5. **Given** a long table, **When** the developer scrolls down, **Then** the column headings stay
   visible, so each column can still be identified.

---

### User Story 3 - See heart-rate and cadence zones by color (Priority: P3)

The developer scans the table and sees, from the background color of the HeartRateBpm and
RunCadence cells, where the activity was easy or hard and where their cadence was low or high.

**Why this priority**: It adds insight on top of a table that already works without it.

**Independent Test**: Open the Table page for a fixture CSV whose rows hit every band edge of both
columns. Each HeartRateBpm and RunCadence cell has the band color in FR-014 and FR-015, and the
other columns have no band color.

**Acceptance Scenarios**:

1. **Given** HeartRateBpm values 89, 90, 119, 120, 139, 140, 159, 160, 179, and 180, **When** they
   are shown, **Then** their cells are light blue, light yellow, light yellow, light orange, light
   orange, dark orange, dark orange, light red, light red, and dark red.
2. **Given** RunCadence CSV values 9.5, 10, 74.5, 75, 89.5, and 90 (doubled: 19, 20, 149, 150,
   179, and 180 steps per minute), **When** they are shown, **Then** their cells are light blue,
   light yellow, light yellow, light green, light green, and darker green, and each cell shows the
   CSV value as written, not the doubled value (FR-015).
3. **Given** a typical running RunCadence value of 88 (176 steps per minute), **When** it is shown,
   **Then** the cell shows `88` on a light green background.
4. **Given** an empty HeartRateBpm or RunCadence value, **When** it is shown, **Then** the cell has
   no band color.
5. **Given** a cell with a dark band color (dark orange, dark red, darker green), **When** it is
   shown, **Then** its number is still easy to read.

---

### Edge Cases

- **No current CSV**: the page explains there is none and links to the home page and the Convert
  page. It does not fail, and it shows no table (Story 1, scenario 3).
- **Current CSV missing**: the page shows the recorded name, says the file is missing, and links to
  the home page (Story 1, scenario 4).
- **Header line only** (an activity with no trackpoints): the box shows the file name; the activity
  values cannot be read from a data row, so they show as dashes (FR-010, FR-011), and the page
  says the activity has no trackpoints instead of showing an empty table.
- **Not a TcxTool CSV**: the file's header line is not the exact 17-field header of the CSV contract
  (`specs/002-tcx-to-csv/contracts/csv-format.md`), or a data row does not have exactly 17 fields.
  The page shows the file name and a clear message that the file is not in the expected format. It
  shows no partial table and does not fail.
- **File unreadable**: the CSV exists but cannot be read (locked, permissions, removed while being
  read). The page shows a clear message and does not fail. The state file is not changed.
- **Non-numeric HeartRateBpm or RunCadence text**: the value is shown as written, and the cell has
  no band color.
- **Decimal values**: a HeartRateBpm value with a fractional part (for example `179.5`) falls in
  the band whose range contains it (FR-014). A RunCadence value is doubled first, so `89.5` is
  179 steps per minute, which is light green (FR-015).
- **Empty values elsewhere**: rows without a time, position, distance, or altitude (the real
  marathon CSV has 9) show empty cells for those columns; the row is still shown.
- **Very wide content**: the long Latitude, Longitude, and DistanceMiles values do not break the
  page layout on a normal desktop window; on a narrow window, the table scrolls sideways rather
  than overflowing the page.
- **Reading the page changes nothing**: opening the Table page never writes the CSV or the state
  file.

## Requirements *(mandatory)*

### Data source

- **FR-001**: The Table page MUST get the current CSV through the shared current-CSV state from
  feature 004 (its FR-009), and MUST NOT keep its own record of which CSV is current.
- **FR-002**: When there is no current CSV, the page MUST say so and link to the home page and to
  the Convert page, and MUST NOT show the header box's activity values or the table.
- **FR-003**: When the current CSV's file is missing, the page MUST show the recorded file name,
  say the file is missing, and link to the home page.
- **FR-004**: The page MUST read the current CSV as defined by the CSV contract of feature 002: a
  `|`-delimited file with the exact 17-field header line and one data row per trackpoint.
- **FR-005**: When the header line does not match the contract, or any data row does not have
  exactly 17 fields, the page MUST show the file name and a clear "not in the expected format"
  message, and MUST NOT show a partial table.
- **FR-006**: When the CSV cannot be read, the page MUST show a clear message instead of an error
  page. Error text MUST NOT include the file's contents (the CSVs hold real GPS positions).
- **FR-007**: Opening the Table page MUST NOT change the CSV file or the state file.

### Header Section

- **FR-008**: The page MUST show a box at the top, above the table, with the same card style as the
  boxes on the Home page.
- **FR-009**: The box MUST show, each with a label: the CSV file name, ActivityId, Date,
  DeviceName, DeviceId, and Sport. The file name comes from the current-CSV state; the other five
  values come from the first data row.
- **FR-010**: An empty value in the box MUST be shown as a dash, with its label still present.
- **FR-011**: When the CSV has a header line but no data rows, the box MUST show the file name, the
  five activity values as dashes, and the page MUST say the activity has no trackpoints instead of
  showing an empty table.

### Data Table Section

- **FR-012**: The page MUST show every data row of the CSV as one table row, in file order, with
  exactly these columns in this order: TrackpointNum, Time, ElapsedSeconds, ElapsedTime,
  HeartRateBpm, RunCadence, DistanceMiles, Latitude, Longitude, AltitudeFeet. The column headings
  MUST be these names.
- **FR-013**: Each cell MUST show the CSV value for its column exactly as written (no rounding or
  reformatting). An empty CSV value MUST be an empty cell.

### Color coding

- **FR-014**: A HeartRateBpm cell's background MUST be set by its numeric value, as follows. A
  value in a band is greater than or equal to the band's lower edge and less than its upper edge.

  | Band | Range | Color |
  |------|-------|-------|
  | 1 | below 90 | light blue |
  | 2 | 90 to below 120 | light yellow |
  | 3 | 120 to below 140 | light orange |
  | 4 | 140 to below 160 | dark orange |
  | 5 | 160 to below 180 | light red |
  | 6 | 180 or more | dark red |

- **FR-015**: A RunCadence cell's background MUST be set by its steps-per-minute value: the CSV
  value multiplied by 2, because the CSV's RunCadence is Garmin's per-foot value. The bands use the
  same edge rule as FR-014. The cell MUST still show the CSV value as written (FR-013), not the
  doubled value.

  | Band | Steps per minute (CSV value × 2) | CSV value | Color |
  |------|----------------------------------|-----------|-------|
  | 1 | below 20 | below 10 | light blue |
  | 2 | 20 to below 150 | 10 to below 75 | light yellow |
  | 3 | 150 to below 180 | 75 to below 90 | light green |
  | 4 | 180 or more | 90 or more | darker green |

- **FR-016**: A HeartRateBpm or RunCadence cell whose value is empty or not a number MUST have no
  band color. No other column has a band color.
- **FR-017**: Text in every colored cell MUST stay readable, including on the dark band colors
  (dark orange, dark red, darker green). The value in the cell, not the color alone, carries the
  information.

### Page behavior

- **FR-018**: The page MUST stay at `/table`, keep its navbar link, and keep "Table" as its heading
  and browser tab title (features 003 and 004).
- **FR-019**: The table's column headings MUST stay visible while the developer scrolls through the
  rows, and the table MUST scroll sideways on a window too narrow for it rather than overflow the
  page.
- **FR-020**: The page MUST stay static server-rendered unless the plan records a reason it needs
  interactivity (constitution Principle I).
- **FR-021**: The existing page contracts from features 001 to 004 MUST still hold, apart from the
  Table page content this feature replaces.
- **FR-022**: Tests MUST NOT read or write the developer's real `tcxtool/tcxtool-state.json` or
  `tcxtool/data/csv/` files. They use small purpose-built CSV fixtures.

### Key Entities

- **Current CSV**: the file named by the application state (feature 004), with a status of none,
  present, or missing. The Table page's only data source.
- **Activity summary**: the values shown in the header box: the CSV file name, plus ActivityId,
  Date, DeviceName, DeviceId, and Sport from the first data row. Every row of one CSV holds the same
  activity values, because a CSV holds one activity.
- **Trackpoint row**: one CSV data row, shown as one table row with the ten displayed columns. The
  CSV's other fields (DistanceMeters, AltitudeMeters, and the five activity fields) are not shown in
  the table.
- **Band**: a value range and its background color, for HeartRateBpm (six bands, in beats per
  minute) and RunCadence (four bands, in steps per minute: the CSV value doubled).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For a fixture CSV, 100% of its data rows appear in the table, in file order, and
  every displayed cell equals the CSV's text for that column.
- **SC-002**: With the real marathon CSV (about 19,900 rows), the Table page is fully shown in under
  5 seconds on the developer's machine.
- **SC-003**: For a fixture that hits every band edge of both columns, 100% of HeartRateBpm and
  RunCadence cells have the color their band defines, and cells with empty or non-numeric values
  have none.
- **SC-004**: In 100% of the edge cases listed (no current CSV, missing file, header only, wrong
  format, unreadable file), the page renders without an error page and tells the developer what to
  do next.
- **SC-005**: The developer can identify the activity being shown (file name, activity id, date,
  device, and sport) within 5 seconds of opening the page, without scrolling.
- **SC-006**: All four constitution Quality Gates pass, including the Docker Compose gate.

## Assumptions

- **One user, local machine**: as in feature 004, the app has one local user. No sign-in or
  per-user state is involved.
- **One activity per CSV**: the converter writes one CSV per activity, so every row has the same
  ActivityId, Date, DeviceName, DeviceId, and Sport. The header box uses the first data row.
- **All rows, one page**: the draft says each data row is shown, so the table shows every row with
  no paging or filtering. Sorting, searching, paging, and downloading are out of scope.
- **Values as written**: cells show the CSV text unchanged, including long decimals such as
  `0.0009444842003490327`. Formatting or rounding values is out of scope.
- **RunCadence units**: Garmin's TCX RunCadence counts one foot, so it is about half of steps per
  minute. In the real marathon CSV, running values are mostly 80 to 99 and walking values 50 to 69.
  Doubling them (Clarifications) puts the draft's 150 and 180 thresholds where running cadence
  actually falls.
- **The RunCadence gap at 20**: the draft gives "less than 20" and "between 21 and 149", which
  leaves 20 uncovered. The spec puts 20 steps per minute (a CSV value of 10) in the light yellow
  band. The draft's "between A and B" is
  read as A up to, but not including, the next band's lower edge, so decimal values have exactly
  one band.
- **Colors**: the draft names the colors (light blue, light yellow, and so on). The exact shades
  are left to the plan, within the app's existing Bootstrap look, provided adjacent bands are easy
  to tell apart and text stays readable (FR-017).
- **No legend**: the draft does not ask for a color legend, so none is added.
- **File size**: CSVs come from the converter, whose upload limit bounds their size. The real
  marathon CSV (about 4 MB, about 19,900 rows) is the size the page must handle well.
- **Existing navigation**: the Table page and its navbar link already exist (feature 003). This
  feature replaces only the page's content.
- **Dependencies**: the CSV contract from feature 002 and the current-CSV state from feature 004.
