# Feature Specification: Activity States

**Feature Branch**: `006-activity-states` (feature id only; no git branch is created by `/speckit-specify`)

**Created**: 2026-11-25

**Status**: Draft

**Input**: User description, from the draft file `docs/spec-006-draft.md` (passed to
`/speckit-specify` as `@../docs/spec-006-draft.md`). The draft asks for three things:

- **Enhanced current CSV file**: double the RunCadence value, and add a `State` column
  (`stopped`, `walking`, `running`, default `unknown`) and a `StateSeconds` column (seconds since
  the state last changed), placed after RunCadence.
- **New States CSV file**: written from the enhanced CSV, named like it but ending `.states.csv`,
  and recorded in the state file under `statesCsvFilename`. It has one row per run of rows in the
  same state (a "StateSet"), with the columns ActivityId, StateEndDate, StateEndTrackpointNum,
  StateElapsedSeconds, ActivityElapsedTime, AverageLatitude, AverageLongitude,
  AverageHeartRateBpm, AverageRunCadence, DistanceMiles, AverageAltitudeMeters, and
  AverageAltitudeFeet.
- **New States page**: after "Table" in the navigation menu, showing every row and column of the
  States CSV, with cell colors that follow the Table page's logic.

## Clarifications

### Session 2026-11-25

- Q: What should the app do with an activity CSV written before this feature (17 fields, RunCadence
  not doubled)? → A: Treat it as not in the format this app writes. The Table page shows its
  wrong-format notice and says to convert the TCX file again. No old-format reading and no upgrade
  in place.
- Q: Should StateEndDate hold the `Date` column (the same day on every row) or the moment the
  StateSet ended? → A: The `Time` value of the set's last row that has one. The column keeps the
  name StateEndDate.
- Q: Where does a StateSet's time and distance start? → A: Where the previous StateSet ended: at
  its last row that has a value. The first StateSet starts at the activity's start (0 seconds,
  0 miles). The StateSets then add up exactly to the activity's total time and distance.

## User Scenarios & Testing *(mandatory)*

The user is the developer running the app on their own machine, as in features 002 to 005.

### User Story 1 - The activity CSV records the runner's state on every trackpoint (Priority: P1)

The developer converts a TCX file. Each row of the new CSV shows RunCadence in steps per minute,
whether the runner was stopped, walking, or running at that moment, and how long they had been in
that state.

**Why this priority**: The States CSV and the States page are both built from these columns.
Doubling RunCadence also gives the Table page a value it can color without converting it.

**Independent Test**: Convert a fixture TCX file whose cadence values cross both state edges. Every
row's RunCadence is double the TCX value, its State follows the thresholds, and its StateSeconds
counts up from the last change. The Table page colors the cadence cells by the new values.

**Acceptance Scenarios**:

1. **Given** a TCX trackpoint with RunCadence 60, **When** it is converted, **Then** its CSV row
   has RunCadence `120` and State `walking`.
2. **Given** TCX RunCadence values of 0, 1, 74, 75, and 90, **When** they are converted, **Then**
   their CSV RunCadence values are `0`, `2`, `148`, `150`, and `180`, and their States are
   `stopped`, `walking`, `walking`, `running`, and `running`.
3. **Given** a trackpoint with no RunCadence, **When** it is converted, **Then** its RunCadence is
   empty and its State is `unknown`.
4. **Given** rows one second apart, **When** they are converted, **Then** their StateSeconds
   values count up by one, and the first row of a new State shows `1`: the second since the last
   row of the previous State. The first row of the activity shows `0`.
5. **Given** a converted CSV, **When** the developer reads its header, **Then** `State` and
   `StateSeconds` come straight after `RunCadence`, and every other column keeps its name and order.
6. **Given** the new CSV is the current CSV, **When** the developer opens the Table page, **Then** a
   RunCadence cell showing `180` gets the darkest cadence color, and one showing `148` gets the
   light yellow color, with no further doubling.

---

### User Story 2 - Each state change is summarized in a States CSV (Priority: P2)

