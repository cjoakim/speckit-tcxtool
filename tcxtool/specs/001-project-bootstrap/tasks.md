---

description: "Task list for feature 001-project-bootstrap"
---

# Tasks: Project Bootstrap

**Input**: Design documents from `specs/001-project-bootstrap/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included. The constitution requires test-first (Principle V, NON-NEGOTIABLE), and the
spec requires automated tests in FR-002 and FR-007. Each test task comes before the code it covers
and must be seen failing first.

**Organization**: Tasks are grouped by user story so each story can be implemented and tested on
its own.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task belongs to (US1, US2, US3)
- All paths are relative to `tcxtool/` (the Spec Kit project root). Run every command from
  `tcxtool/`. The git root is the parent directory.

## Path Conventions

These come from plan.md → Project Structure:

- Production code: `src/JoakimSoftware.TcxTool.Parser/` and `src/JoakimSoftware.TcxTool.Web/`
- Tests: `tests/JoakimSoftware.TcxTool.Tests/`
- Solution and root config: the `tcxtool/` root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the solution and the three projects from the `dotnet new` templates.

- [X] T001 Create `global.json` with `dotnet new globaljson --sdk-version 11.0.100 --roll-forward latestFeature` (research.md R6). Confirm `dotnet --version` prints 11.0.1xx.
- [X] T002 [P] Create `.gitignore` with `dotnet new gitignore`. The repo-root `.gitignore` has no rules for `bin/`, `obj/`, or `TestResults/`. Append `.DS_Store`.
- [X] T003 Create the solution `JoakimSoftware.TcxTool.slnx` with `dotnet new sln -n JoakimSoftware.TcxTool` (research.md R1). Confirm the file extension is `.slnx`.
- [X] T004 [P] Create the parser library with `dotnet new classlib -n JoakimSoftware.TcxTool.Parser -o src/JoakimSoftware.TcxTool.Parser`, then delete `src/JoakimSoftware.TcxTool.Parser/Class1.cs` (research.md R3).
- [X] T005 [P] Create the web app with `dotnet new blazor -n JoakimSoftware.TcxTool.Web -o src/JoakimSoftware.TcxTool.Web --interactivity Server --empty --no-https` (research.md R2). Do not pass `--all-interactive`.
- [X] T006 [P] Create the test project with `dotnet new xunit -n JoakimSoftware.TcxTool.Tests -o tests/JoakimSoftware.TcxTool.Tests`, then delete `tests/JoakimSoftware.TcxTool.Tests/UnitTest1.cs` (research.md R3).
- [X] T007 Add the three projects to `JoakimSoftware.TcxTool.slnx` with `dotnet sln JoakimSoftware.TcxTool.slnx add` for each `.csproj` from T004–T006.
- [X] T008 [P] Create `.editorconfig` with `dotnet new editorconfig`, then add (research.md R5):
  - `csharp_style_namespace_declarations = file_scoped:warning`
  - a naming rule requiring `_camelCase` for private fields
  - a naming rule requiring the `Async` suffix on async methods
- [X] T009 Run `dotnet build JoakimSoftware.TcxTool.slnx` and confirm the untouched template baseline builds before any settings are tightened.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Test infrastructure, the architecture tests, then the project settings and references
those tests enforce. This implements the structural model in data-model.md, test-first
(Principle V).

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Test infrastructure

- [X] T010 Edit `tests/JoakimSoftware.TcxTool.Tests/JoakimSoftware.TcxTool.Tests.csproj`:
  - Keep `Sdk="Microsoft.NET.Sdk"` per data-model.md › `JoakimSoftware.TcxTool.Tests`.Kind.
  - Set `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest`, and `EnforceCodeStyleInBuild=true` per data-model.md › Validation rules (project settings).
  - Set `RootNamespace` per data-model.md › `JoakimSoftware.TcxTool.Tests`.Root namespace.
  - Add `ProjectReference`s to Parser and Web per data-model.md › `JoakimSoftware.TcxTool.Tests`.Depends on.
  - Add packages `bunit` 2.15.2 and `FluentAssertions` 7.2.2 (research.md R4). Keep the template versions of `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, and `coverlet.collector` (plan.md Testing).
  - Add global `<Using Include="FluentAssertions" />` next to the existing `Xunit` using.
  - Do **not** touch the Parser or Web `.csproj` yet.
