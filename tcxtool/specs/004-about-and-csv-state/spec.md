# Feature Specification: About Page and Current CSV State

**Feature Branch**: `004-about-and-csv-state` (feature id only; no git branch is created by `/speckit-specify`)

**Created**: 2026-11-24

**Status**: Draft

**Input**: User description, from the draft file `docs/spec-004-draft.md` (passed to
`/speckit-specify` as `@../docs/spec-004-draft.md`). The draft asks for four changes:

1. **About page**: an "About" page, shown as the last item on the top navigation bar, with the
   initial content given in the draft (reproduced under FR-003).
2. **Home page enhancement**: next to the "Convert TCX to CSV" box, a similarly sized box named
   "Select CSV File", which lets the user pick a CSV file that this application created earlier.
3. **Application state**: the CSV file converted or selected is recorded in an application state
   JSON file named `tcxtool-state.json` in the `tcxtool/` directory. The file has one key,
   `csvFilename`, whose value is the CSV's file name. Other pages will use this file.
4. **Convert page enhancement**: after a TCX file is converted, the CSV file name is stored in the
   state file at `csvFilename`.

## Clarifications

### Session 2026-11-24

- Q: The draft says the "Select CSV File" box uses a file selector. A browser file picker uploads
  the file and never gives the server its path. How does the user choose the CSV? → A: From a list
  of the CSV files in the app's CSV output directory. Nothing is uploaded (FR-015).

## User Scenarios & Testing *(mandatory)*

The user is the developer who maintains `JoakimSoftware.TcxTool` and runs it on their own machine.
The analysis pages (Table, Histograms, Timeline) will each work on one activity's CSV. Before
those pages can be built, the app needs a "current CSV" that the user sets once and every page can
find. The developer also wants an About page that explains how the app was built.

### User Story 1 - Converting a TCX file makes its CSV the current one (Priority: P1)

The developer converts a TCX file on the Convert page. The new CSV becomes the app's current CSV
without any extra step, and the choice is still there after the app restarts.

**Why this priority**: Converting is the most common way a CSV comes to exist, and it already
works. Recording its result is the smallest change that gives later pages a current CSV.

**Independent Test**: Convert a sample TCX file, then open the state file. It names the CSV the
conversion result listed. Restart the app: the state file still names it.

**Acceptance Scenarios**:

1. **Given** no state file exists, **When** the developer converts a TCX file with one activity,
   **Then** the state file is created and its `csvFilename` is the name of the CSV that was saved.
2. **Given** the state file names an earlier CSV, **When** the developer converts another TCX file,
   **Then** `csvFilename` is replaced with the new CSV's name.
3. **Given** a conversion that fails (no file, too large, not TCX, malformed), **When** the result
   is shown, **Then** the state file is unchanged.
4. **Given** a successful conversion, **When** the result is shown, **Then** it also tells the
   developer which CSV is now the current one.

---

### User Story 2 - Select an earlier CSV from the home page (Priority: P2)

The developer wants to look at an activity they converted on an earlier day. On the home page,
next to the "Convert TCX to CSV" box, a "Select CSV File" box lists the CSVs the app created, and
they pick one. The picked CSV becomes the current one.

**Why this priority**: Without it, the only way to change the current CSV is to convert the TCX
file again. It depends on the state file from Story 1.

**Independent Test**: With at least two converted CSVs available, open the home page, pick the one
that is not current, and submit. The page confirms the choice, and the state file names it.

**Acceptance Scenarios**:

1. **Given** the home page on a wide window, **When** it renders, **Then** the "Select CSV File" box
   sits to the right of the "Convert TCX to CSV" box, at the same size and in the same style.
2. **Given** CSVs created by this app are available, **When** the developer picks one in the
   "Select CSV File" box and submits, **Then** the state file's `csvFilename` becomes that CSV's
   name and the page confirms the selection.
3. **Given** the state file names a CSV, **When** the home page renders, **Then** the "Select CSV
   File" box shows that name as the current CSV.
4. **Given** no current CSV, **When** the home page renders, **Then** the box says that no CSV is
   selected yet.
5. **Given** the developer submits with no CSV chosen, or with a name that is not in the list (a
   hand-edited form, or a file deleted since the page loaded), **When** the page responds, **Then**
   it shows a clear error and the state file is unchanged.

---

### User Story 3 - Read about the app (Priority: P3)

The developer, or someone they show the app to, clicks "About" in the navigation bar and reads how
the app was built, with links to Spec Kit and to the project's repository.

**Why this priority**: It is informational, and it is independent of the other stories.

**Independent Test**: From any page, click "About", the last link in the navbar. The About page
shows the content in FR-003, formatted, with working links.

**Acceptance Scenarios**:

1. **Given** any page, **When** the developer looks at the navbar, **Then** the links read Home,
   Convert, Table, Histograms, Timeline, About, in that order.