The developer converts a TCX file. Next to its CSV, the app writes a States CSV with one row for
each StateSet: how long it lasted, where it ended, and the average heart rate, cadence, position,
and altitude while in it. The state file records the States CSV next to the current CSV.

**Why this priority**: It turns about 20,000 trackpoints into a few hundred rows that show the
shape of the activity. It depends on Story 1's State column.

**Independent Test**: Convert a fixture with 38 walking rows followed by 113 running rows. The
States CSV has two data rows: walking ending at trackpoint 38, and running ending at trackpoint 151,
with values that match a calculation made independently of the app.

**Acceptance Scenarios**:

1. **Given** a TCX file `24932322230.tcx`, **When** it is converted, **Then** the CSV folder holds
   `2026-11-01_24932322230.csv` and `2026-11-01_24932322230.states.csv`.
2. **Given** 38 walking rows then 113 running rows, **When** the States CSV is written, **Then** it
   has a header and two data rows, in that order, with StateEndTrackpointNum `38` and `151`. With
   one trackpoint per second from 0, their StateElapsedSeconds are `37` and `113`, which add up to
   the activity's 150 seconds.
3. **Given** a StateSet, **When** its row is written, **Then** each Average column is the mean of
   that column's values in the set's rows, and AverageRunCadence uses the doubled values.
4. **Given** a successful conversion, **When** the developer opens the state file, **Then** it names
   the current CSV under `csvFilename` and its States CSV under `statesCsvFilename`.
5. **Given** a States CSV in the CSV folder, **When** the developer opens the home page's Select CSV
   File list, **Then** the States CSV is not listed and cannot be chosen as the current CSV.
6. **Given** the developer selects another CSV on the home page, **When** the state file is written,
   **Then** `statesCsvFilename` names that CSV's States CSV, or is absent when it has none.
7. **Given** the same TCX file converted twice, **When** the two States CSVs are compared, **Then**
   they are byte-identical.

---

### User Story 3 - Read the state changes on a States page (Priority: P3)

The developer clicks "States" in the navbar, just after "Table". The page shows the current
activity's States CSV as a table, with every row and every column, and colors the average heart
rate and average cadence cells like the Table page does.

**Why this priority**: It is the visible result of Stories 1 and 2, which are useful on their own
as files.

**Independent Test**: Convert a fixture, then open the States page. The table has one row per
States CSV data row and the 13 column headings in file order, and the two average cells of each
row carry the expected band colors.

**Acceptance Scenarios**:

1. **Given** the current CSV has a States CSV, **When** the developer opens the States page,
   **Then** the page names the States CSV and shows a table with one row per data row, in file
   order, with every value exactly as written.
2. **Given** a row with AverageHeartRateBpm `165.4` and AverageRunCadence `172`, **When** the page
   renders, **Then** those cells get the same colors the Table page gives a heart rate of 165.4 and
   a cadence of 172.
3. **Given** the navbar, **When** any page renders, **Then** "States" appears straight after
   "Table", and on the States page it is marked as the current page.
4. **Given** no current CSV, **When** the developer opens the States page, **Then** the page says so
   and links to the home page and the Convert page, with no table and no error page.
5. **Given** a current CSV with no States CSV, **When** the developer opens the States page,
   **Then** the page names the current CSV and says to convert its TCX file again.
6. **Given** a States CSV that cannot be read or is not in the expected format, **When** the
   developer opens the States page, **Then** the page shows a notice naming the file instead of an
   error page.

---

### Edge Cases

- **No trackpoints**: the activity CSV and the States CSV each hold only their header line. The
  States page shows the file name and says the activity has no state changes.
- **One state throughout**: the States CSV has one data row covering every trackpoint.
- **A StateSet of one row**: its averages are that row's values. Its StateElapsedSeconds is the
  time since the previous StateSet's last row, 1 second when the watch records once a second.
- **No RunCadence in the whole activity** (no cadence sensor): every row is `unknown`, so the States
  CSV has one `unknown` row.
- **`unknown` between known states**: a run of `unknown` rows is its own StateSet with its own
  States CSV row, so a sensor drop-out shows up instead of being hidden inside a neighbor.