- [X] T011 [P] Create `tests/JoakimSoftware.TcxTool.Tests/Support/SecureXml.cs`: one public static class whose `Load(string path)` returns an `XDocument` read through `XmlReader.Create` with `DtdProcessing = DtdProcessing.Prohibit`, `XmlResolver = null`, and a bounded `MaxCharactersFromEntities` (Principle IV). Never use `XDocument.Load(path)` or `XDocument.Parse`.
- [X] T012 [P] Create `tests/JoakimSoftware.TcxTool.Tests/Support/RepositoryRoot.cs`: one public static class that walks up from `AppContext.BaseDirectory` to the directory holding `JoakimSoftware.TcxTool.slnx`, and exposes that path (research.md R8).

### Architecture tests (write first, see them fail)

> **NOTE: Write these tests FIRST, and make sure they FAIL before T015 and T016.**

- [X] T013 Write `tests/JoakimSoftware.TcxTool.Tests/Architecture/ProjectSettingsTests.cs` (contracts/quality-gates.md "Automated checks"):
  - Read every `Project` path from the `.slnx` through `SecureXml`.
  - Assert the solution lists exactly the three projects in data-model.md › Validation rules (exactly three projects).
  - Assert each `.csproj` declares the four settings in data-model.md › Validation rules (project settings).
  - Run `dotnet test` and confirm it **fails** for Parser and Web, whose template `.csproj` files lack `TreatWarningsAsErrors` and `AnalysisLevel`.
