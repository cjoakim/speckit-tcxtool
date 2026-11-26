# Implementation Plan: TCX to CSV Conversion

**Branch**: `002-tcx-to-csv` (feature id; work is currently on git branch `main`) | **Date**: 2026-11-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/002-tcx-to-csv/spec.md`

## Summary

Add a static SSR page at `/convert`. The user uploads one `.tcx` file, and the app writes one
`|`-delimited CSV per activity to `data/csv/`, with the 17 fields from the spec's CSV Output Data
section. It then lists each saved file with its row count.

The work splits across the two existing projects:

- **Parser library** (BCL only):
  - parses TCX through the secure `XmlReader` into immutable records
  - keeps every copied value's original text, so values are written exactly as recorded
  - formats derived values (miles, feet, elapsed time) as shortest round-trip decimals
  - renders every CSV in memory, then writes through temp files and renames, so a failure leaves
    no file behind
- **Web project**:
  - adds the page and a result component
  - adds a conversion service with the 50 MB limit and logging
  - adds an upload-size guard middleware, because Kestrel's own 413 surfaces as an exception
  - adds the options that point at `data/csv/` on the host and at a bind mount in the container

Decisions and measurements are in [research.md](./research.md).

## Technical Context

**Language/Version**: C# (ships with SDK 11.0.100), `net11.0`, pinned by `global.json`.

**Primary Dependencies**:
- Runtime: BCL `System.Xml` and `System.Xml.Linq` (Principle IV), plus ASP.NET Core / Blazor from
  the shared framework. No new runtime packages.
- Test project only: `Microsoft.AspNetCore.Mvc.Testing` 11.0.0 (research R10).

**Storage**: Files only. CSVs go to `TcxTool:CsvOutputDirectory`: `tcxtool/data/csv/` on the host,
and `/data/csv` bind-mounted from `./data/csv` in the container. No database.

**Testing**:
- xUnit 2.9.3, bUnit 2.15.2, FluentAssertions 7.2.2, coverlet 6.0.4 (unchanged)
- `Microsoft.AspNetCore.Mvc.Testing` 11.0.0 for in-process HTTP tests
- `.tcx` fixtures and golden CSVs in `tests/JoakimSoftware.TcxTool.Tests/Fixtures/`
- NSubstitute is still not needed: no boundary needs a fake

**Target Platform**: macOS host (`dotnet run`), and the Linux container from `aspnet:11.0`, running
non-root on port 8080.

**Project Type**: Web application (Blazor Web App), with a parser class library and a test project.

**Performance Goals**: each sample file (13–15 MB, about 19,900 trackpoints) converts in under
10 s (SC-001). Measured parse time is 120–270 ms.

**Constraints**:
- Upload limit 50 MB, configurable, with a file of exactly the limit accepted (FR-002).
- Byte-identical output on repeat runs (SC-006).
- No partial files on failure (FR-015).
- The uploaded TCX is never saved (FR-019).
- Logs carry the file name and error type only (FR-018).
- Tests run offline.

**Scale/Scope**: single local user, one file per upload. Adds 1 page, 1 component, 1 service,
1 middleware, about 21 parser types, 14 fixtures, and about 16 test classes.

No item is marked NEEDS CLARIFICATION. Research R1–R11 settle each unknown and the spec's
outstanding items.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Section | Requirement | Plan | Pre | Post |
|---|---|---|---|---|
| I. .NET 11 Blazor Web App | Static SSR default, interactivity only where needed | `/convert` is a static SSR form; no `@rendermode` anywhere (R1) | ✅ | ✅ |
| II. Style by toolchain | Settings in each project, idioms: records, one type per file, `Async` + `CancellationToken`, XML docs, code-behind | Parsed data are `record`s; `ParseAsync` / `ConvertAsync` take a `CancellationToken`; `ConvertPage.razor.cs` code-behind; `ProjectSettingsTests` unchanged | ✅ | ✅ |
| III. Parser library, UI on top | Parser has no ASP.NET Core reference; typed result crosses the boundary; components don't read XML or catch parse errors | Parser returns `TcxParseResult` / `ConversionResult`; the page only renders `ConversionResult`; `ParserIndependenceTests` stays green | ✅ | ✅ |
| IV. BCL XML only (NON-NEGOTIABLE) | `XmlReader.Create` with Prohibit, null resolver, bounded entities; `XDocument` loads from that reader; TCX v2 namespace; extension namespace tolerated; typed errors; size limit before parsing | Exactly as R3; DTD reported as `ProhibitedContent` via a prolog scan after the reader rejects it (R4); size checked before the stream is opened (R2) | ✅ | ✅ |
| V. Test-first (NON-NEGOTIABLE) | Tests first and failing; required fixtures | Fixtures cover the five the constitution names (run with laps, ride with positions, no trackpoints, extensions, malformed) plus the spec's edge cases. Tasks will order each test before its subject. | ✅ | ✅ |
| VI. Docker multi-stage, non-root | No samples in the image | Dockerfile unchanged; `data/` stays in `.dockerignore`; output goes through a bind mount, not the image (R9) | ✅ | ✅ |
| VII. Compose test gate (NON-NEGOTIABLE) | Tests in the build stage, fixtures mounted, no network | `compose.test.yaml` unchanged (fixtures, including `Expected/`, already mounted); `Mvc.Testing` restores at image build and runs in-process | ✅ | ✅ |
| Technology Constraints | 3 projects; `ILogger<T>`; parse failures log file name and error type, never XML | No new project; `CsvConversionService` logs through `ILogger<CsvConversionService>` with name + `Kind` only | ✅ | ✅ |
| Quality Gates 1–4 | All pass | Validated by [quickstart.md](./quickstart.md) scenarios 1 and 4 | ✅ | ✅ |

Result: **PASS**, with no violations, so Complexity Tracking stays empty.

Two points to note, neither a violation:

- **File I/O in the parser library.** `TcxCsvConverter` writes files from the parser library. It
  uses only the BCL, so Principle III holds. Keeping it there means the atomic-write behavior is
  tested without a web host.
- **`app.UseRouting()` becomes explicit** in `Program.cs`, so `UploadSizeGuard` can run before
  antiforgery reads the form (R2). The 001 home-page and not-found behavior is unchanged.
- **Component tests for activity, lap and trackpoint views** (Principle V) are deferred to the
  display feature, because this feature has no such views. Its component tests cover `/convert`
  and `ConversionResultView`, which render parser and conversion records.

## Project Structure

### Documentation (this feature)

```text
specs/002-tcx-to-csv/
├── plan.md                  # This file
├── research.md              # Phase 0: decisions R1–R11
├── data-model.md            # Phase 1: records, errors, results, lifecycle
├── quickstart.md            # Phase 1: validation scenarios 1–4
├── contracts/
│   ├── csv-format.md        # CSV file contract (fields, header, formatting)
│   ├── parser-api.md        # Parser library public API
│   └── convert-page.md      # /convert UI contract
├── checklists/
│   └── requirements.md      # Spec quality checklist
└── tasks.md                 # Phase 2 output (/speckit-tasks, not created here)
```

### Source Code (`tcxtool/`)

```text
src/JoakimSoftware.TcxTool.Parser/
├── Tcx/            # RecordedNumber, RecordedTime, TcxDocument, TcxActivity, TcxDevice, TcxLap,
│                   # TcxTrackpoint, TcxPosition, TcxParseResult, TcxParser, TcxNamespaces
├── Csv/            # ActivityIdName, CsvFileName, ActivityCsvWriter, DerivedNumber, ElapsedTime, CsvText
└── Conversion/     # TcxErrorKind, TcxError, TcxErrorMessages, SavedCsv, ConversionResult, TcxCsvConverter

