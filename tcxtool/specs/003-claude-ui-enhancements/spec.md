# Feature Specification: Claude UI Enhancements

**Feature Branch**: `main` (no feature branch created; spec directory is `specs/003-claude-ui-enhancements`)

**Created**: 2026-11-24

**Status**: Implemented (reverse-engineered from the delivered code)

**Input**: User descriptions, given directly to Claude Code rather than through `/speckit-specify`:

1. "I want to enhance the UI of the tcxtool application, and have it use the Bootstrap UI/CSS
   framework as described here: https://getbootstrap.com. Please see my running Python-based PLog
   application here: http://localhost:8000. I want to replicate the look-and-feel of the PLog app
   into this tcxtool app. Please add Bootstrap to the tcxtool app. The plog code is here:
   /Users/cjoakim/github/cj-plog/python. Don't modify the PLog code!"
2. "In the TcxTool UI, add the following pages: Table, Histograms, Timeline. These pages should
   just show their page Name in their content initially. Enhance the Top-Nav Navigation to enable
   navigation to any of these pages."

> **Reverse-engineered spec.** The code was written first, test-first but outside the Spec Kit
> command sequence, at the maintainer's request. This spec, the plan, and the tasks record what
> was built and why, so later features (and `/speckit-converge`) have a baseline. See plan.md
> "Complexity Tracking" for the process deviation.

## User Scenarios & Testing *(mandatory)*

The user is the developer who maintains `JoakimSoftware.TcxTool` and also runs the PLog app. They
want both apps to look like one family of tools, and they want the navigation for the analysis
pages in place before those pages are built.

### User Story 1 - The app looks like PLog (Priority: P1)

The developer opens any TcxTool page and sees the same page shell as PLog: a dark top navigation
bar with the app name on the left, the page content in a centred container, and a light footer.
Fonts, colours, spacing, buttons, and form controls match PLog's.

**Why this priority**: This is the core of the request. Every other change in this feature sits
inside this shell.

**Independent Test**: Open `/` in TcxTool and `/` in PLog side by side. The navbar, container
width, typography, and footer look the same. View the page source: the stylesheet and script come
from the TcxTool server, not from a CDN.

**Acceptance Scenarios**:

1. **Given** the app is running, **When** the developer opens any page, **Then** it shows a dark
   navbar whose brand reads "TcxTool" and links to the home page, the page content in a container,
   and a footer reading "© 2026 Chris Joakim".
2. **Given** the app is running with no network access, **When** the developer opens any page,
   **Then** the page is fully styled, because the CSS and JS are served by the app itself.
3. **Given** the PLog app's source, **When** this feature is complete, **Then** the PLog source
   tree has no changes.

---

### User Story 2 - Existing pages use the new styles (Priority: P2)

The home page, the convert page, the conversion result, and the not-found page use the framework's
standard components, so they look like PLog's pages, without changing what they say or how they
behave.

**Why this priority**: Without it, the shell would wrap unstyled content. It depends on Story 1's
stylesheet.

**Independent Test**: Upload a sample TCX file on `/convert` and see the form on a card, a primary
"Convert" button, and the result in a green alert. Upload a non-TCX file and see a red alert.

**Acceptance Scenarios**:

1. **Given** the home page, **When** it renders, **Then** the title and one-sentence description
   appear first, followed by a card with a primary "Convert" button linking to `/convert`.
2. **Given** the convert page, **When** it renders, **Then** the upload form is on a card, the file
   input and label use the framework's form styles, the size limit appears as hint text under the
   input, and the submit button is a primary button.
3. **Given** a successful conversion, **When** the result renders, **Then** it is a success alert
   listing each saved file.
4. **Given** a failed conversion, **When** the result renders, **Then** it is a danger alert showing
   the error message.
5. **Given** the page contracts from features 001 and 002, **When** this feature is complete,
   **Then** every element and attribute those contracts require is still present.

---

### User Story 3 - Navigate to the analysis pages (Priority: P3)

The developer uses the top navigation to reach three new pages, Table, Histograms, and Timeline.
For now, each page shows only its name. Later features fill them in.

**Why this priority**: The pages have no content yet. The value is the navigation structure that
later analysis features will build on.

**Independent Test**: From the home page, click each navbar link in turn. Each page loads, shows
its name as its heading and browser tab title, and its navbar link is highlighted.

**Acceptance Scenarios**:

1. **Given** any page, **When** the developer looks at the navbar, **Then** it shows the links
   Home, Convert, Table, Histograms, and Timeline, in that order.