- **Rows with no time**: their StateSeconds is empty. They still belong to their StateSet. A
  StateSet's time is measured from the last timed row before it, or from 0 when there is none. A
  set with no timed rows has an empty StateEndDate, StateElapsedSeconds, and ActivityElapsedTime.
- **A CSV written before this feature**: the Table page says it is not in the format this app
  writes and to convert its TCX file again. It has no States CSV, so the States page says the same.
- **Rows with no position, heart rate, altitude, or distance**: they are left out of that column's
  average or distance. When no row in the set has a value, the field is empty.
- **Several activities in one TCX file**: each activity CSV gets its own States CSV. As in feature
  004, the first CSV becomes current, and `statesCsvFilename` names its States CSV.
- **A state file naming a States CSV as `csvFilename`**: the app treats it as "no current CSV", like
  any other name it does not accept (feature 004 FR-008).
- **A `statesCsvFilename` that does not belong to the current CSV** (for example, after a hand
  edit): the States page treats it as "no States CSV" and says to convert the TCX file again.
- **The States CSV was deleted after the conversion**: the States page names the missing file.
- **A States CSV whose header or field count is wrong**: the States page says the file is not in the
  format this app writes and names it.

## Requirements *(mandatory)*

### Enhanced activity CSV

- **FR-001**: Every activity CSV MUST have 19 fields on every line, with this exact header:
  `ActivityId|Date|DeviceName|DeviceId|Sport|TrackpointNum|Time|ElapsedSeconds|ElapsedTime|Latitude|Longitude|HeartRateBpm|RunCadence|State|StateSeconds|DistanceMeters|DistanceMiles|AltitudeMeters|AltitudeFeet`.
  Every field other than RunCadence, State, and StateSeconds MUST keep its feature 002 value. The
  file rules of the feature 002 CSV contract (name, encoding, line endings, delimiter, text
  cleaning, replacing an existing file) MUST still apply.
- **FR-002**: RunCadence MUST be twice the TCX RunCadence value, written as a shortest decimal (the
  feature 002 rule for derived numbers). A TCX value of 60 MUST be written as `120`. A missing value
  MUST be written as an empty field. A TCX RunCadence that is not a finite number MUST still reject
  the file before anything is written, as feature 002 does for every number (`InvalidValue`).
- **FR-003**: State MUST be set from the row's CSV RunCadence value, which is already doubled:
  `stopped` when it is 0, `walking` when it is greater than 0 and less than 150, `running` when it
  is 150 or more, and `unknown` when it is empty or negative. The parser accepts any finite number,
  so a negative value can reach the converter.
- **FR-004**: A StateSet MUST be a longest run of consecutive rows, in file order, that share one
  State. A new StateSet MUST start at the first row and at every row whose State differs from the
  row before it.
- **FR-005**: StateSeconds MUST be the row's ElapsedSeconds minus the StateSet's start time. The
  start time MUST be the ElapsedSeconds of the last row before the StateSet that has one, or 0 when
  no earlier row has one. StateSeconds MUST be written as a shortest decimal, and MUST be empty when
  the row has no ElapsedSeconds.
- **FR-006**: Converting the same TCX file twice MUST give byte-identical activity CSVs, as in
  feature 002.

### States CSV

- **FR-007**: Each conversion MUST write a States CSV for every activity CSV it writes, in the same
  folder. Its name MUST be the activity CSV's name with the final `.csv` replaced by `.states.csv`,
  for example `2026-11-01_24932322230.states.csv`. It MUST follow the same encoding, line-ending,
  delimiter, text-cleaning, and replacement rules as the activity CSV.
- **FR-008**: The States CSV MUST have this exact header, followed by one row per StateSet in file
  order:
  `ActivityId|State|StateEndDate|StateEndTrackpointNum|StateElapsedSeconds|ActivityElapsedTime|AverageLatitude|AverageLongitude|AverageHeartRateBpm|AverageRunCadence|DistanceMiles|AverageAltitudeMeters|AverageAltitudeFeet`.
  An activity with no trackpoints MUST give a States CSV with only the header line.
