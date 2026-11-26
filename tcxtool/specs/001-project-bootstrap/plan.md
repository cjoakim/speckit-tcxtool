# Implementation Plan: Project Bootstrap

**Branch**: `001-project-bootstrap` (feature id; work is currently on git branch `main`) | **Date**: 2026-11-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-project-bootstrap/spec.md`

## Summary

Create the empty `JoakimSoftware.TcxTool` solution with `dotnet new`:

- a BCL-only parser class library
- a Blazor Web App (static SSR by default, Interactive Server available per component, demo pages
  removed) with a home page naming the tool
- an xUnit, bUnit, and FluentAssertions test project

Add the toolchain settings and the container setup, so all four constitution Quality Gates pass
before any TCX functionality exists:

- `.editorconfig` and `global.json`
- a multi-stage non-root Dockerfile
- `compose.yaml` and an offline `compose.test.yaml` gate

Three tests written first enforce the home page, parser independence, and the mandatory project
settings. Finish by filling in the `CLAUDE.md` build and test section. Template and option choices
are in [research.md](./research.md).

## Technical Context

**Language/Version**: C# (the version that ships with SDK 11.0.100), target framework `net11.0`. SDK
pinned by `global.json` (11.0.100, `latestFeature`).

**Primary Dependencies**: ASP.NET Core / Blazor Web App (from the shared framework). No third-party
runtime packages.

**Storage**: N/A (no database, no persistence)

**Testing**:
- xUnit 2.9.3 and xunit.runner.visualstudio 3.1.5, with Microsoft.NET.Test.Sdk 17.14.1 (template
  defaults)
- bUnit 2.15.2
- FluentAssertions 7.2.2, Apache-2.0 (see research R4)
- coverlet.collector 6.0.4
- NSubstitute deferred until a boundary needs a fake

**Target Platform**: macOS developer host (`dotnet run`). Linux container from `aspnet:11.0`
(Docker, later Azure Container Instances).

**Project Type**: Web application (Blazor Web App), plus a class library and a test project

**Performance Goals**: from a clean checkout to the running home page in under 5 minutes (SC-001).
Container serving within 30 s of start (SC-005).

**Constraints**:
- Warnings are errors.
- The format check must be clean.
- Tests run with no network (`network_mode: none`).
- The runtime image runs non-root on port 8080 and contains no SDK, `data/`, or `.git`.

**Scale/Scope**: 3 projects, 1 page, 3 test classes. No TCX, CSV, upload, or chart functionality
(FR-013).

No item is marked NEEDS CLARIFICATION. Every unknown was resolved in research.md R1–R9 by checking
the installed SDK, the template output, and NuGet.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Section | Requirement | Plan | Pre | Post |
|---|---|---|---|---|
| I. .NET 11 Blazor Web App | `net11.0`, static SSR default, interactivity per component, no WASM | `blazor --interactivity Server`, `--all-interactive` false, no WASM project (R2) | ✅ | ✅ |
| II. Style by toolchain | 4 settings in every project file, `.editorconfig`, idioms, format check | Settings in each `.csproj` plus `EnforceCodeStyleInBuild`, enforced by `ProjectSettingsTests`; `.editorconfig` with file-scoped namespace and naming rules (R5) | ✅ | ✅ |
| III. Parser library, UI on top | Parser has no ASP.NET Core or Blazor reference; Web references Parser | Web → Parser reference now; `ParserIndependenceTests` checks the project file and the assembly (R8) | ✅ | ✅ |
| IV. BCL XML only (NON-NEGOTIABLE) | Secure `XmlReader` for all parsing | No TCX parsing yet. Test helpers that read `.csproj` and `.slnx` use the secure `XmlReader` settings (R8). | ✅ | ✅ |
| V. Test-first (NON-NEGOTIABLE) | Tests first and failing; xUnit, bUnit, FluentAssertions, coverlet | Tasks order each of the 3 test classes before its subject; packages per Testing above (R3, R4) | ✅ | ✅ |
| VI. Docker multi-stage, non-root | `sdk:11.0` build, `aspnet:11.0` runtime, `UseAppHost=false`, non-root, 8080, `.dockerignore` | Exactly as specified, plus `data/` excluded (R7) | ✅ | ✅ |
| VII. Compose test gate (NON-NEGOTIABLE) | Overlay runs tests in the build stage, exit code from tests, fixtures mounted, no network, no app health dependency | `compose.test.yaml` `tests` service: target `build`, `network_mode: none`, read-only fixtures mount, no `depends_on` (R7) | ✅ | ✅ |
| Technology Constraints | 3 projects, no DB, no MediatR, no Garmin client, `ILogger<T>` | 3 projects only, nothing else added | ✅ | ✅ |
| Quality Gates 1–4 | All pass | Listed in [contracts/quality-gates.md](./contracts/quality-gates.md) and validated by [quickstart.md](./quickstart.md) | ✅ | ✅ |
| Development Environment | macOS, zsh, no PowerShell | All commands are zsh/POSIX | ✅ | ✅ |

Result: **PASS**, with no violations, so Complexity Tracking stays empty. Two settings go beyond
the constitution: `EnforceCodeStyleInBuild` everywhere and `GenerateDocumentationFile` on Parser.
Both make existing rules stricter (Principle II's enforcement and its XML-doc idiom). They add no
new components.

One spec change during planning: the edge case "Application container fails to start" was reworded
to match how `--abort-on-container-exit` behaves (research R7).

## Project Structure

### Documentation (this feature)

```text
specs/001-project-bootstrap/
├── plan.md                    # This file
├── research.md                # Phase 0: decisions R1–R9
├── data-model.md              # Phase 1: structural model (no domain entities yet)
├── quickstart.md              # Phase 1: validation scenarios 1–3
├── contracts/
│   ├── home-page.md           # UI contract for /
│   └── quality-gates.md       # Commands, compose services, automated checks
├── checklists/
│   └── requirements.md        # Spec quality checklist
└── tasks.md                   # Phase 2 output (/speckit-tasks, not created here)
```

### Source Code (`tcxtool/`, the Spec Kit project root)

```text
tcxtool/
├── JoakimSoftware.TcxTool.slnx
├── global.json                          # SDK 11.0.100, rollForward latestFeature
├── .gitignore                           # dotnet new gitignore (+ .DS_Store)
├── .editorconfig
├── .dockerignore
├── Dockerfile                           # stages: build (sdk:11.0), runtime (aspnet:11.0)
├── compose.yaml                         # service: app
├── compose.test.yaml                    # service: tests
├── CLAUDE.md                            # "Build and test commands" filled in (FR-012)
├── src/
│   ├── JoakimSoftware.TcxTool.Parser/
│   │   └── JoakimSoftware.TcxTool.Parser.csproj     # no source files yet
│   └── JoakimSoftware.TcxTool.Web/
│       ├── JoakimSoftware.TcxTool.Web.csproj        # references Parser
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Properties/launchSettings.json
│       ├── wwwroot/app.css
│       └── Components/
│           ├── App.razor
│           ├── Routes.razor
│           ├── _Imports.razor
│           ├── Layout/          # MainLayout, ReconnectModal (+ .css/.js)
│           └── Pages/           # Home.razor, Error.razor, NotFound.razor
├── tests/
│   └── JoakimSoftware.TcxTool.Tests/
│       ├── JoakimSoftware.TcxTool.Tests.csproj      # references Parser, Web
│       ├── Fixtures/README.md                       # mount point; .tcx fixtures arrive in feature 2
│       ├── Support/
│       │   ├── RepositoryRoot.cs                    # locate the .slnx
│       │   └── SecureXml.cs                         # Principle IV secure XmlReader → XDocument
│       ├── Web/HomePageTests.cs
│       └── Architecture/
│           ├── ParserIndependenceTests.cs
│           └── ProjectSettingsTests.cs
├── data/                                # existing sample exports (git-tracked, docker-ignored)
├── specs/  .specify/  .claude/          # Spec Kit (docker-ignored)
```

**Structure Decision**: one `.slnx` at the `tcxtool/` root, with the two production projects under
`src/` and the single test project under `tests/`. These are exactly the three components that
Technology Constraints allows. Test classes are grouped by folder (`Web/`, `Architecture/`) inside
the one test project rather than split into separate projects, which keeps the project count at
three.

## Implementation Order (input to `/speckit-tasks`)

1. **Setup**: create `global.json`, `.gitignore`, the solution, and the three projects from templates
   (R1–R3). Delete `Class1.cs` and `UnitTest1.cs`. Add `.editorconfig` (R5). Confirm the untouched
   baseline builds.
2. **Foundational, test-first**:
   - Set up the test project (packages, references, its own settings) and the `SecureXml` and
     `RepositoryRoot` helpers.
   - Write `ProjectSettingsTests` and see them fail on the template Parser and Web projects.
   - Write `ParserIndependenceTests` and see them fail with a temporary `FrameworkReference`.
   - Then add the Principle II settings and the Web → Parser reference until the strict build and
     both tests are green.
3. **US1**: write `HomePageTests`, which fail against "Hello, world!", then update `Home.razor` to
   the contract.
4. **US2**: run quality gates 1–3, then the warnings-as-errors negative check.
5. **US3**: add `.dockerignore`, `Dockerfile`, `compose.yaml`, `compose.test.yaml`, and
   `Fixtures/README.md`. Run the timed container checks and the gate, including the failing-test
   negative check.
6. **Docs**: fill in the `CLAUDE.md` "Build and test commands" section. Run all quickstart scenarios.

## Complexity Tracking

No violations. Table intentionally empty.