2. **Given** any page, **When** the developer clicks Table, Histograms, or Timeline, **Then** the
   page at `/table`, `/histograms`, or `/timeline` loads and shows only its name.
3. **Given** a page reached from the navbar, **When** it renders, **Then** that page's navbar link
   is marked as the current page and no other link is.

---

### Edge Cases

- **No network**: pages still render fully styled, and the test suite still passes inside the
  Compose gate's `network_mode: none` container.
- **Narrow browser window**: as in PLog, the navbar has no collapse toggle. Below the large
  breakpoint the links stack vertically under the brand. This matches PLog and is accepted.
- **Browser caching of the stylesheet**: asset URLs carry a content fingerprint, so a later
  Bootstrap upgrade is picked up without a hard refresh.
- **Source control ignores `lib/`**: the repository-root `.gitignore` ignores every `lib/`
  directory (a Python template rule). The vendored framework files must still be tracked, or a
  clean clone has no styles and the asset tests fail.
- **Unknown route**: the not-found page renders inside the same shell, with a styled heading.
- **Keyboard focus after navigation**: the router focuses the page's `h1`. Each page keeps exactly
  one `h1`, styled to PLog's heading size.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The web app MUST use the Bootstrap 5 CSS framework, with its CSS and its JavaScript
  bundle served by the app itself. Pages MUST NOT load them from a CDN or any other network
  location.
- **FR-002**: The Bootstrap version MUST match PLog's, so both apps render the same way.
- **FR-003**: Every page MUST render inside a shared layout made of a dark navbar, a container
  holding the page content, and a light footer, as PLog's base template does. The navbar brand MUST
  read "TcxTool" and link to the home page. The footer MUST read "© 2026 Chris Joakim".
- **FR-004**: The navbar MUST link to Home, Convert, Table, Histograms, and Timeline, in that
  order. It MUST mark the current page's link as active. It MUST NOT link to pages that do not
  exist.
- **FR-005**: Each page MUST keep exactly one `h1` (contracts from features 001 and 002), styled
  at the size PLog uses for page titles.
- **FR-006**: The home page MUST show its description directly after the `h1`, then a card that
  links to `/convert` with a primary button.
- **FR-007**: The convert page MUST place its form on a card and use Bootstrap form-label,
  form-control, form-text, and primary-button styles. All markup required by
  `specs/002-tcx-to-csv/contracts/convert-page.md` MUST remain.
- **FR-008**: A successful conversion result MUST render as a success alert, and a failed one as a
  danger alert. The `status` and `alert` roles from feature 002 MUST remain.
- **FR-009**: The app MUST serve three pages, Table at `/table`, Histograms at `/histograms`, and
  Timeline at `/timeline`. Each MUST show only its name, as both the browser tab title and the
  `h1`.
- **FR-010**: No component in this feature may add an interactive render mode. Every page stays
  static server-rendered (Principle I).
- **FR-011**: The vendored Bootstrap files MUST be tracked by source control, despite the
  repository-root `lib/` ignore rule.
- **FR-012**: The PLog source tree MUST NOT be modified.
- **FR-013**: The feature MUST NOT add a project, a NuGet package, or a client-side build tool.
- **FR-014**: `CLAUDE.md` MUST record the UI styling convention so later features follow it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 4 constitution Quality Gates pass, including the Compose gate with exit status 0.
- **SC-002**: Rendering any page makes 0 requests to hosts other than the app.
- **SC-003**: From any page, each of the 5 navbar destinations is reachable in 1 click.
- **SC-004**: Every test from features 001 and 002 still passes unchanged, apart from the one
  navbar-order assertion this feature owns.
- **SC-005**: `git status` in the PLog repository shows 0 changed files after the feature.
- **SC-006**: A side-by-side look at TcxTool and PLog shows the same navbar, container, typography,
  and footer. (This is a manual check.)

## Assumptions

- "Replicate the look-and-feel" means PLog's page shell and stock Bootstrap components. It does not
  mean PLog's navigation items, which belong to PLog.
- The navbar brand is "TcxTool", which is short like PLog's "PLog". The full name
  `JoakimSoftware.TcxTool` stays the home page `h1`, as feature 001 requires.
- PLog's Chart.js script is not copied. Charting belongs to a later feature, which chooses its own
  library.
- The Table, Histograms, and Timeline pages are placeholders. Their content, data sources, and any
  interactivity are out of scope and belong to later features.
- The footer text is copied from PLog verbatim, because both apps have the same author.
