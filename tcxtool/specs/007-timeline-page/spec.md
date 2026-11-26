# Feature Specification: Timeline Page

**Feature Branch**: `007-timeline-page` (feature id only; no git branch is created by `/speckit-specify`)

**Created**: 2026-11-25

**Status**: Draft

**Input**: User description, from the draft file `docs/spec-007-draft.md` (passed to
`/speckit-specify` as `@../docs/spec-007-draft.md`). In summary:

- **States CSV**:
  - Add three columns: StateTrackpointCount, ActivityDistanceMiles, and ActivityElapsedSeconds.
  - Rename DistanceMiles to StateDistanceMiles.
  - Put the 16 fields in a new order.
- **Timeline page**: build it out with three sections:
  - a header box
  - a horizontal Timeline Bar of the activity's StateSets, colored by state, with a popup of a
    segment's values
  - three charts (Run Cadence, Heart Rate, Altitude), exactly as wide as the bar and aligned with it

"StateSet" is defined in `specs/006-activity-states/spec.md`: a run of consecutive trackpoints with
the same State.

## Clarifications

### Session 2026-11-25

- Q: What sets a segment's width and the charts' horizontal axis? → A: Time. Widths are
  proportional to StateElapsedSeconds, and the charts share that elapsed-time axis (FR-013). Stops
  show at their real length.
- Q: Which colors for the states? → A: Bootstrap 5.3 colors: stopped gray, walking amber, running
  green, unknown light gray (FR-014).

## User Scenarios & Testing *(mandatory)*

The user is the developer, running the app locally and analyzing one of their own Garmin
activities. They have already converted a TCX file, so a current CSV and its States CSV exist.

### User Story 1 - The States CSV carries activity totals and set sizes (Priority: P1)

The developer converts a TCX file. Each States CSV row now says how many trackpoints its StateSet
holds, and where the activity stood (miles and seconds) when the set ended. The fields are in an
order that reads from the activity down to the set's averages.

**Why this priority**: The Timeline page is drawn from these values. On its own, the file also lets
the developer see the running totals without going back to the activity CSV.

**Independent Test**: Convert a fixture TCX file. Every States CSV row has 16 fields in the new
order. The new values match a hand calculation from the fixture's activity CSV.

**Acceptance Scenarios**:

1. **Given** a StateSet of 38 trackpoints whose last row has ElapsedSeconds `37` and DistanceMiles
   `0.0712`, **When** the TCX file is converted, **Then** that set's row has StateTrackpointCount
   `38`, ActivityElapsedSeconds `37`, and ActivityDistanceMiles `0.0712`.
2. **Given** any conversion, **When** the States CSV is opened, **Then** its header is exactly the
   16 field names in the order of FR-002, and the column feature 006 called `DistanceMiles` is now
   `StateDistanceMiles`, with the same values.
3. **Given** an activity, **When** the StateTrackpointCount values of its States CSV are added up,
   **Then** the total equals the activity CSV's row count.

---

### User Story 2 - See the whole activity as one bar of states (Priority: P2)

The developer opens the Timeline page. A header box names the current CSV and the activity. Below
it, one horizontal bar spans the page and represents the activity's elapsed time. It is split into
adjacent segments, one per StateSet, in order, each as wide as the set's share of the time and
colored by its state. Hovering over a segment shows a popup with every value of that
StateSet.

**Why this priority**: It shows at a glance where the developer ran, walked, and stopped, which is
the main reason for the page.

**Independent Test**: Convert a fixture with a known sequence of StateSets, then open the Timeline
page. The header shows the expected values, the bar has one segment per StateSet in order with the
expected colors and relative widths, and hovering over a segment shows its values.

**Acceptance Scenarios**:

1. **Given** a current CSV with a States CSV, **When** the developer opens the Timeline page,
   **Then** a header box shows the CSV file name, ActivityId, Date, ActivityDistanceMiles, and
   ActivityElapsedTime, styled like the boxes on the Home page.
2. **Given** a States CSV with 541 StateSets, **When** the page is shown, **Then** the bar has 541
   adjacent segments in file order, with no gaps or overlaps, and together they fill the bar's
   full width.
3. **Given** a segment, **When** the developer hovers over it, **Then** a popup shows all 16 values
   of that StateSet, labeled with their field names.
4. **Given** an open popup, **When** the developer clicks anywhere outside it, **Then** it closes.
5. **Given** an open popup, **When** the developer hovers over a different segment, **Then** the
   popup shows that segment's values instead.

---

### User Story 3 - Compare cadence, heart rate, and altitude across the activity (Priority: P3)

Below the bar, three charts show the AverageRunCadence, AverageHeartRateBpm, and
AverageAltitudeMeters of each StateSet, labeled "Run Cadence", "Heart Rate", and "Altitude". They
are exactly as wide as the bar, and each StateSet's point sits directly below its segment. So the
developer can look straight down from a segment and read its values.

**Why this priority**: It adds the "why" to the bar, for example a heart-rate rise during a run,
but the bar is useful without it.