src/JoakimSoftware.TcxTool.Web/
├── Program.cs                          # options, Kestrel/form limits, UploadSizeGuard, explicit UseRouting
├── appsettings.json                    # TcxTool: MaxUploadBytes 52428800, CsvOutputDirectory data/csv
├── appsettings.Development.json        # TcxTool: CsvOutputDirectory ../../data/csv
├── Conversion/                         # TcxToolOptions, CsvConversionService, UploadSizeGuard
└── Components/
    ├── Pages/Home.razor                # + link to /convert
    ├── Pages/ConvertPage.razor (+ .razor.cs)   # not `Convert`: that would hide System.Convert
    └── Conversion/ConversionResultView.razor

tests/JoakimSoftware.TcxTool.Tests/
├── JoakimSoftware.TcxTool.Tests.csproj # + Microsoft.AspNetCore.Mvc.Testing 11.0.0
├── Parser/         # TcxParserTests, DerivedNumberTests, ElapsedTimeTests, ActivityIdNameTests,
│                   # CsvFileNameTests, ActivityCsvWriterTests, TcxCsvConverterTests, CsvTextTests,
│                   # ConversionResultTests, ConversionPerformanceTests
├── Web/            # ConversionResultViewTests, ConvertPageTests, ConvertEndpointTests, AppSettingsTests,
│                   # CsvConversionServiceTests (+ HomePageTests)
├── Support/        # + Fixture, TempDirectory, ListLogger
└── Fixtures/
    ├── run-two-laps.tcx            # run with laps + extensions (golden: Expected/run-two-laps.csv)
    ├── ride-positions.tcx          # Biking, positions, no extensions
    ├── no-trackpoints.tcx
    ├── missing-optional.tcx        # no HR / position / ext / altitude / time, no Creator
    ├── fractional-times.tcx        # .6 s times, a trackpoint before the reference, device "Edge|1040"
    ├── multi-activity.tcx          # 2 activities, same date, different start times
    ├── duplicate-start.tcx         # 2 activities, same start second
    ├── malformed.tcx
    ├── not-tcx.tcx                 # well-formed, wrong root
    ├── dtd.tcx                     # DOCTYPE with an external SYSTEM entity
    ├── invalid-number.tcx          # HeartRateBpm "abc"
    ├── invalid-number-nan.tcx      # AltitudeMeters NaN
    ├── invalid-time.tcx            # unparseable trackpoint Time
    ├── missing-id.tcx
    ├── Expected/*.csv              # golden outputs for the success fixtures
    └── README.md                   # updated list

compose.yaml        # app: + TcxTool__CsvOutputDirectory=/data/csv, + ./data/csv:/data/csv bind mount
.gitignore          # + data/csv/
CLAUDE.md           # feature history row, parser types now exist, Mvc.Testing package note
```

**Structure Decision**: the existing three-project layout, unchanged in count:

- `src/JoakimSoftware.TcxTool.Parser` holds the TCX model, CSV formatting, and file conversion.
- `src/JoakimSoftware.TcxTool.Web` holds the page, the service, the guard, and the options.
- `tests/JoakimSoftware.TcxTool.Tests` gains a `Parser/` folder and new `Web/` tests. Following
  `CLAUDE.md`, these are folders, not new projects.

## Complexity Tracking

No constitution violations to justify.