2. **Given** any page, **When** the developer clicks About, **Then** the About page loads, its
   navbar link is marked as the current page, and its heading and browser tab title read "About".
3. **Given** the About page, **When** it renders, **Then** it shows the FR-003 content with its
   headings as headings, its bold names in bold, its commands as a list, and its links as links to
   the listed addresses.

---

### Edge Cases

- **Several activities in one TCX file**: a conversion can save more than one CSV. The current CSV
  is the first one in the conversion result's list, and the result says which one it is.
- **State file missing**: the app treats this as "no current CSV". Pages still render, and nothing
  is created until a CSV is converted or selected.
- **State file unreadable or malformed** (not JSON, no `csvFilename`, value not text): the app treats
  it as "no current CSV", logs a warning without the file's content, and does not fail the page. The
  next conversion or selection replaces it with a valid file.
- **Current CSV no longer exists**: someone deleted the CSV after it was recorded. The home page
  shows the name and says the file is missing. The state file is left as it is until the next
  conversion or selection.
- **No CSVs available yet**: the "Select CSV File" box says there are no CSVs and points to the
  Convert page. Submitting changes nothing.
- **A name that tries to leave the CSV directory** (for example `../secrets.csv` or an absolute
  path, sent from a hand-edited form): the app rejects it, and the state file is unchanged. Only a plain file name inside the CSV
  output directory can become current.
- **Writing the state file fails** (for example, a read-only location): the conversion's CSVs are
  still saved and listed. The page says the current CSV could not be recorded. The app does not
  crash.
- **Interrupted write**: the state file is replaced whole, so a reader never sees half a file.
- **Two browser tabs**: the app has one local user. When two tabs convert or select at about the
  same time, the last write wins, and the state file is always one valid choice.
- **Narrow window**: the two home page boxes stack vertically, like the other cards in the app.
- **No network**: the About page renders fully. Its links point outside the app, but nothing on
  the page loads from the network.

## Requirements *(mandatory)*

### Functional Requirements

**About page**

- **FR-001**: The app MUST serve an About page at `/about`. Its browser tab title and its single
  `h1` MUST read "About".
- **FR-002**: The navbar MUST link to the About page as its last item, after Timeline. It MUST mark
  the link active on the About page only.
- **FR-003**: The About page MUST show this content, formatted: headings as headings, bold text in
  bold, list items as a list, and links as links with the given text and addresses. The `#`
  heading is the page's `h1` from FR-001.

  ```markdown
  # About

  The **TcxTool** application was developed by **Chris Joakim** as an
  experiment of using the **Software Factory** approach to develop software
  in an entirely hands-off manner using **Claude Code** and **Spec-Kit**.

  ## Spec-Kit Process

  ## Spec-Kit Project Startup

  - /speckit-constitution - Establish project principles

  ## Spec-Kit Feature/Spec Development Loop

  - /speckit-specify - Create baseline specification
  - /speckit-plan - Create implementation plan
  - /speckit-tasks - Generate actionable tasks
  - /speckit-implement - Execute implementation
  - /speckit-converge - Assess the codebase and append remaining work as tasks

  ## Links

  - [speckit.org](https://speckit.org)
  - [spec-kit @ GitHub](https://github.com/github/spec-kit)
  - [Spec-Kit Quickstart](https://github.github.io/spec-kit/quickstart.html)
  - [specify-cli @ PyPI](https://pypi.org/project/specify-cli/)
  - [GitHub repo for this project](https://github.com/cjoakim/cj-speckit-explore)
  ```

- **FR-004**: The About page content MUST be kept in one place in the source, so it can be edited
  without changing page structure or tests other than the content tests.

**Application state**

- **FR-005**: The app MUST keep its state in a JSON file named `tcxtool-state.json`. When the app
  runs on the host, the file MUST be in the `tcxtool/` project directory. The location MUST be
  configurable, so the container and the tests can use their own.
- **FR-006**: The state file MUST be a JSON object with exactly one key, `csvFilename`. Its value
  MUST be the CSV's file name only (for example `2026-11-01_24932322230.csv`), with no directory.
  The name refers to a file in the CSV output directory from feature 002.
- **FR-007**: The app MUST replace the state file whole on each write, so it is never left half
  written.
- **FR-008**: A missing, unreadable, or malformed state file MUST be treated as "no current CSV".
  It MUST NOT cause an error page. A malformed file MUST be logged as a warning with the file's
  path and the reason, never its content.
- **FR-009**: The app MUST provide one shared way to read the current CSV: its file name, whether
  that file exists in the CSV output directory, or that there is none. Pages MUST NOT read the state
  file themselves (Principle III: components bind and display).