- [X] T014 [P] Write `tests/JoakimSoftware.TcxTool.Tests/Architecture/ParserIndependenceTests.cs`. It enforces data-model.md › `JoakimSoftware.TcxTool.Parser`.Kind and `JoakimSoftware.TcxTool.Parser`.Must not depend on in two ways (research.md R8):
  - **Project-file check** (read through `SecureXml`): the `.csproj` has `Sdk="Microsoft.NET.Sdk"`, no `FrameworkReference`, no `ProjectReference` containing `JoakimSoftware.TcxTool.Web`, and no `Microsoft.AspNetCore*` `PackageReference`.
  - **Assembly check**: `Assembly.Load("JoakimSoftware.TcxTool.Parser").GetReferencedAssemblies()` contains no `Microsoft.AspNetCore*` and no `JoakimSoftware.TcxTool.Web`.
  - This test guards against something being added, so the untouched template passes it. Show it fails by temporarily adding `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to the Parser `.csproj`, run `dotnet test`, then remove it (quickstart.md Scenario 2).

### Implementation (makes T013 pass)

- [X] T015 [P] Edit `src/JoakimSoftware.TcxTool.Parser/JoakimSoftware.TcxTool.Parser.csproj`:
  - Keep `Sdk="Microsoft.NET.Sdk"` per data-model.md › `JoakimSoftware.TcxTool.Parser`.Kind.
  - Set the four settings per data-model.md › Validation rules (project settings), plus `EnforceCodeStyleInBuild=true` and `GenerateDocumentationFile=true` (research.md R5).
  - Set `RootNamespace` per data-model.md › `JoakimSoftware.TcxTool.Parser`.Root namespace.
  - Add no references, per data-model.md › `JoakimSoftware.TcxTool.Parser`.Depends on (BCL only) and `JoakimSoftware.TcxTool.Parser`.Must not depend on.
- [X] T016 [P] Edit `src/JoakimSoftware.TcxTool.Web/JoakimSoftware.TcxTool.Web.csproj`:
  - Keep `Sdk="Microsoft.NET.Sdk.Web"` per data-model.md › `JoakimSoftware.TcxTool.Web`.Kind.
  - Set the four settings per data-model.md › Validation rules (project settings), plus `EnforceCodeStyleInBuild=true`.
  - Set `RootNamespace` per data-model.md › `JoakimSoftware.TcxTool.Web`.Root namespace.
  - Add a `ProjectReference` to Parser per data-model.md › `JoakimSoftware.TcxTool.Web`.Depends on. Add no reference to the test project, per data-model.md › `JoakimSoftware.TcxTool.Web`.Must not depend on.
  - Keep the template's `BlazorDisableThrowNavigationException`.
- [X] T017 Run `dotnet format JoakimSoftware.TcxTool.slnx`, `dotnet build JoakimSoftware.TcxTool.slnx`, and `dotnet test JoakimSoftware.TcxTool.slnx`:
  - Fix every warning or error the stricter settings expose in template-generated code under `src/JoakimSoftware.TcxTool.Web/`, for example in `Program.cs` and `Components/*.razor`. Fix the code; never relax a setting or suppress a rule globally.
  - Done when the build reports `0 Warning(s)` and `0 Error(s)` and T013 and T014 pass.

**Checkpoint**: Strict build is green and the architecture tests pass. User story phases can begin.

---

## Phase 3: User Story 1 - Run the empty application locally (Priority: P1) 🎯 MVP

**Goal**: A clean build launches a home page that names the tool, with no template demo content
(FR-003, FR-004).

**Independent Test**: `dotnet build` then `dotnet run --project src/JoakimSoftware.TcxTool.Web`.
The printed URL shows the `JoakimSoftware.TcxTool` heading and description, and `/counter` shows
the not-found page (quickstart.md Scenario 1).

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, and make sure they FAIL before implementation.**

- [X] T018 [US1] Write `tests/JoakimSoftware.TcxTool.Tests/Web/HomePageTests.cs` per contracts/home-page.md "Content of /".
  - The test uses bUnit 2 (`BunitContext`, `Render<Home>()`) and FluentAssertions.
  - It asserts that exactly one `h1` has the text `JoakimSoftware.TcxTool`, and that the `p` right after it has non-empty text.
  - Use a file-scoped namespace `JoakimSoftware.TcxTool.Tests.Web`.
  - Run `dotnet test` and confirm it **fails** against the template's "Hello, world!" page.

### Implementation for User Story 1

- [X] T019 [US1] Update `src/JoakimSoftware.TcxTool.Web/Components/Pages/Home.razor` per contracts/home-page.md.
  - Use `@page "/"` with no `@rendermode`, so the page stays static SSR.
  - Add `<PageTitle>JoakimSoftware.TcxTool</PageTitle>`, one `<h1>JoakimSoftware.TcxTool</h1>`, and one `<p>` with the one-sentence description. Include no links to features that don't exist yet.
  - Confirm that T018 now passes.
- [X] T020 [US1] Check `src/JoakimSoftware.TcxTool.Web/Components/App.razor`, `Components/Routes.razor`, and `Components/Layout/MainLayout.razor` for leftover references to template demo routes or styling (`/counter`, `/weather`, Bootstrap), and remove any you find (FR-004).
- [X] T021 [US1] Run quickstart.md Scenario 1 with `dotnet run --project src/JoakimSoftware.TcxTool.Web`.
  - Confirm that `/` returns 200 with the heading and description.
  - Confirm that `/counter` and `/weather` return the 404 not-found page (contracts/home-page.md Route table).

**Checkpoint**: User Story 1 works on its own. This is the MVP.

---

## Phase 4: User Story 2 - Quality gates pass on the skeleton (Priority: P2)

**Goal**: Quality gates 1–3 are green on the full skeleton, and the warnings-as-errors guard is
proven (FR-005, FR-007, FR-011 gates 1–3). The architecture tests that enforce FR-002 and FR-005
are written test-first in Phase 2 (T013, T014).

**Independent Test**: `dotnet format JoakimSoftware.TcxTool.slnx --verify-no-changes` and
`dotnet test JoakimSoftware.TcxTool.slnx` both exit 0, and the test list includes
`HomePageTests`, `ParserIndependenceTests`, and `ProjectSettingsTests`. Then the negative checks in
quickstart.md Scenario 2 must fail as expected.

### Implementation for User Story 2

- [X] T022 [US2] Run quality gates 1–3 in order:
  - `dotnet build JoakimSoftware.TcxTool.slnx` must report 0 warnings.
  - `dotnet format JoakimSoftware.TcxTool.slnx --verify-no-changes` must exit 0.
  - `dotnet test JoakimSoftware.TcxTool.slnx` must exit 0, with all 3 test classes listed.
  - Fix any failure in the file it points to.
- [X] T023 [US2] Run the warnings-as-errors negative check (quickstart.md Scenario 2):
  - Add an unused local variable to `src/JoakimSoftware.TcxTool.Web/Program.cs`.
  - Confirm `dotnet build` fails.
  - Revert the change and confirm the build is clean again.

**Checkpoint**: User Stories 1 and 2 both work. Quality gates 1–3 are green.

---

## Phase 5: User Story 3 - Run and test inside containers (Priority: P3)

**Goal**: A non-root runtime image on port 8080, plus the Compose test gate whose exit code is the
test result (FR-008–FR-010, FR-011 gate 4).

**Independent Test**: quickstart.md Scenario 3.
- The `app` container answers 200 on `http://localhost:8080/` within 30 s of start.
- It runs as UID 1654 and has no SDK and no `.tcx` files.
- The gate command exits 0, and exits non-zero with one deliberately failing test.

### Implementation for User Story 3

- [X] T024 [P] [US3] Create `.dockerignore` excluding `.git`, `**/bin`, `**/obj`, `**/TestResults`, `data/`, `specs/`, `docs/`, `.specify/`, `.claude/`, `*.env`, `**/secrets*`, and `.DS_Store` (research.md R7, FR-009).
- [X] T025 [P] [US3] Create `tests/JoakimSoftware.TcxTool.Tests/Fixtures/README.md`. It explains that this directory holds the checked-in `.tcx` fixtures from Principle V, arriving in the first parser feature, and that the `tests` Compose service mounts it read-only.
- [X] T026 [P] [US3] Create `Dockerfile` with two stages (research.md R7, Principle VI):
  - **Stage `build`** (`mcr.microsoft.com/dotnet/sdk:11.0`, `WORKDIR /src`):
    - Copy `global.json`, `JoakimSoftware.TcxTool.slnx`, `.editorconfig`, and each of the three `.csproj` files to their relative paths.
    - Run `dotnet restore JoakimSoftware.TcxTool.slnx`.
    - Copy the remaining source with `COPY . .`.
    - Run `dotnet publish src/JoakimSoftware.TcxTool.Web/JoakimSoftware.TcxTool.Web.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false`.
  - **Stage `runtime`** (`mcr.microsoft.com/dotnet/aspnet:11.0`, `WORKDIR /app`):
    - Set `ENV ASPNETCORE_HTTP_PORTS=8080` and `EXPOSE 8080`.
    - `COPY --from=build /app/publish .`
    - `USER $APP_UID`
    - `ENTRYPOINT ["dotnet", "JoakimSoftware.TcxTool.Web.dll"]`
- [X] T027 [P] [US3] Create `compose.yaml` with service `app` (contracts/quality-gates.md "Compose services"):
  - `build: { context: ., target: runtime }`
  - `image: joakimsoftware/tcxtool:dev`
  - `ports: ["${TCXTOOL_HOST_PORT:-8080}:8080"]`
- [X] T028 [P] [US3] Create `compose.test.yaml` with service `tests` (research.md R7, Principle VII):
  - `build: { context: ., target: build }`
  - `working_dir: /src`
  - `command: ["dotnet", "test", "JoakimSoftware.TcxTool.slnx", "--no-restore"]`
  - `network_mode: none`
  - environment: `DOTNET_NOLOGO=1`, `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1`
  - a read-only bind mount `./tests/JoakimSoftware.TcxTool.Tests/Fixtures:/src/tests/JoakimSoftware.TcxTool.Tests/Fixtures:ro`
  - no `depends_on`
- [X] T029 [US3] Start Docker Desktop and confirm `docker info` succeeds (research.md "Open items").
- [X] T030 [US3] Run the `app` checks in quickstart.md Scenario 3 against `compose.yaml`:
  - `docker compose build app`, then time the start-up (SC-005):
    ```zsh
    docker compose up -d app; start=$SECONDS
    until curl -fs -o /dev/null http://localhost:8080/ || (( SECONDS - start >= 30 )); do sleep 1; done
    curl -s -o /dev/null -w '%{http_code}\n' http://localhost:8080/   # expect 200
    echo "$(( SECONDS - start ))s"                                     # expect < 30s
    ```
  - `docker compose exec app id -u` prints 1654.
  - `docker compose exec app dotnet --list-sdks` prints nothing.
  - No `.tcx` or `.git` in the image (SC-006).
  - `docker compose down`.
  - Fix `Dockerfile` or `.dockerignore` if any check fails.
- [X] T031 [US3] Run the gate `docker compose -f compose.yaml -f compose.test.yaml up --build --abort-on-container-exit --exit-code-from tests` and confirm `echo $?` prints 0. Fix `compose.test.yaml` or `Dockerfile` if it does not.
- [X] T032 [US3] Run the gate negative check (SC-004):
  - Temporarily add `[Fact] public void Fail() => false.Should().BeTrue();` to `tests/JoakimSoftware.TcxTool.Tests/Web/HomePageTests.cs`.
  - Re-run the T031 command and confirm a non-zero exit.
  - Remove the failing test and confirm the gate returns 0 again.
- [X] T033 [US3] Check the port override: `TCXTOOL_HOST_PORT=8081 docker compose up --build -d app`, `curl http://localhost:8081/` returns 200, then `docker compose down` (spec Edge Cases, contracts/quality-gates.md "Run commands").

