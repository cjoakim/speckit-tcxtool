# Feature Specification: Project Bootstrap

**Feature Branch**: `main` (no feature branch created; spec directory is `specs/001-project-bootstrap`)

**Created**: 2026-11-22

**Status**: Draft

**Input**: User description: "Bootstrap this dotnet blazor project with `dotnet new`"

## User Scenarios & Testing *(mandatory)*

The user of this feature is the developer who maintains `JoakimSoftware.TcxTool`. The feature
delivers no end-user functionality. It delivers an empty, runnable application that already passes
every quality gate in the constitution, so each later feature starts from a known-good baseline.

### User Story 1 - Run the empty application locally (Priority: P1)

From a clean checkout, the developer builds and launches the application on their machine and sees
a home page that names the application. Nothing else is on the page yet.

**Why this priority**: Every later feature needs a running application to build on. Without it,
nothing else in this feature can be shown to work.

**Independent Test**: From a clean checkout, follow only the documented commands to build and launch
the application, open it in a browser, and confirm the home page shows the application name.

**Acceptance Scenarios**:

1. **Given** a clean checkout and the required SDK installed, **When** the developer runs the
   documented build command, **Then** the build succeeds with zero warnings and zero errors.
2. **Given** a successful build, **When** the developer runs the documented launch command and
   opens the printed address, **Then** a home page shows the title "JoakimSoftware.TcxTool" and a
   one-sentence description of the tool.
3. **Given** the application is running, **When** the developer looks for demo content that came
   with the starting templates (for example, sample counter or weather pages), **Then** none is
   present in navigation or reachable by address.

---

### User Story 2 - Quality gates pass on the skeleton (Priority: P2)

The developer runs the build, format check, and test commands from the constitution's Quality Gates
and all of them pass. The test suite already includes a UI test and an architecture test, so later
features add tests to working infrastructure instead of setting it up first.

**Why this priority**: The constitution says a change is complete only when every gate passes.
Gates that only start working halfway through feature 2 would block that feature for reasons
unrelated to it.

**Independent Test**: Run the build, format-check, and test commands. All three exit successfully,
and the test report lists at least the home-page test and the parser-independence test.

**Acceptance Scenarios**:

1. **Given** the bootstrapped project, **When** the developer runs the format check, **Then** it
   reports no changes needed.
2. **Given** the bootstrapped project, **When** the developer runs the test command, **Then** all
   tests pass, including a test that renders the home page and checks the application name.
3. **Given** the bootstrapped project, **When** the parsing library is given a reference to the web
   application or web UI framework, **Then** the parser-independence test fails.
4. **Given** the bootstrapped project, **When** a developer adds code that produces a compiler or
   analyzer warning, **Then** the build fails.

---

### User Story 3 - Run and test inside containers (Priority: P3)

The developer builds the container image, runs the application in a container, and runs the
containerized test gate. The gate's exit status matches the test result.

**Why this priority**: The containerized gate is the final quality gate. It depends on the
skeleton from Stories 1 and 2, and it is also the path to the later Azure deployment.

**Independent Test**: Run the documented container commands. The application answers on port 8080
from the container, and the test gate exits with success. Add a deliberately failing test and the
gate exits with failure. Remove the failing test afterwards.

**Acceptance Scenarios**:

1. **Given** a container runtime on the developer's machine, **When** the developer starts the
   application with the documented container command, **Then** the home page is reachable on port
   8080 and the application process runs as a non-root user.
2. **Given** the bootstrapped project, **When** the developer runs the containerized test gate,
   **Then** the tests run inside the build environment and the gate exits with status 0.
3. **Given** one deliberately failing test, **When** the developer runs the containerized test gate,
   **Then** the gate exits with a non-zero status.
4. **Given** a built runtime image, **When** the developer lists its contents, **Then** it contains
   no SDK tooling, no sample activity files, no source-control metadata, no build intermediates, and
   no local secrets.

---

### Edge Cases

- **Wrong SDK installed**: if the required SDK version is missing, the build fails with a message
  naming the required version instead of building silently with a different one.
- **Port 8080 already in use on the host**: the documentation shows how to map the container to a
  different host port.