- **FR-009**: Each States CSV row MUST hold these values for its StateSet:

  | Field | Value |
  |-------|-------|
  | ActivityId | The set's ActivityId |
  | State | The set's State |
  | StateEndDate | Time of the set's last row that has one, as written; empty when none does |
  | StateEndTrackpointNum | TrackpointNum of the set's last row |
  | StateElapsedSeconds | StateSeconds of the set's last row that has one; empty when none does |
  | ActivityElapsedTime | ElapsedTime of the set's last row that has one; empty when none does |
  | AverageLatitude, AverageLongitude, AverageHeartRateBpm, AverageRunCadence, AverageAltitudeMeters, AverageAltitudeFeet | The mean of the matching activity CSV column over the set's rows that have a value; empty when none does |
  | DistanceMiles | DistanceMiles of the set's last row that has one, minus the DistanceMiles of the last row before the set that has one (0 when none does); empty when no row in the set has one |

  Calculated numbers MUST be written as shortest decimals. AverageRunCadence MUST use the doubled
  RunCadence values.
- **FR-010**: The States CSV MUST be calculated from the values written to the activity CSV, so the
  two files always agree.
- **FR-011**: The Convert page's result MUST name the States CSV next to each activity CSV it lists.
- **FR-012**: Converting the same TCX file twice MUST give byte-identical States CSVs.

### State file and CSV selection

- **FR-013**: When a conversion records its current CSV (feature 004), the state file MUST also hold
  `statesCsvFilename`, naming that CSV's States CSV.
- **FR-014**: When the developer selects a CSV on the home page, the state file MUST set
  `statesCsvFilename` to that CSV's States CSV when the file exists in the CSV folder, and MUST
  leave it out otherwise. The app MUST never write a `statesCsvFilename` that is not the States CSV
  of the `csvFilename` written with it.
- **FR-015**: A States CSV MUST NOT appear in the home page's Select CSV File list, and MUST NOT be
  accepted as the current CSV from the form or from the state file's `csvFilename`.
- **FR-016**: A missing or malformed `statesCsvFilename`, or one that is not the current CSV's
  States CSV, MUST be treated as "no States CSV". It MUST NOT change how `csvFilename` is read, and
  MUST NOT make any page fail.

### Existing CSVs and the Table page

- **FR-017**: An activity CSV written before this feature (17 fields, RunCadence not doubled) MUST
  be treated as not in the format this app writes. The Table page MUST show its wrong-format notice
  naming the file, and the notice MUST say to convert the TCX file again. The app MUST NOT read the
  old format or rewrite an old file.
- **FR-018**: The Table page MUST color a RunCadence cell by the value as written, since it is now
  in steps per minute: below 20, from 20 to below 150, from 150 to below 180, and 180 or more, with
  the same colors as feature 005. The Table page's other behavior, including its 10 columns, MUST
  stay as feature 005 defines it.

### States page

- **FR-019**: The app MUST have a States page at `/states`, with a navbar link "States" placed
  straight after "Table". Its heading and browser tab title MUST be "States". It MUST be static
  server-rendered, like the Table page.
- **FR-020**: When the current CSV has a States CSV, the page MUST show the States CSV's file name
  above a table. The table MUST have the 13 States CSV columns as headings, in file order, and one
  row per data row, in file order. Each cell MUST show its value exactly as written.
- **FR-021**: AverageHeartRateBpm cells MUST use the Table page's heart-rate bands, and
  AverageRunCadence cells its cadence bands (FR-018), with the same thresholds and colors. A cell
  that is empty or not a number MUST have no color. No other cell is colored.
- **FR-022**: Instead of a table or an error page, the States page MUST show a notice when there is
  no current CSV (with links to the home page and the Convert page), when the current CSV has no
  States CSV (naming the current CSV and saying to convert its TCX file again), when the States CSV
  is missing, cannot be read, or is not in the States CSV format (naming the file), and when it has
  no data rows.