**Checkpoint**: All three user stories work. All four Quality Gates are green.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Developer documentation and full end-to-end validation.

- [X] T034 Fill in the "Build and test commands" section of `CLAUDE.md` (FR-012, research.md R9), replacing the TODO:
  - the solution layout from plan.md → Source Code
  - the gate and run commands from contracts/quality-gates.md
  - the `TCXTOOL_HOST_PORT` override
  - the note that every command runs from `tcxtool/`
- [X] T035 Run quickstart.md Scenarios 1–3 end to end from a clean state:
  - Delete only the `bin/` and `obj/` directories under `src/` and `tests/`, then time Scenario 1. It must take under 5 minutes, not counting downloads (SC-001).
  - Confirm every expected result in quickstart.md.
- [X] T036 Final constitution sweep across `src/`, `tests/`, `Dockerfile`, and the compose files:
  - file-scoped namespaces and one public type per file (Principle II)
  - no `XDocument.Parse`, `XDocument.Load(path)`, or `XmlDocument.Load` anywhere (Principle IV)
  - no TCX, CSV, upload, or chart code (FR-013)
  - no fourth project (Governance)
  - Record the result in `specs/001-project-bootstrap/checklists/requirements.md` Notes.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies. T003 must finish before T007. T004–T006 must finish before T007. T009 comes after T007 and T008.