- **Large sample files in the working directory**: the real activity exports in `data/tcx/` (about
  13–15 MB each) are not sent to the container build or copied into any image.
- **Application container fails to start**: the test service never waits on the application
  container's health, so a slow or unhealthy application does not delay or block the tests. If the
  application container exits early, the gate stops and reports failure. A runtime image that
  cannot start is a failed gate. (Clarified during `/speckit-plan`; see research.md R7.)
- **No network while tests run**: the tests pass without network access. Network access is needed
  only to download packages and base images during the build.
- **Commands run from the git repository root**: the documentation states that commands run from
  `tcxtool/`, since the git root is the parent directory.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The project MUST contain exactly the three components the constitution requires: a TCX
  parsing library, a web application, and a test project. All of them use the root namespace
  `JoakimSoftware.TcxTool`.
- **FR-002**: The parsing library MUST NOT depend on the web application or the web UI framework
  (Principle III). An automated test MUST check this.
- **FR-003**: The web application MUST serve a home page showing the application name and a
  one-sentence description, using static server rendering by default (Principle I).
- **FR-004**: Demo pages and sample code from the starting templates MUST be removed. Only the home
  page, the shared layout, and the standard error and not-found handling remain.
- **FR-005**: Every component MUST enable the project settings Principle II requires: nullable
  reference checks, implicit usings, warnings treated as errors, and the latest analysis level. A
  shared code-style configuration MUST sit at the project root.
- **FR-006**: The project MUST declare the required SDK version (.NET 11) so that a build on a
  different SDK version fails with a clear message.
- **FR-007**: The test project MUST reference the test packages Principle V requires. It MUST contain
  at least the home-page rendering test (FR-003) and the parser-independence test (FR-002).
- **FR-008**: A multi-stage container definition MUST build and package the web application as
  Principle VI requires: SDK only in the build stage, a runtime-only final image, a non-root user,
  and port 8080.
- **FR-009**: A container build exclusion list MUST keep source-control metadata, build outputs,
  test results, local secrets, and the `data/` sample files out of the container build.
- **FR-010**: A compose configuration MUST run the application from the container definition. A
  test overlay MUST run the test suite in the build stage and return the test process's exit code,
  using the gate command in Principle VII.
- **FR-011**: All four constitution Quality Gates MUST pass on the bootstrapped project: build with
  warnings as errors, format check, tests, and the containerized gate.
- **FR-012**: The "Build and test commands" section of `CLAUDE.md` MUST be filled in with the
  solution layout and the exact commands to build, format-check, test, run locally, run in a
  container, and run the containerized test gate. Commands are run from `tcxtool/`.
- **FR-013**: This feature MUST NOT add TCX parsing, file upload, CSV conversion, analysis, or
  charts. Those belong to later features.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: From a clean checkout with the required SDK installed, a developer reaches the running
  home page in under 5 minutes using only the documented commands, not counting first-time package
  downloads.
- **SC-002**: All 4 Quality Gates pass on the first run, with no manual fixes after setup.
- **SC-003**: The build reports 0 warnings and the format check reports 0 files needing changes.
- **SC-004**: The containerized test gate returns success when all tests pass, and failure when 1
  deliberately failing test is added. Both results come from the gate's exit status alone.
- **SC-005**: The containerized application serves the home page on port 8080 within 30 seconds of
  container start.
- **SC-006**: The runtime image contains 0 SDK tools, 0 sample activity files, and 0 source-control
  files, checked by listing the image contents.

## Assumptions

- The only user of this feature is the developer who maintains the project. No end-user
  functionality is in scope.
- As the input asks, the standard .NET project templates (`dotnet new`) are the starting point for
  each component. The plan chooses which templates and options to use.
- The solution sits at the root of `tcxtool/` (the Spec Kit project root). The git repository root
  is the parent directory, `cj-speckit-explore/`.
- Test-first (Principle V) applies to code this feature writes, such as the home page content and
  the parser-independence check. Files generated by the templates are the baseline.
- The home page is a placeholder that later features will replace or extend.
- A container runtime with compose support (Docker Desktop or equivalent) is installed on the
  developer's macOS machine.
- Authentication, persistence, health endpoints, and Azure deployment are out of scope for this
  feature.
