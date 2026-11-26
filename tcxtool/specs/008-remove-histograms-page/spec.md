# Feature Specification: Remove the Histograms Page

**Feature Branch**: `008-remove-histograms-page`

**Created**: 2026-11-26

**Status**: Draft

**Input**: User description: "Remove the `Histograms` page and all code related to it." (`docs/spec-008-draft.md`)

## Context

Feature 003 added Histograms as a placeholder page: it shows only its heading, and the navbar links
to it. No later feature gave it content. The Table, States, and Timeline pages now cover the
activity views, so the placeholder is to go, with everything that exists only for it.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The app no longer offers a Histograms page (Priority: P1)

The user opens TcxTool and sees only pages that do something. The navbar has no Histograms link,
and the Histograms address no longer shows a page.

**Why this priority**: This is the whole feature. A link to an empty page suggests a feature that
doesn't exist.

**Independent Test**: Open any page and read the navbar, then open `/histograms`.

**Acceptance Scenarios**:

1. **Given** any page, **When** the user reads the navbar, **Then** it links to Home, Convert,
   Table, States, Timeline, and About, in that order, and has no Histograms link.
2. **Given** the app is running, **When** the user opens `/histograms`, **Then** they get the app's
   standard "Not Found" page inside the usual layout, with a not-found status, the same as any
   other unknown address.
3. **Given** a current CSV is selected, **When** the user opens Home, Convert, Table, States,
   Timeline, and About, **Then** each looks and behaves exactly as before this feature.

---

### User Story 2 - Nothing is left behind for Histograms (Priority: P2)

A developer working on TcxTool finds no code, test, or guidance about a Histograms page, so they
don't maintain it or build on it by mistake.

**Why this priority**: The user asked for all related code to go. Leftovers cost maintenance and
mislead later features, but they don't change what the user sees.

**Independent Test**: Search the application, its tests, and the current developer guidance for
"Histograms", ignoring case.

**Acceptance Scenarios**:

1. **Given** the finished feature, **When** a developer searches the application code and tests
   for "histogram", ignoring case, **Then** nothing is found, apart from third-party files that are
   vendored unchanged.
2. **Given** the finished feature, **When** a developer reads the project's current guidance,
   **Then** it doesn't describe a Histograms page as existing or planned. The history of past
   features can still say that feature 003 added it, and this feature records its removal.
3. **Given** the finished feature, **When** a developer runs every quality gate, **Then** they all
   pass, and no remaining test refers to the Histograms page.

### Edge Cases

- **A bookmark or old link to `/histograms`**: it gets the standard Not Found page, not an error
  page and not a redirect.
- **Code that Histograms shared with other pages**: it stays when another page still uses it.
  For example, the current-activity loading that the guidance said Histograms "should use" is still
  used by the Table page.
- **A test that covered Histograms together with another page**: only the Histograms part goes.
  The other page keeps the same coverage.
- **The navbar's active link**: on each remaining page, exactly that page's link is active, as
  before.
- **Past feature records**: the specs, plans, and tasks of features 001–007 are history and are not
  edited, even where they mention Histograms.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The app MUST NOT have a page at `/histograms`.
- **FR-002**: A request for `/histograms` MUST get the app's standard Not Found response, the same
  as any other unknown address: the "Not Found" page in the usual layout, with a not-found status.
- **FR-003**: The navbar MUST link to Home, Convert, Table, States, Timeline, and About, in that
  order, and nothing else.
- **FR-004**: Code, styles, and assets used only by the Histograms page MUST be removed. Anything
  another page still uses MUST stay and keep working.
- **FR-005**: Tests that exist only for the Histograms page MUST be removed. Tests that also cover
  another page MUST keep that page's coverage.
- **FR-006**: After the feature, a case-insensitive search for "histogram" in the application code
  and tests MUST find nothing, apart from vendored third-party files.
- **FR-007**: The project's current developer guidance MUST NOT describe a Histograms page as
  existing or planned. Its feature history MUST record this feature.
- **FR-008**: Every other page MUST look and behave as before, including the navbar's active link.
- **FR-009**: Records of past features MUST NOT be edited.

### Key Entities

None. The feature removes a page and touches no data, files, or application state.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The navbar has exactly 6 links, and none of them is Histograms.
- **SC-002**: Opening `/histograms` gives the Not Found page with a not-found status, 100% of the
  time.
- **SC-003**: A case-insensitive search for "histogram" finds 0 matches in the application code and
  tests, apart from vendored third-party files.
- **SC-004**: Every quality gate passes, and the tests of the other 6 pages pass without changes to
  what they check, apart from the navbar's link list.

## Assumptions

- "All code related to it" covers the page, its navbar link, its tests, and its mentions in the
  current developer guidance. Code that another page also uses is not related only to Histograms,
  so it stays.
- A removed address should behave like any other unknown address. No redirect and no "this page was
  removed" notice is wanted.
- Feature history (specs 001–007, and the history table's existing rows) records what was true when
  each feature was done, so it is left as it is.
- The vendored charting library can contain the word "histogram" in its own code. It is
  third-party, used by the Timeline page, and stays unchanged.
- No data, CSV format, or state file changes, so existing CSVs and the current selection are
  unaffected.
