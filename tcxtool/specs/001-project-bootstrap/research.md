# Research: Project Bootstrap

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Date**: 2026-11-22

Facts below were verified on the development machine on 2026-11-22 by generating throwaway
projects in a scratch directory and querying NuGet:

- .NET SDK 11.0.100 is the only installed SDK.
- Docker 28.0.4 and Compose v2.34.0 are installed. The Docker daemon was **not running** at the
  time of checking.

## R1. Solution format and layout

- **Decision**: `dotnet new sln -n JoakimSoftware.TcxTool` at the `tcxtool/` root. On SDK 11 this
  produces `JoakimSoftware.TcxTool.slnx`. Projects go under `src/` and `tests/`:
  - `src/JoakimSoftware.TcxTool.Parser`
  - `src/JoakimSoftware.TcxTool.Web`
  - `tests/JoakimSoftware.TcxTool.Tests`
- **Rationale**: `.slnx` is the SDK default, and it is plain XML that diffs cleanly. The `src/` and
  `tests/` split matches the three components in the constitution. Project names follow the root
  namespace.
- **Alternatives considered**: `.sln` via `--format sln` was rejected because it is the legacy
  format with no benefit here. A flat layout with no `src/` was rejected because the Dockerfile
  copy steps and test discovery are clearer with the split.

## R2. Web project template options

- **Decision**: `dotnet new blazor -n JoakimSoftware.TcxTool.Web -o src/JoakimSoftware.TcxTool.Web
  --interactivity Server --empty --no-https`. `--all-interactive` stays at its default of `false`.
- **Rationale**:
  - With `--interactivity Server` and `--all-interactive false`, the Interactive Server services
    and endpoints are registered, but pages render with static SSR unless a component opts in.
    That is exactly Principle I. Later features need an interactive path: file upload, and charts
    through JS interop.
  - `--empty` drops the Counter and Weather demo pages and the Bootstrap styling, which covers most
    of FR-004. It keeps `Home`, `Error`, `NotFound`, `MainLayout`, and `ReconnectModal`, which the
    Interactive Server reconnect UI needs.
  - `--no-https`: the container listens on plain HTTP on port 8080 (Principle VI), and local use is
    on localhost. Using one scheme everywhere avoids dev-certificate setup.
- **Alternatives considered**:
  - `--interactivity None` was rejected: feature 2 (upload) would have to re-add the interactive
    wiring.
  - `Auto` and `WebAssembly` were rejected: WebAssembly is out of scope under Principle I.
  - Keeping HTTPS locally was rejected: it adds dev-certificate trust steps and still differs from
    the container.

## R3. Class library and test project templates

- **Decision**:
  - `dotnet new classlib` for the parser. Delete the generated `Class1.cs`.
  - `dotnet new xunit` for the tests. Delete the generated `UnitTest1.cs`.
  - The xunit template on SDK 11.0.100 supplies `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.5,
    `Microsoft.NET.Test.Sdk` 17.14.1, and `coverlet.collector` 6.0.4. Keep these versions.
  - Add `bunit` 2.15.2 and `FluentAssertions` 7.2.2 (see R4).
  - The test project references both the Parser and Web projects.
- **Rationale**: the template defaults already include the xUnit runner and coverlet packages that
  Principle V requires. bUnit 2.x is the current major version and is independent of the test
  framework. Leaving the parser library empty is valid: it compiles to an assembly with no types,
  and the first parser feature fills it.
- **Alternatives considered**:
  - xUnit v3 (`xunit.v3`) was rejected for now. The template default is v2, and the constitution
    names only "xUnit". Moving to v3 is a later one-package change.
  - NSubstitute was not added. Principle V allows it "only where a boundary needs a fake", and the
    bootstrap has no such boundary.

## R4. FluentAssertions version (licensing)

- **Decision**: Pin `FluentAssertions` **7.2.2**, the latest 7.x release.
- **Rationale**: FluentAssertions 8.x (latest 8.11.0) uses the Xceed commercial license, which
  requires a paid license for commercial use. 7.x stays under Apache-2.0. The constitution requires
  FluentAssertions but says nothing about the license, so the license-neutral line is the safe
  default.
- **Alternatives considered**: 8.11.0 is fine if the project stays non-commercial or a license is
  bought. Switching is a version bump in one file. **This is the maintainer's call.**

## R5. Project settings (Principle II) and toolchain enforcement

- **Decision**:
  - Each of the three `.csproj` files states these explicitly: `Nullable=enable`,
    `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, and `AnalysisLevel=latest`.
  - Each also sets `EnforceCodeStyleInBuild=true`, so `.editorconfig` IDE rules break the build.
  - The Parser project also sets `GenerateDocumentationFile=true`, so a missing XML doc comment on
    a public parser API (CS1591) fails the build.
  - `RootNamespace` matches each project name.
  - `.editorconfig` starts from `dotnet new editorconfig` and adds:
    `csharp_style_namespace_declarations = file_scoped:warning`, plus naming rules for `_camelCase`
    private fields and the `Async` suffix.
- **Rationale**: the constitution says the four settings are "mandatory in every project file".
  Putting them in each `.csproj` satisfies that literally, and an automated test checks it (see
  [contracts/quality-gates.md](./contracts/quality-gates.md)). `EnforceCodeStyleInBuild` is what
  makes the build, not just the IDE, enforce `.editorconfig` style.
