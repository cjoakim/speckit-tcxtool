# JoakimSoftware.TcxTool Constitution

This application is a .NET 11 Blazor Web App that parses Garmin Training Center XML
(TCX) files, parses them to CSV, performs analysis of a CSV, and also
displays rich graphics relating to one Garmin activity (i.e. - a run/marathon).

The name of the application is `JoakimSoftware.TcxTool`, which is also the root namespace.

## Core Principles

### I. .NET 11 Blazor Web App

The solution targets the .NET 11 SDK (`net11.0`) and the C# language version that
ships with that SDK. The UI is a Blazor Web App. Static server-side rendering is
the default. `InteractiveServer` or `InteractiveAuto` is enabled per component
only when that component needs it. WebAssembly is out of scope.

Rationale: one hosting model keeps render-mode boundaries explicit and avoids a
second client stack.

### II. Coding Style Is Enforced by the Toolchain

Style is defined in `.editorconfig` and enforced by the SDK analyzers plus
`dotnet format`. The build treats warnings as errors. These settings are
mandatory in every project file:

- `<Nullable>enable</Nullable>`
- `<ImplicitUsings>enable</ImplicitUsings>`
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- `<AnalysisLevel>latest</AnalysisLevel>`

Required idioms:

- File-scoped namespaces. One public type per file.
- `JoakimSoftware.TcxTool` is the root namespace.
- PascalCase for types, methods, and properties. `camelCase` for locals and
  parameters. `_camelCase` for private fields.
- `async` methods end in `Async` and accept a `CancellationToken`.
- Immutable `record` types for parsed TCX data.
- Public parser APIs carry XML doc comments.
- Razor files bind and compose. Parsing, validation, and mapping live in C#
  types, with a `.razor.cs` code-behind when a component is more than markup.
- Components stay small, do one job, and take strongly typed `[Parameter]`
  properties.

`dotnet format --verify-no-changes` MUST pass before a change is done.

### III. Parser Library, UI on Top

TCX parsing is a class library with no reference to ASP.NET Core or Blazor.
The Blazor project references that library and only displays its results.
Components do not open files, read XML, or catch parse failures inline. A
typed parse result (success value or typed error) crosses the boundary.

Rationale: the parser must be testable with `dotnet test` and no web host.

### IV. Built-in XML Parsing Only (NON-NEGOTIABLE)

Parsing uses the BCL only: `System.Xml`, `System.Xml.Linq`, and
`System.Xml.Serialization`. Third-party XML libraries are prohibited.

Every parse starts with `XmlReader.Create` and these settings:

- `DtdProcessing = DtdProcessing.Prohibit`
- `XmlResolver = null`
- A bounded `MaxCharactersFromEntities`

`XDocument` or `XmlSerializer` loads from that reader. `XDocument.Parse` and
`XmlDocument.Load` on a raw string or path are prohibited, because they skip
the secure reader.

The reader recognizes the Training Center Database v2 namespace
(`http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2`) and tolerates
the Activity Extension v2 namespace. Optional elements (position, heart rate,
cadence, altitude, extensions) stay optional. An unexpected root element or
malformed XML returns a typed parse error. Uploads are rejected past a
configured size limit before parsing begins.

### V. Test-First with xUnit, bUnit, and FluentAssertions (NON-NEGOTIABLE)

Tests are written first and MUST fail before implementation. The runner is
`dotnet test`. Required packages:

- xUnit and `xunit.runner.visualstudio` for parser and service tests
- FluentAssertions for assertions
- bUnit for Blazor component tests
- coverlet.collector for coverage
- NSubstitute only where a boundary needs a fake

Parser tests use checked-in `.tcx` fixtures, including a run with laps, a ride
with positions, an activity with no trackpoints, a file with extensions, and
a malformed file. Component tests render activity, lap, and trackpoint views
from parser records. Tests do not call Garmin, the network, or a live browser.

### VI. Docker Builds Are Multi-Stage and Non-Root

The app ships from a multi-stage Dockerfile:

- Build stage: `mcr.microsoft.com/dotnet/sdk:11.0`. Restore, then publish
  Release with `/p:UseAppHost=false`.
- Runtime stage: `mcr.microsoft.com/dotnet/aspnet:11.0`. Copy the publish
  output, run as a non-root user, listen on port 8080
  (`ASPNETCORE_HTTP_PORTS=8080`).

A `.dockerignore` excludes `.git`, `bin`, `obj`, test results, and local
secrets. The image contains no SDK and no sample uploads.

### VII. Compose Is the Test Gate (NON-NEGOTIABLE)

`compose.yaml` builds and runs the app from that Dockerfile. A Compose overlay
runs the test suite inside the SDK build stage and exits with the test
process code. The gate command is:

```bash
docker compose -f compose.yaml -f compose.test.yaml up \
  --build --abort-on-container-exit --exit-code-from tests
```

Fixtures are mounted into the test service. The test run does not depend on
the app container being healthy, and it does not reach the network. A green
`dotnet test` on the host does not replace this gate.

## Technology Constraints

- Solution layout: a parser class library, a Blazor Web App, and a test
  project. No database, no MediatR, and no external Garmin API client in the
  initial scope.
- Input is a user-supplied `.tcx` file. Display covers activity summary
  (sport, start time, duration, distance), laps, and trackpoints (time,
  position, altitude, distance, heart rate, cadence when present).
- Secrets, connection strings, and server-only services stay on the server.
  Nothing in the parser or the UI reads environment secrets to parse a file.
- Logging uses `ILogger<T>`. Parse failures log the file name and error type,
  never the raw XML body.

## Quality Gates

A change is complete only when all of these pass:

1. `dotnet build` with warnings as errors.
2. `dotnet format --verify-no-changes`.
3. `dotnet test` (xUnit, bUnit, FluentAssertions, coverlet).
4. The Compose gate in Principle VII, including the Docker image build.

## Development Environment 

1. Development environment is macOS, current version.
2. zsh is the shell, PowerShell and Windows is out-of-scope.
3. Claude Code is the AI assistant.
4. JetBrains Rider, Cursor, and Visual Studio Code are the IDEs, with Cursor preferred.
5. Docker builds and Docker Compose will be used for testing prior to Azure deployment.

## Deployment 

1. The application is intended to be primarily exexuted locally on a developer's machine.
2. It may also be deployed to Azure as a Docker Container in Azure Container Instances. 

## Governance

This constitution overrides local habit and template defaults. Amendments
change this file first, state what moved, and keep the version line current.

- MAJOR: a principle removed or redefined so existing practice breaks.
- MINOR: a new principle or a material expansion.
- PATCH: wording only.

Reviews check each gate in the Quality Gates section. Complexity beyond the
parser library, the Blazor app, and the test project needs a reason recorded
in the spec that introduced it.

**Version**: 0.1.0 | **Ratified**: TODO | **Last Amended**: 2026-11-22