- **Foundational (Phase 2)**: depends on Setup and blocks every story. Order: T010 → (T011, T012) → T013 and T014 (fail) → (T015, T016) → T017 (green).
- **US1 (Phase 3)**: depends on Foundational.
- **US2 (Phase 4)**: depends on Foundational and US1, because T022 runs `HomePageTests`.
- **US3 (Phase 5)**: authoring tasks T024–T028 depend only on Foundational. T031 and T032 run the full test suite, so they need US1 and US2.
- **Polish (Phase 6)**: depends on all three stories.

### User Story Dependencies

- **US1 (P1)**: independent after Foundational.
- **US2 (P2)**: its gate run (T022) includes US1's test.
- **US3 (P3)**: container authoring is independent. Its gate run includes all tests, so it needs US1 and US2.

### Within Each Phase

- Tests are written first and seen failing: T013 before T015 and T016 (fails naturally); T014 through temporary breakage; T018 before T019.
- Helpers come before the tests that use them: T011 and T012 before T013 and T014.
- Author files first, then verify: T024–T028 before T029–T033.

## Parallel Opportunities

- **Setup**: T002, T004, T005, T006, and T008 touch different files and can run together after T001. T003 can run alongside them too, since it only creates the `.slnx`.
- **Foundational**: T011 ∥ T012, then T013 ∥ T014, then T015 ∥ T016.
- **US3**: T024, T025, T026, T027, and T028, which are all separate new files.
- **Across stories**: with Foundational done, US1 (T018–T021) and US3 authoring (T024–T028) can proceed at the same time.

## Parallel Example: Foundational

```text
# after T010 (test project set up)
Task: "Create Support/SecureXml.cs"                    # T011
Task: "Create Support/RepositoryRoot.cs"               # T012
# after T011 and T012
Task: "Write Architecture/ProjectSettingsTests.cs"     # T013
Task: "Write Architecture/ParserIndependenceTests.cs"  # T014
# after T013 and T014 are seen failing
Task: "Edit Parser .csproj"                            # T015
Task: "Edit Web .csproj"                               # T016
```

## Parallel Example: User Story 3

```text
Task: "Create .dockerignore per research.md R7"                            # T024
Task: "Create tests/JoakimSoftware.TcxTool.Tests/Fixtures/README.md"        # T025
Task: "Create Dockerfile (build + runtime stages) per research.md R7"       # T026
Task: "Create compose.yaml with service app"                                # T027
Task: "Create compose.test.yaml with service tests"                         # T028
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup (T001–T009)
2. Phase 2: Foundational (T010–T017)
3. Phase 3: US1 (T018–T021)
4. **STOP and VALIDATE**: quickstart.md Scenario 1. The app runs locally and shows the home page.

### Incremental Delivery

1. Setup + Foundational: strict build green, architecture tests in place.
2. US1: a runnable home page (MVP).
3. US2: quality gates 1–3 green, with the warnings-as-errors guard proven.
4. US3: the container image plus Compose gate 4 green.
5. Polish: `CLAUDE.md` commands and the full quickstart run. The feature is complete only when all
   four Quality Gates pass (constitution).

---

## Notes

- [P] tasks touch different files and have no dependencies on incomplete tasks.
- Every "temporarily break" step must be reverted in the same task. Confirm with `git diff` that
  only intended files changed. Remember the git root is `cj-speckit-explore/`.
- Never relax `TreatWarningsAsErrors`, `AnalysisLevel`, or `EnforceCodeStyleInBuild` to get a green
  build. Fix the code instead.
- FluentAssertions is pinned to 7.2.2 for licensing reasons (research.md R4). Don't upgrade to 8.x
  without the maintainer's decision.
- Commit after each phase checkpoint, if you want commits.