- **FR-010**: The app MUST accept as the current CSV only a plain file name with a `.csv` extension
  that names a file in the CSV output directory. Names with directory parts, `..`, or absolute paths
  MUST be rejected.

**Convert page**

- **FR-011**: After a successful conversion, the app MUST record the first CSV in the conversion
  result's list as the current CSV.
- **FR-012**: The conversion result MUST say which CSV is now current. If recording it failed, the
  result MUST still list the saved CSVs and MUST say the current CSV could not be recorded.
- **FR-013**: A failed conversion MUST NOT change the state file.

**Home page**

- **FR-014**: The home page MUST show a "Select CSV File" card to the right of the "Convert TCX to
  CSV" card, the same size and style, stacking below it on narrow windows.
- **FR-015**: The "Select CSV File" card MUST let the user choose a CSV from a list of the `.csv`
  files in the CSV output directory, and submit the choice. Nothing is uploaded. The list MUST show
  file names only, newest first (by the date at the start of the name, then by name), and MUST
  preselect the current CSV when it is in the list. Files without a `.csv` extension and
  subdirectories MUST NOT be listed.
- **FR-016**: A successful selection MUST record the chosen CSV as current and confirm it on the
  page. A missing or invalid choice MUST show an error and MUST NOT change the state file.
- **FR-017**: The "Select CSV File" card MUST show the current CSV's name, or say none is selected,
  or say the recorded file is missing (FR-009).

**Constraints**

- **FR-018**: Every page in this feature MUST stay static server-rendered unless the plan records
  why a component needs an interactive render mode (Principle I).
- **FR-019**: The existing page contracts from features 001 to 003 MUST still hold, apart from the
  navbar list, which this feature extends.
- **FR-020**: Tests MUST NOT read or write the developer's real `tcxtool/tcxtool-state.json`.
- **FR-021**: The state file is local to the machine and MUST NOT be committed to source control
  or copied into the Docker image.

### Key Entities

- **Application state**: the app's memory between requests and restarts. It holds one fact, the
  current CSV's file name. It lives in `tcxtool-state.json`, and absent or invalid means "none".
- **Current CSV**: the CSV that the analysis pages will work on. It is identified by file name
  within the CSV output directory. It is set by a successful conversion or a selection on the home
  page.
- **CSV file**: an output of feature 002, in the CSV output directory, in the format of
  `specs/002-tcx-to-csv/contracts/csv-format.md`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 4 constitution Quality Gates pass, including the Compose gate with exit status 0.
- **SC-002**: After a successful conversion, the state file names the new CSV with 0 extra steps by
  the user.
- **SC-003**: From the home page, the developer can make an earlier CSV current in at most 2
  actions (choose, submit).
- **SC-004**: In 100% of the edge cases listed (missing, malformed, or unwritable state file;
  missing CSV; bad name), the page renders without an error page, and the state file is either
  unchanged or valid.
- **SC-005**: The current CSV survives an app restart in 100% of cases where the state file was
  written.
- **SC-006**: From any page, the About page is reachable in 1 click, and every one of its 5 links
  points to the address in FR-003.
- **SC-007**: After the test suite runs, the developer's real `tcxtool/tcxtool-state.json` is
  unchanged.

## Assumptions

- **Draft clean-up**: the draft's About content contained stray box-drawing characters (`│`) and
  trailing spaces from a terminal copy. They are removed. The words, headings, order, and links
  are kept as written, including the empty "Spec-Kit Process" heading and the loop's list, which
  does not mention `/speckit-clarify` or `/speckit-analyze`.
- **Draft count**: the draft's overview says "three features" and lists four. The four sections are
  the scope.
- **"Converted or selected on the Home page"**: the Home page links to the Convert page, which does
  the conversion. So a conversion on the Convert page counts, as the draft's section 4 says.
- **File name only**: `csvFilename` holds the name, not a path. It resolves against the CSV output
  directory, so the same state file works on the host and in the container, where that directory is
  bind-mounted from `tcxtool/data/csv`.
- **Container location**: in the container there is no `tcxtool/` directory. The plan chooses where
  the state file lives there. State that is lost when the container is removed is acceptable,
  because the app runs mainly on the host.
- **First CSV wins**: when one TCX file has several activities, the first CSV becomes current. The
  developer can pick another from the home page.
- **Other pages**: the draft says other pages will use the state file. In this feature, the Table,
  Histograms, and Timeline pages stay placeholders. This feature supplies the shared reader
  (FR-009) that they will use later.
- **Source control**: `tcxtool-state.json` is git-ignored. It names a local file and changes on
  every run.
- **Single user**: the app runs locally for one person. No locking beyond whole-file replacement is
  needed.
- **List order**: CSV names start with the activity date (`yyyy-MM-dd`), so sorting names in
  descending order puts the newest activity first.
- **External links**: the About links are ordinary links. Tests check their addresses, not that the
  sites respond.