- **Alternatives considered**: `Directory.Build.props` was rejected. It is cleaner, but the
  settings would no longer be in each project file, and it would conflict with the wording of
  Principle II.

## R6. SDK pinning

- **Decision**: `dotnet new globaljson --sdk-version 11.0.100 --roll-forward latestFeature` at the
  `tcxtool/` root.
- **Rationale**: any 11.0.x SDK at feature band 100 or later is accepted, including whatever patch
  the `sdk:11.0` image carries. A 10.x or 12.x SDK fails with an error that names the required
  version, which covers the spec edge case "Wrong SDK installed".
- **Alternatives considered**: `patch` roll-forward was rejected because it is too strict for the
  container image's SDK patch level. No `global.json` at all was rejected because it would not
  detect the wrong SDK.

## R7. Container and Compose gate design

- **Decision**:
  - **Dockerfile** with two stages, `build` and `runtime`:
    - `build` (`sdk:11.0`): copy `global.json`, the `.slnx`, and every `.csproj`, then run
      `dotnet restore` on the **whole solution** so the test project is also restored for offline
      test runs. Then copy the source and run `dotnet publish` on the Web project with
      `-c Release --no-restore /p:UseAppHost=false`.
    - `runtime` (`aspnet:11.0`): copy the publish output, set `ASPNETCORE_HTTP_PORTS=8080`,
      `EXPOSE 8080`, run as `USER $APP_UID` (the built-in non-root `app` user), and use
      `dotnet JoakimSoftware.TcxTool.Web.dll` as the entrypoint.
  - **`.dockerignore`** excludes `.git`, `**/bin`, `**/obj`, `**/TestResults`, `data/`, `specs/`,
    `docs/`, `.specify/`, `.claude/`, `*.env`, and `**/secrets*`.
  - **`compose.yaml`** defines service `app`, built from target `runtime`. Its port mapping is
    `${TCXTOOL_HOST_PORT:-8080}:8080`, which covers the "port in use" edge case.
  - **`compose.test.yaml`** defines service `tests`:
    - built from target `build`, working directory `/src`
    - command: `dotnet test JoakimSoftware.TcxTool.slnx --no-restore`
    - `network_mode: none`
    - read-only bind mount of `tests/JoakimSoftware.TcxTool.Tests/Fixtures`
    - `DOTNET_NOLOGO=1` and `DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=1`
    - no `depends_on`
- **Rationale**:
  - Restoring the whole solution in the build stage lets the test container run with no network,
    as Principle VII requires.
  - Building both services in one `up --build` covers the "including the Docker image build" part
    of Quality Gate 4.
  - `network_mode: none` makes "does not reach the network" enforced rather than just intended.
  - The fixtures directory is created now, holding only a README, so the mount exists before
    feature 2 adds `.tcx` fixtures.
- **Effect of `--abort-on-container-exit`**: if the `app` container crashes, Compose stops the
  `tests` container and the gate fails. That is the right outcome, because the runtime image is
  broken. The spec edge case was reworded to say this.
- **Alternatives considered**:
  - Moving `app` to a profile so the gate runs only `tests` was rejected: the runtime image would
    then never be built or started by the gate.
  - Running the tests in their own Dockerfile stage at image build time was rejected: Principle VII
    requires a Compose overlay service whose exit code is the gate's.

## R8. Automated architecture and settings tests

- **Decision**: there are three test classes in the bootstrap. Each is written first and fails
  before its subject exists.
  1. `HomePageTests` (bUnit): rendering `Home` produces an `h1` with text `JoakimSoftware.TcxTool`
     and a non-empty description paragraph.
  2. `ParserIndependenceTests`: these combine two checks.
     - **Project-file check**: the Parser `.csproj` uses `Sdk="Microsoft.NET.Sdk"`, has no
       `FrameworkReference`, no `ProjectReference` to Web, and no `Microsoft.AspNetCore*`
       `PackageReference`.
     - **Assembly check**: the compiled Parser assembly references no `Microsoft.AspNetCore.*` or
       `JoakimSoftware.TcxTool.Web` assembly.
  3. `ProjectSettingsTests`: every project listed in the `.slnx` declares the four Principle II
     settings with the required values.
- **Rationale**: an assembly-reference check alone misses a reference that is added but unused. The
  project-file check catches it, which satisfies spec acceptance scenario 2.3. The tests find the
  repo root by walking up from `AppContext.BaseDirectory` to `JoakimSoftware.TcxTool.slnx`, which
  works on the host and in the container at `/src`.
- **XML handling**: the tests read `.csproj` and `.slnx` files through `XmlReader.Create` with the
  Principle IV secure settings, so the project uses one XML pattern everywhere, not a looser one in
  tests.
- **Alternatives considered**: NetArchTest or ArchUnitNET were rejected. They add a third-party
  dependency for two simple checks, and they only see compiled assemblies.

## R9. Developer documentation

- **Decision**: fill in the `CLAUDE.md` "Build and test commands" section with the layout from
  plan.md and the commands from [quickstart.md](./quickstart.md).
- **Rationale**: this is what FR-012 requires. Keeping the commands in one place, with quickstart
  as the validation run, avoids the two drifting apart.

## Open items for implementation

- Start Docker Desktop before running the Compose gate. The daemon was not running when this was
  checked.
- Code generated by the templates may trip `AnalysisLevel=latest` or `EnforceCodeStyleInBuild`
  rules once warnings are errors. Fix it in code; don't relax the settings.