- **FR-023**: The table's column headings MUST stay visible while the developer scrolls through the
  rows, as on the Table page.
- **FR-024**: Opening the States page MUST NOT change any file.

### Unchanged behavior

- **FR-025**: The page contracts of features 001 to 005 MUST still hold, apart from the changes this
  spec names: the CSV format, the Table page's cadence bands and wrong-format notice, the state
  file's new property, the hidden States CSVs, and the navbar's new link.
- **FR-026**: Tests MUST NOT read or write the developer's real `tcxtool/tcxtool-state.json` or
  `data/csv/` files.

### Key Entities

- **Activity CSV row**: one trackpoint, now with 19 fields. RunCadence is in steps per minute, and
  State and StateSeconds describe the runner's state at that trackpoint.
- **State**: `stopped`, `walking`, `running`, or `unknown`, set by the doubled RunCadence.
- **StateSet**: a longest run of consecutive rows with the same State. It has a first row and a last
  row, and the rows in between.
- **States CSV**: one file per activity CSV, with one row per StateSet that summarizes its time,
  end point, distance, and averages.
- **State file**: the app's `tcxtool-state.json`. It names the current CSV (`csvFilename`, feature
  004) and now that CSV's States CSV (`statesCsvFilename`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For every fixture, 100% of the activity CSV's fields match the expected values
  exactly, including RunCadence, State, and StateSeconds. The expected values are calculated
  independently of the app.
- **SC-002**: For every fixture, the States CSV has exactly one data row per StateSet, and 100% of
  its fields match expected values calculated independently of the app.
- **SC-003**: Each sample file in `data/tcx/` still converts, now to both files, in under 10
  seconds, as feature 002 SC-001 requires.
- **SC-004**: With the real marathon file (about 19,900 trackpoints, which gives about 540
  StateSets), the States page is fully shown in under 5 seconds.
- **SC-005**: In 100% of the States page's notice cases (FR-022), the page shows its notice, never
  an error page, and no file changes.
- **SC-006**: All four constitution Quality Gates pass, including the Docker Compose gate.

## Assumptions

- **One user, local machine**: as in features 004 and 005.
- **Thresholds apply to the doubled value**: the draft lists the doubling first and gives 150 as
  the running edge, which matches steps per minute. In the real marathon CSV, running cadence is
  mostly 160 to 198 steps per minute once doubled.
- **No gaps between states**: the draft gives "between 1 and 149" for walking. Doubled TCX values
  are whole even numbers, so the spec reads it as "greater than 0 and less than 150". Every
  number then has exactly one state.
- **A `State` column in the States CSV**: the draft's column list has no field naming the state,
  so a row could not say whether it summarizes walking or running. The spec adds `State` as the
  second column.
- **`unknown` StateSets are summarized**: the draft names stopped, walking, and running, and says
  the default is `unknown`. The spec writes a row for `unknown` sets too, so the States CSV accounts
  for every trackpoint. The real marathon CSV has no `unknown` rows.
- **Every change counts**: the draft says one row per state change, so no smoothing is applied.
  The real marathon CSV gives about 540 StateSets, and about 150 of them are 5 rows or fewer.
  Merging short StateSets is out of scope.
- **Distance from the cumulative column**: DistanceMiles in the activity CSV is cumulative, so a
  StateSet's distance is the difference between two of its values, not a sum.
- **No name clash**: ActivityIds contain only letters, digits, `-`, and `_` (feature 002), so an
  activity CSV's name can never end in `.states.csv`.
- **The Table page keeps its 10 columns**: the draft does not ask for State or StateSeconds there,
  so they are not added.
- **The States page has no Activity card**: the draft asks for the table only. The page names the
  States CSV file above it.
- **Old CSVs need one reconversion**: the CSVs already in `data/csv/`, including the tracked
  `2026-11-01_24932322230.csv`, are in the old format. They still appear in the home page's list
  (they are valid CSV names), and the pages say to convert them again (Clarifications).
- **Dependencies**: the CSV contract from feature 002, the current-CSV state and home page
  selection from feature 004, and the Table page's bands and colors from feature 005.