**Independent Test**: Open the Timeline page for a fixture. The three charts and the bar have the
same pixel width and left edge. Each chart has one point per StateSet that has a value, directly
below the middle of its segment, joined in order by thin gray lines.

**Acceptance Scenarios**:

1. **Given** the Timeline page, **When** it is shown, **Then** the bar and the three charts have
   exactly the same width in pixels and the same left edge, at any browser window width.
2. **Given** a StateSet, **When** its points are drawn, **Then** each point is horizontally at the
   middle of that StateSet's segment in the bar.
3. **Given** a chart, **When** it is drawn, **Then** its vertical axis runs from that chart's own
   minimum to its own maximum value, so its points use the chart's full height.
4. **Given** the points of a chart, **When** they are drawn, **Then** thin gray lines join each
   point to the next one in StateSet order.

---

### Edge Cases

- **No current CSV, no States CSV, or a States CSV that is missing, unreadable, empty, or in the
  wrong format**: the page shows the same kind of notice as the States page, never an error page.
- **A States CSV written by feature 006** (13 fields) is the wrong format, with a link to convert
  the TCX file again. It is not read or upgraded in place, as feature 006 did for old activity CSVs.
- **One StateSet**: the bar is one segment across the full width, and each chart has one point and
  no line.
- **Empty values**:
  - A StateSet with no value for a chart's field has no point in that chart. The line joins the
    points on either side of it.
  - A chart with no values at all shows its label and a short "no data" note instead of an empty
    chart.
- **All values in a chart are equal**: the chart still draws its points, at mid-height, with a
  small range around the value.
- **Very short StateSets**: a segment narrower than one pixel is still in the bar's order, and its
  point is still drawn.
  - Hovering within 4 px of a narrow segment shows its popup. A segment under 4 px wide is picked by
    the nearest center, so a zero-width segment can be reached too.
  - Where several segments share one pixel, the nearest center wins. All of them are still listed
    on the States page.
- **The `unknown` state** has its own neutral color in the bar.
- **A StateSet with StateElapsedSeconds `0`** (for example, a one-trackpoint set) has zero width. It
  is still in the bar's order, and its chart points are drawn at its position.
- **The browser window is resized**: the bar and charts stay the same width as each other, and
  aligned.
- **Script turned off in the browser**: the header box still shows. The bar and charts don't
  appear, and a short note says they need script.

## Requirements *(mandatory)*

### States CSV

- **FR-001**: Each States CSV row MUST have three new fields:
  - **StateTrackpointCount**: the number of activity CSV rows in the StateSet. It is never empty.
  - **ActivityDistanceMiles**: the DistanceMiles of the StateSet's last row that has one, as
    written. It is empty when no row in the set has one.
  - **ActivityElapsedSeconds**: the ElapsedSeconds of the StateSet's last row that has one, as
    written. It is empty when no row in the set has one.
- **FR-002**: The States CSV MUST have exactly these 16 fields, in this order:
  ActivityId, ActivityDistanceMiles, ActivityElapsedTime, ActivityElapsedSeconds, State,
  StateEndDate, StateEndTrackpointNum, StateTrackpointCount, StateElapsedSeconds,
  StateDistanceMiles, AverageHeartRateBpm, AverageRunCadence, AverageLatitude, AverageLongitude,
  AverageAltitudeMeters, AverageAltitudeFeet.
- **FR-003**: `StateDistanceMiles` is feature 006's `DistanceMiles` under a new name. Its values and
  every other carried-over field's rule MUST stay as feature 006 defined them.
- **FR-004**: Everything else about the States CSV MUST stay as feature 006 defined it: the file
  name, encoding, delimiter, one row per StateSet, both files written or neither, the same bytes
  when converted twice, and never being listed on the home page.
- **FR-005**: The States page MUST show the 16 fields in the FR-002 order, and keep its heart-rate
  and cadence band colors on the two average columns.
- **FR-006**: A States CSV in feature 006's 13-field format MUST be treated as the wrong format, by
  both the States page and the Timeline page, with a link to convert the TCX file again.

### Timeline page: data and notices

- **FR-007**: The Timeline page MUST take its data from the current CSV's States CSV only. It never
  reads the activity CSV.
- **FR-008**: When there is no current CSV, no States CSV, or the States CSV is missing,
  unreadable, in the wrong format, or has no StateSets, the page MUST show a notice with the same
  wording and links as the States page, and no bar or charts. It MUST never show an error page.
- **FR-009**: Serving the Timeline page MUST NOT change any file.

### Timeline page: header

- **FR-010**: The header box MUST show:
  - the current CSV's file name
  - the ActivityId of the first StateSet
  - the Date: the `yyyy-MM-dd` at the start of the CSV file name, or empty when the name does not
    start with one
  - the ActivityDistanceMiles and ActivityElapsedTime of the last StateSet that has each value
- **FR-011**: The header box MUST look like the cards on the Home page.

### Timeline page: bar

- **FR-012**: The bar MUST be one horizontal strip with one segment per StateSet, in file order,
  adjacent with no gaps or overlaps, together filling the bar's full width.
- **FR-013**: Each segment's width MUST be proportional to its StateElapsedSeconds, so the bar is
  the activity's elapsed time. The charts' horizontal axis MUST be the same elapsed-time scale.
  A StateSet whose StateElapsedSeconds is empty, not a number, negative, or infinite counts as 0
  seconds, and has zero width. When every StateSet counts as 0 seconds, as in a one-trackpoint
  activity, all segments get equal widths, so the bar is still filled.
- **FR-014**: Each segment MUST be colored by its State, with Bootstrap 5.3 colors: `stopped` gray
  (`#6c757d`), `walking` amber (`#ffc107`), `running` green (`#198754`), and `unknown` light gray
  (`#dee2e6`). These are the app's state colors, defined in one place.
- **FR-015**: Hovering over a segment MUST show a popup with all 16 values of its StateSet, each
  labeled with its field name, exactly as written in the file.
- **FR-016**: The popup MUST stay open until the developer clicks outside it, or hovers over another
  segment, which replaces its content.

### Timeline page: charts

- **FR-017**: Below the bar there MUST be three charts, in this order:
  - "Run Cadence", of AverageRunCadence
  - "Heart Rate", of AverageHeartRateBpm
  - "Altitude", of AverageAltitudeMeters
- **FR-018**: The bar and the three charts MUST have exactly the same width in pixels and the same
  left edge, at any browser window width, including after a resize.
- **FR-019**: Each StateSet with a value MUST have one point in the chart, at the horizontal middle
  of its segment in the bar.
- **FR-020**: Each chart's vertical axis MUST run from its own minimum to its own maximum value,
  with labeled ticks. When all its values are equal, it MUST use a small range around the value.
- **FR-021**: Thin gray lines MUST join each chart's points in StateSet order. A StateSet with no
  value is skipped, so the line joins its neighbours.

### Unchanged behavior

- **FR-022**: The activity CSV, the state file, and the navbar MUST be unchanged. The Convert,
  Table, and About pages MUST keep their content and behavior. The only change they get is one
  shared script reference that every page loads, which defines the timeline element and does
  nothing on pages without one. The Timeline link already exists.
- **FR-023**: The page MUST work with no network access, as the rest of the app does.
- **FR-024**: Tests MUST NOT read or write the developer's real `tcxtool-state.json` or `data/csv/`
  files, and MUST NOT use real GPS positions.

### Key Entities

- **States CSV row (changed)**: one StateSet, with 16 fields: where the activity stood when the set
  ended (ActivityId, ActivityDistanceMiles, ActivityElapsedTime, ActivityElapsedSeconds), the set
  itself (State, StateEndDate, StateEndTrackpointNum, StateTrackpointCount, StateElapsedSeconds,
  StateDistanceMiles), and its averages.
- **Timeline segment**: one StateSet drawn as part of the bar, with a width, a color, and a popup.
- **Chart point**: one StateSet's average for one chart, placed under its segment.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For the sample marathon, the States CSV's StateTrackpointCount values add up to the
  activity CSV's 19,908 rows, and the last row's ActivityDistanceMiles and ActivityElapsedSeconds
  equal the activity CSV's last DistanceMiles and ElapsedSeconds.
- **SC-002**: For the sample marathon (541 StateSets), the Timeline page is fully shown, bar and
  charts included, in under 3 seconds of opening it.
- **SC-003**: At three window widths (narrow, medium, and wide), the bar and the three charts are
  the same width to the pixel, and every chart point is within 1 pixel of the middle of its
  segment.
- **SC-004**: A popup appears within half a second of hovering over a segment, and shows all 16
  values exactly as they are in the file.
- **SC-005**: Every notice case gives a page with a notice, never an error page.
- **SC-006**: All four quality gates pass, including the Docker Compose gate.

## Assumptions

- The charting library is D3.js, as the constitution names. It is served from the app's own files,
  as Bootstrap is, so the page needs no network.
- The bar, charts, and popup run in the browser. The page itself stays server-rendered; only the
  drawing needs script.
- The Home page cards are the Bootstrap cards on `Home.razor`.
- The Timeline link is already in the navbar, from feature 003. The placeholder page is replaced.
- Old States CSVs are handled as feature 006 handled old activity CSVs: wrong format, convert again.
- The draft's "the DistanceMiles value of the last row in the StateSet" means the last row that has
  a value, the rule feature 006 uses for ActivityElapsedTime. The same goes for ElapsedSeconds.
- The draft's "click outside the popup" is the only way to close it, apart from hovering another
  segment. Moving the pointer off a segment leaves the popup open, so its values can be read and
  selected.
- A chart point at the middle of its segment is what "perfectly vertically aligned" means. Points
  are not stretched across the segment.
- Charts show AverageAltitudeMeters only, not AverageAltitudeFeet, as the draft says.
- The page is for one local user on a desktop browser. Touch input is not a goal.
