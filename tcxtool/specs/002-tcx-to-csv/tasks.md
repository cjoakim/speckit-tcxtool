---

description: "Task list for feature 002-tcx-to-csv"
---

# Tasks: TCX to CSV Conversion

**Input**: Design documents from `specs/002-tcx-to-csv/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included. The constitution requires test-first (Principle V, NON-NEGOTIABLE).

- Every test task comes before the code it covers, and must be run and seen failing first.
- A test that fails to compile because its subject type does not exist yet counts as failing.
  Add the type in the next task.
- If a test passes before the code it covers is written, show it failing with a temporary break
  in that code. Revert the break in the same task (see `CLAUDE.md`, Lessons from feature 001).

**Organization**: Tasks are grouped by user story. US2 and US3 extend the parser and page that
US1 creates, so they follow US1. Each still has its own tests and checkpoint.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task belongs to (US1, US2, US3)
- All paths are relative to `tcxtool/`. Run every command from `tcxtool/`. The git root is the
  parent directory.

## Path Conventions

These come from plan.md → Project Structure:

- Parser: `src/JoakimSoftware.TcxTool.Parser/{Tcx,Csv,Conversion}/`
- Web: `src/JoakimSoftware.TcxTool.Web/{Conversion,Components/Pages,Components/Conversion}/`
- Tests: `tests/JoakimSoftware.TcxTool.Tests/{Parser,Web,Support,Fixtures}/`
- Fixtures: `tests/JoakimSoftware.TcxTool.Tests/Fixtures/` (`.tcx`) and `Fixtures/Expected/` (golden `.csv`)

**Conventions for every task**:

- **Code rules**:
  - one public type per file, file-scoped namespaces
  - XML doc comments on every public parser member (the build fails otherwise)
  - `Async` methods take a `CancellationToken`
  - no relaxed settings or suppressed rules
- **Reading XML in tests**: always through `Support/SecureXml`. The parser itself is the only
  production XML reader.
- **Golden CSVs**:
  - write them by hand from the fixture, not by running the code under test
  - compute derived values independently, for example
    `python3 -c "print(repr(103.5999984741211/0.3048))"`
  - expand any exponent form into plain digits, per `contracts/csv-format.md`
  - end every line with LF, and save as UTF-8 without BOM
- **Real samples**: never copy `data/tcx/` content into fixtures. Fixtures are small and synthetic,
  with made-up coordinates.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Test package, git-ignore, and test helpers.

- [X] T001 Add `Microsoft.AspNetCore.Mvc.Testing` 11.0.0 to `tests/JoakimSoftware.TcxTool.Tests/JoakimSoftware.TcxTool.Tests.csproj` with `dotnet add tests/JoakimSoftware.TcxTool.Tests package Microsoft.AspNetCore.Mvc.Testing --version 11.0.0` (research.md R10). Confirm `dotnet build JoakimSoftware.TcxTool.slnx` still passes.
- [X] T002 [P] Add `data/csv/` to `.gitignore` with a comment that CSV output contains GPS positions (research.md R9). Confirm `git check-ignore -v data/csv/x.csv` matches.
- [X] T003 [P] Create `tests/JoakimSoftware.TcxTool.Tests/Support/Fixture.cs`, a static class built on `RepositoryRoot.Combine`:
  - `Path(name)` returns `Fixtures/<name>`
  - `ExpectedPath(name)` returns `Fixtures/Expected/<name>`
  - `OpenRead(name)` opens a fixture
  - `ReadExpected(name)` returns the golden file's text, read as UTF-8
- [X] T004 [P] Create `tests/JoakimSoftware.TcxTool.Tests/Support/TempDirectory.cs`, an `IDisposable` that creates a unique directory under `Path.GetTempPath()`, exposes `FullPath`, and deletes the directory recursively on dispose.
- [X] T005 [P] Create `tests/JoakimSoftware.TcxTool.Tests/Support/ListLogger.cs`, an `ILogger<T>` fake that records each entry's level and formatted message, for the FR-018 checks. The constitution allows NSubstitute only where needed; a plain fake avoids the package.
- [X] T006 Run `dotnet test JoakimSoftware.TcxTool.slnx` and confirm the existing 001 tests still pass with the new helpers.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The formatting primitives and the error and result types that every story uses.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T007 [P] Write failing `tests/JoakimSoftware.TcxTool.Tests/Parser/DerivedNumberTests.cs` for `DerivedNumber.Format` (research.md R5, contracts/csv-format.md "Shortest decimal"):
  - `0` gives `0`, and `-0.0` gives `0`
  - `1.5199999809265137 / 1609.344` gives `0.0009444842003490327`
  - `0.01 / 1609.344` gives `0.000006213711922373339`, with no `E`
  - `42481.4296875 / 1609.344` gives `26.39673661286835`
  - `-2 / 0.3048` gives `-6.561679790026246`
  - a value ≥ 1e15 has no exponent
  - for a theory over sample values, `double.Parse(Format(v), InvariantCulture) == v`
  - output is the same under the `de-DE` culture
- [X] T008 Implement `src/JoakimSoftware.TcxTool.Parser/Csv/DerivedNumber.cs` so T007 passes: format with `"R"` and the invariant culture, expand any exponent by string manipulation, write `-0` as `0`, never append `.0` (research.md R5).
- [X] T009 [P] Write failing `tests/JoakimSoftware.TcxTool.Tests/Parser/ElapsedTimeTests.cs` (research.md R6). Format a `TimeSpan` as `(ElapsedSeconds, ElapsedTime)`:
  - 0 s gives `0` and `00:00:00`
  - 3725 s gives `3725` and `01:02:05`
  - 3725.6 s gives `3725.6` and `01:02:05`
  - 360000 s gives `360000` and `100:00:00`
  - -3.5 s gives `-3.5` and `-00:00:03`
- [X] T010 Implement `src/JoakimSoftware.TcxTool.Parser/Csv/ElapsedTime.cs` so T009 passes: seconds = `Ticks / 1e7` through `DerivedNumber.Format`; whole seconds truncated toward zero; hours at least 2 digits and unbounded; a leading `-` when negative.
- [X] T011 [P] Write failing `tests/JoakimSoftware.TcxTool.Tests/Parser/CsvTextTests.cs` for `CsvText.Clean` (spec FR-011b):
  - `Edge|1040` gives `Edge 1040`
  - `a\r\nb` and `a\nb` give `ab`
  - `"quoted"` is unchanged
  - `null` gives an empty string
- [X] T012 Implement `src/JoakimSoftware.TcxTool.Parser/Csv/CsvText.cs` so T011 passes.
- [X] T013 [P] Write failing `tests/JoakimSoftware.TcxTool.Tests/Parser/ConversionResultTests.cs`:
  - `ConversionResult.Success(files)` has `IsSuccess` true, `Error` null, and `Files` equal to the input
  - `ConversionResult.Failure(error)` has `IsSuccess` false and empty `Files`
  - `TcxError` keeps `Kind`, `Message`, `Element`, and `TrackpointNumber`
  - each `TcxErrorKind` produces a non-empty message from `TcxErrorMessages` that differs from every other kind's (SC-005)
  - the `InvalidValue` message includes the element name and the trackpoint number
- [X] T014 Create `src/JoakimSoftware.TcxTool.Parser/Conversion/TcxErrorKind.cs`, `TcxError.cs`, `SavedCsv.cs`, `ConversionResult.cs`, and `TcxErrorMessages.cs` (one fixed message per kind; `TooLarge` takes the limit in MB, `InvalidValue` the element and trackpoint number) with XML docs, so T013 passes. Fields are as defined in data-model.md: `TcxErrorKind` (all eleven values), `TcxError.Kind`, `TcxError.Message`, `TcxError.Element`, `TcxError.TrackpointNumber`, `SavedCsv.FileName`, `SavedCsv.RowCount`, `ConversionResult.Files`, `ConversionResult.Error`, and `ConversionResult.IsSuccess` (true exactly when `Error` is null; enforced by `Success` and `Failure`).

**Checkpoint**: Formatting primitives and result types are ready. `dotnet test` is green.

---

## Phase 3: User Story 1 - Convert one TCX file to a CSV (Priority: P1) 🎯 MVP

**Goal**: Upload a complete TCX file on `/convert`. The CSV in `data/csv/` follows
`contracts/csv-format.md`, and the page lists each file with its row count.

**Independent Test**: Upload `run-two-laps.tcx` through `ConvertEndpointTests`. The temp output
directory then holds `2026-11-01_run-two-laps.csv`, byte-identical to its golden file, and the
page lists it with `10 rows`.

### Fixtures for User Story 1

- [X] T015 [P] [US1] Create `tests/JoakimSoftware.TcxTool.Tests/Fixtures/run-two-laps.tcx` with:
  - TCX v2 and Activity Extension v2 namespaces
  - one `Running` activity with `Id` `2026-11-01T12:04:07.000Z`
  - 2 laps and 10 trackpoints, each with time, position, altitude, distance, heart rate, and extension `TPX` (`Speed`, `RunCadence`, `Watts`)
  - one trackpoint 3725 s after the first
  - `Creator` with `Name` and `UnitId` placed after the laps
  - one unknown element in an unknown namespace

  Create the golden file `Fixtures/Expected/run-two-laps.csv` by hand, with the header and 10 rows, ActivityId `run-two-laps`.
- [X] T016 [P] [US1] Create `Fixtures/ride-positions.tcx`, a `Biking` activity with positions on every trackpoint and no `Extensions`. Create its golden file `Fixtures/Expected/ride-positions.csv` by hand.
- [X] T017 [P] [US1] Create `Fixtures/multi-activity.tcx`, with two activities on `2026-11-01` whose `Id`s are `…T07:15:00.000Z` and `…T18:30:05.000Z`, 3 trackpoints each. Create golden files `Fixtures/Expected/2026-11-01_071500_multi-activity.csv` and `Fixtures/Expected/2026-11-01_183005_multi-activity.csv` by hand. TrackpointNum and ElapsedSeconds restart in each.
- [X] T018 [P] [US1] Create `Fixtures/fractional-times.tcx`, an activity with:
  - `Creator/Name` `Edge|1040`
  - trackpoint times with `.600` and `.250` fractions
  - one trackpoint timed before the first timed trackpoint (negative elapsed, research.md R6)
  - an `Id` with offset `-05:00`, so Date uses the Id's own offset

  Create its golden file `Fixtures/Expected/fractional-times.csv` by hand.
- [X] T019 [P] [US1] Update `tests/JoakimSoftware.TcxTool.Tests/Fixtures/README.md`: list each fixture with its purpose, describe `Expected/` and how golden files are written by hand, and keep the rule against copying `data/tcx/`.

### Tests for User Story 1 ⚠️ write first, see them fail

- [X] T020 [P] [US1] Write failing `tests/JoakimSoftware.TcxTool.Tests/Parser/TcxParserTests.cs` (success cases) for `TcxParser.ParseAsync` (contracts/parser-api.md):
  - `run-two-laps.tcx` gives 1 activity, Sport `Running`, 2 laps, and 10 trackpoints in document order
  - `RecordedNumber.Text` keeps file text exactly (for example `0.0` stays `0.0`), and `RecordedNumber.Value` is parsed
  - `TcxActivity.Creator` is found after the laps
  - `TcxTrackpoint.RunCadence`, `Speed`, and `Watts` are read from the extension namespace
  - unknown elements are ignored
  - `multi-activity.tcx` gives 2 activities in order
  - `ride-positions.tcx` gives `TcxPosition` values

  Field names are as in data-model.md (`TcxActivity`, `TcxLap`, `TcxTrackpoint`, `RecordedNumber`, `RecordedTime`).
- [X] T021 [P] [US1] Write failing `tests/JoakimSoftware.TcxTool.Tests/Parser/ActivityIdNameTests.cs` for `ActivityIdName.TryFromFileName`, following data-model.md (CSV row → `ActivityId`):
  - `24932322230.tcx` gives `24932322230`
  - `Morning Run (2).tcx` gives `Morning_Run__2_`
  - `run.TCX` gives `run`
  - `/a/b/run.tcx` and `C:\a\b\run.tcx` give `run`
  - `.tcx` gives `EmptyFileName`
  - `notes.txt` gives `NotTcxFileName`
  - non-ASCII letters are replaced with `_`
- [X] T022 [P] [US1] Write failing `tests/JoakimSoftware.TcxTool.Tests/Parser/CsvFileNameTests.cs` for `CsvFileName.TryFor` (contracts/parser-api.md, data-model.md "Output file names"):
  - one activity gives `2026-11-01_run-two-laps.csv`
  - two activities give `2026-11-01_071500_multi-activity.csv` and `2026-11-01_183005_multi-activity.csv`
  - an Id with offset `-05:00` keeps its own date and time
- [X] T023 [P] [US1] Write failing `tests/JoakimSoftware.TcxTool.Tests/Parser/ActivityCsvWriterTests.cs`:
  - `ActivityCsvWriter.Header` equals the exact header in contracts/csv-format.md
  - writing each parsed success fixture (`run-two-laps`, `ride-positions`, both `multi-activity` activities, `fractional-times`) to a `StringWriter` equals its golden file exactly
  - every line has 17 fields
  - lines end with `\n` only
  - the return value equals the trackpoint count
  - output is the same under `de-DE`
- [X] T024 [P] [US1] Write failing `tests/JoakimSoftware.TcxTool.Tests/Parser/TcxCsvConverterTests.cs` (success cases) for `TcxCsvConverter.ConvertAsync` into a `TempDirectory`:
  - `run-two-laps.tcx` writes `2026-11-01_run-two-laps.csv`, byte-equal to the golden file, UTF-8 without BOM, and returns `SavedCsv(…, 10)`
  - `multi-activity.tcx` writes both files
  - converting twice gives byte-identical files and replaces the first (SC-006)
  - a missing output directory is created
  - no `*.tmp` file remains afterwards
- [X] T025 [P] [US1] Write failing `tests/JoakimSoftware.TcxTool.Tests/Web/AppSettingsTests.cs`. It reads `src/JoakimSoftware.TcxTool.Web/appsettings.json` and `appsettings.Development.json` with `System.Text.Json` through `RepositoryRoot`, and asserts the values of `TcxToolOptions.MaxUploadBytes` and `TcxToolOptions.CsvOutputDirectory` (data-model.md "Web types"):
  - `TcxTool:MaxUploadBytes` is `52428800`
  - `TcxTool:CsvOutputDirectory` is `data/csv`
  - Development overrides `CsvOutputDirectory` to `../../data/csv`

  This configuration test comes before the configuration (`CLAUDE.md` lesson).
- [X] T026 [P] [US1] Write failing `tests/JoakimSoftware.TcxTool.Tests/Web/ConversionResultViewTests.cs` (bUnit, derives from `BunitContext`, follows contracts/convert-page.md):
  - a null result renders nothing
  - a success renders `section[role=status]` with one `li` per `SavedCsv`, reading `<FileName>: <RowCount> rows`
- [X] T027 [P] [US1] Write failing `tests/JoakimSoftware.TcxTool.Tests/Web/ConvertPageTests.cs` (bUnit, follows contracts/convert-page.md "Markup"):
  - exactly one `h1`, `Convert TCX to CSV`
  - form `method=post` and `enctype=multipart/form-data`
  - `input[type=file][name=TcxFile][accept=".tcx"]#tcx-file` with `label[for=tcx-file]`
  - the text `Maximum file size: 50 MB`
  - a submit button `Convert`

  Register `CsvConversionService` and `IOptions<TcxToolOptions>` in `Services`. If bUnit cannot render a page that has `[SupplyParameterFromForm]`, move these markup checks into T029's GET test instead, and record that in research.md R10.
- [X] T028 [P] [US1] Add a failing test to `tests/JoakimSoftware.TcxTool.Tests/Web/HomePageTests.cs`: the home page has an `a[href=convert]` or `a[href=/convert]` link, and its `h1 + p` contract is unchanged.
- [X] T029 [US1] Write failing `tests/JoakimSoftware.TcxTool.Tests/Web/ConvertEndpointTests.cs` (success cases) using `WebApplicationFactory<Program>` with `WithWebHostBuilder` to override `TcxTool:CsvOutputDirectory` to a `TempDirectory` (research.md R10):
  - `GET /convert` returns 200 and contains the form and an antiforgery token
  - `POST /convert` with the token, `_handler=tcx-upload`, and `run-two-laps.tcx` as `TcxFile` returns 200 and contains `2026-11-01_run-two-laps.csv: 10 rows`, and the file in the temp directory equals the golden file
  - after the POST, the output directory contains only the expected `.csv` file, with no `.tcx` or `.tmp` files (FR-019)

### Implementation for User Story 1

- [X] T030 [P] [US1] Create the value records `src/JoakimSoftware.TcxTool.Parser/Tcx/RecordedNumber.cs` and `RecordedTime.cs` with XML docs. `RecordedNumber.Text`, `RecordedNumber.Value`, `RecordedTime.Text`, and `RecordedTime.Value` are as defined in data-model.md "Value types".
- [X] T031 [P] [US1] Create the model records `src/JoakimSoftware.TcxTool.Parser/Tcx/TcxPosition.cs`, `TcxTrackpoint.cs`, `TcxLap.cs`, `TcxDevice.cs`, `TcxActivity.cs`, `TcxDocument.cs`, and `TcxParseResult.cs` (with `Success` / `Failure`), each with XML docs. Fields and rules are as defined in data-model.md:
  - `TcxDocument.Activities` (document order)
  - `TcxActivity.Id` (required), `TcxActivity.Sport`, `TcxActivity.Creator`, `TcxActivity.Laps`
  - `TcxDevice.Name`, `TcxDevice.UnitId` (both optional)
  - `TcxLap.StartTime`, `TcxLap.TotalTimeSeconds`, `TcxLap.DistanceMeters`, `TcxLap.MaximumSpeed`, `TcxLap.Calories`, `TcxLap.AverageHeartRateBpm`, `TcxLap.MaximumHeartRateBpm`, `TcxLap.Intensity`, `TcxLap.TriggerMethod` (all optional), `TcxLap.Trackpoints`
  - `TcxTrackpoint.Time`, `TcxTrackpoint.Position`, `TcxTrackpoint.AltitudeMeters`, `TcxTrackpoint.DistanceMeters`, `TcxTrackpoint.HeartRateBpm`, `TcxTrackpoint.RunCadence`, `TcxTrackpoint.Speed`, `TcxTrackpoint.Watts` (all optional)
  - `TcxPosition.LatitudeDegrees`, `TcxPosition.LongitudeDegrees` (both required)
  - `TcxParseResult.Document`, `TcxParseResult.Error`, `TcxParseResult.IsSuccess` (exactly one of `Document` or `Error` is set; enforced by `Success` and `Failure`)
- [X] T032 [P] [US1] Create `src/JoakimSoftware.TcxTool.Parser/Tcx/TcxNamespaces.cs` with the TCX v2 and Activity Extension v2 `XNamespace` constants.
- [X] T033 [US1] Implement `src/JoakimSoftware.TcxTool.Parser/Tcx/TcxParser.cs` (`ParseAsync`, success path), so T020 passes:
  - one `XmlReader.Create` with `DtdProcessing.Prohibit`, `XmlResolver = null`, `MaxCharactersFromEntities = 1024`, and `Async = true`, then `XDocument.LoadAsync` from that reader (research.md R3, Principle IV)
  - map `Activities/Activity` → `Lap` → `Track/Trackpoint` into the records
  - parse `RecordedNumber.Value` with `NumberStyles.Float` and the invariant culture, and `RecordedTime.Value` with `DateTimeStyles.RoundtripKind`
  - read extension values from the `TPX` element
- [X] T034 [P] [US1] Implement `src/JoakimSoftware.TcxTool.Parser/Csv/ActivityIdName.cs` so T021 passes, following data-model.md (CSV row → `ActivityId`).
- [X] T035 [US1] Implement `src/JoakimSoftware.TcxTool.Parser/Csv/CsvFileName.cs` (`TryFor`, signature in contracts/parser-api.md) so T022 passes. It takes Date and `HHmmss` from `TcxActivity.Id` `RecordedTime.Value` in its own offset (data-model.md, research.md R6).
- [X] T036 [US1] Implement `src/JoakimSoftware.TcxTool.Parser/Csv/ActivityCsvWriter.cs` so T023 passes:
  - `Header` constant, and the 17 fields in contract order
  - copied fields write `RecordedNumber.Text` and `RecordedTime.Text`
  - derived fields go through `DerivedNumber` and `ElapsedTime`
  - every field goes through `CsvText.Clean`
  - `\n` line endings
  - returns the data row count

  Calculated fields follow data-model.md "CSV row":
  - `Date` uses the activity Id's own offset
  - `TrackpointNum` is 1-based per activity, across laps
  - `ElapsedSeconds` and `ElapsedTime` are measured from the first timed trackpoint
  - `DistanceMiles` and `AltitudeFeet` are `Value / 1609.344` and `Value / 0.3048`
- [X] T037 [US1] Implement `src/JoakimSoftware.TcxTool.Parser/Conversion/TcxCsvConverter.cs` (success path) so T024 passes (research.md R8):
  1. take the ActivityId from the file name
  2. parse
  3. work out the file names
  4. render each CSV to a `StringWriter`
  5. create the directory
  6. write `<name>.csv.<random>.tmp` as UTF-8 without BOM (`new UTF8Encoding(false)`)
  7. `File.Move(tmp, final, overwrite: true)`

  Return `ConversionResult.Success` with each `SavedCsv`.
- [X] T038 [US1] Add a `"TcxTool"` section to `src/JoakimSoftware.TcxTool.Web/appsettings.json` (`MaxUploadBytes` 52428800, `CsvOutputDirectory` `data/csv`) and to `appsettings.Development.json` (`CsvOutputDirectory` `../../data/csv`), so T025 passes.
- [X] T039 [US1] Create `src/JoakimSoftware.TcxTool.Web/Conversion/TcxToolOptions.cs`, with defaults and startup validation for `TcxToolOptions.MaxUploadBytes` (> 0) and `TcxToolOptions.CsvOutputDirectory` (not blank) as defined in data-model.md "Web types".
- [X] T040 [US1] Create `src/JoakimSoftware.TcxTool.Web/Conversion/CsvConversionService.cs`, with `ConvertAsync(IFormFile, CancellationToken)`. It resolves `TcxToolOptions.CsvOutputDirectory` against `IHostEnvironment.ContentRootPath` when the path is relative, opens the file stream, and calls `TcxCsvConverter.ConvertAsync`. The size check and logging come in US3.
- [X] T041 [P] [US1] Create `src/JoakimSoftware.TcxTool.Web/Components/Conversion/ConversionResultView.razor` with `[Parameter] ConversionResult? Result`, rendering the success state so T026 passes. Add `@using` lines for the new namespaces to `Components/_Imports.razor`.
- [X] T042 [US1] Create `src/JoakimSoftware.TcxTool.Web/Components/Pages/Convert.razor` and `Convert.razor.cs` (code-behind), following contracts/convert-page.md, so T027 passes:
  - static SSR, with no `@rendermode`
  - form name `tcx-upload` with `<AntiforgeryToken />`
  - `[SupplyParameterFromForm(FormName = "tcx-upload")] IFormFile? TcxFile`
  - the submit handler calls `CsvConversionService` and passes the result to `ConversionResultView`
  - the limit hint comes from `TcxToolOptions.MaxUploadBytes` in MB
- [X] T043 [US1] Add the `/convert` link to `src/JoakimSoftware.TcxTool.Web/Components/Pages/Home.razor`, after the description paragraph, so T028 passes and the 001 home-page tests still pass.
- [X] T044 [US1] Register the services in `src/JoakimSoftware.TcxTool.Web/Program.cs`: `TcxToolOptions` bound from `TcxTool` with `ValidateOnStart`, and `CsvConversionService` scoped. This makes T029 pass.
- [X] T045 [US1] Update `compose.yaml` `app` service (research.md R9):
  - environment `TcxTool__CsvOutputDirectory: /data/csv`
  - a bind mount from `./data/csv` to `/data/csv`
  - a comment noting the non-root user writes CSVs to the host

  Confirm `docker compose config` is valid. `compose.test.yaml` is unchanged.

**Checkpoint**: US1 works on its own. `dotnet test` is green, and uploading `run-two-laps.tcx`
on `/convert` writes the golden CSV.

---

## Phase 4: User Story 2 - Convert files that are missing optional data (Priority: P2)

**Goal**: Files missing positions, heart rate, extensions, altitude, distance, times, or Creator
still convert. Those fields are empty and every row keeps 17 fields. An activity with no
trackpoints writes a header-only CSV and says so.

**Independent Test**: Converting `missing-optional.tcx` matches its golden CSV. Converting
`no-trackpoints.tcx` writes a header-only file, and the page shows `contains no trackpoints`.

### Fixtures for User Story 2

- [X] T046 [P] [US2] Create `Fixtures/missing-optional.tcx` with no `Creator`, and with trackpoints that between them lack:
  - `HeartRateBpm`
  - `Position`
  - a `Position` that has only `LatitudeDegrees`
  - `Extensions`
  - `AltitudeMeters`
  - `DistanceMeters`
  - `Time` (including the first trackpoint, so the reference is the first timed one)
  - an element that is present but empty, such as `<AltitudeMeters></AltitudeMeters>`

  Create its golden file `Fixtures/Expected/missing-optional.csv` by hand. Derived fields are empty wherever their source is empty (FR-012).
- [X] T047 [P] [US2] Create `Fixtures/no-trackpoints.tcx`, with one activity, one lap with an empty `Track`, and a second lap with no `Track`. Create its golden file `Fixtures/Expected/no-trackpoints.csv` containing the header line only.

### Tests for User Story 2 ⚠️ write first, see them fail

- [X] T048 [P] [US2] Add to `tests/JoakimSoftware.TcxTool.Tests/Parser/TcxParserTests.cs`, for `missing-optional.tcx` (data-model.md "Validation while mapping"):
  - each missing element gives a null field
  - a `Position` missing one child gives `TcxTrackpoint.Position` null
  - an empty element counts as missing
  - `TcxActivity.Creator` is null

  For `no-trackpoints.tcx`: 2 laps and 0 trackpoints.
- [X] T049 [P] [US2] Add to `tests/JoakimSoftware.TcxTool.Tests/Parser/ActivityCsvWriterTests.cs`:
  - `missing-optional` output equals its golden file, with 17 fields on every line
  - `no-trackpoints` writes the header only and returns 0
- [X] T050 [P] [US2] Add to `tests/JoakimSoftware.TcxTool.Tests/Parser/TcxCsvConverterTests.cs`: `no-trackpoints.tcx` writes a header-only `2026-11-01_no-trackpoints.csv` and returns `SavedCsv.RowCount` 0 (data-model.md "Results").
- [X] T051 [P] [US2] Add to `tests/JoakimSoftware.TcxTool.Tests/Web/ConversionResultViewTests.cs`: a `SavedCsv` with `RowCount` 0 renders `contains no trackpoints` in its `li` (FR-017).
- [X] T052 [US2] Add to `tests/JoakimSoftware.TcxTool.Tests/Web/ConvertEndpointTests.cs`: uploading `no-trackpoints.tcx` shows `contains no trackpoints`, and the header-only file exists.

### Implementation for User Story 2

- [X] T053 [US2] Update `src/JoakimSoftware.TcxTool.Parser/Tcx/TcxParser.cs` so T048 passes. Apply data-model.md "Validation while mapping": a `Position` missing `TcxPosition.LatitudeDegrees` or `TcxPosition.LongitudeDegrees` sets `TcxTrackpoint.Position` to null, and a missing `Creator` sets `TcxActivity.Creator` to null. Also treat missing and empty elements as null, a `Position` missing a child as null, a missing `Creator` or `Sport` as null, and a lap without a `Track` as zero trackpoints. If T048 already passed after T033, show it failing with a temporary break in this handling, then revert.
- [X] T054 [US2] Update `src/JoakimSoftware.TcxTool.Parser/Csv/ActivityCsvWriter.cs` so T049 and T050 pass:
  - empty source gives empty derived fields
  - the elapsed reference is the first trackpoint with a `TcxTrackpoint.Time`
  - a missing time gives empty `Time`, `ElapsedSeconds`, and `ElapsedTime`

  Apply the same temporary-break rule if the tests already pass.
- [X] T055 [US2] Update `src/JoakimSoftware.TcxTool.Web/Components/Conversion/ConversionResultView.razor` to render `contains no trackpoints` for `RowCount` 0, so T051 and T052 pass.

**Checkpoint**: US1 and US2 pass. Both sample-style gaps (no heart rate, missing positions) convert
with aligned fields.

---

## Phase 5: User Story 3 - Get a clear error for a file that cannot be converted (Priority: P3)

**Goal**: Each kind of bad upload gives its own typed, readable error, and nothing is left in
`data/csv/`. The bad uploads are: malformed, not TCX, DTD, invalid value, bad Id, duplicate start,
bad file name, too large, and write failure.

**Independent Test**: Each invalid fixture, uploaded through `ConvertEndpointTests`, shows a
distinct `role=alert` message. The temp output directory is unchanged afterwards.

### Fixtures for User Story 3

- [X] T056 [P] [US3] Create the error fixtures in `tests/JoakimSoftware.TcxTool.Tests/Fixtures/`:
  - `malformed.tcx`: TCX that breaks off mid-trackpoint
  - `not-tcx.tcx`: a well-formed `<gpx>` document
  - `dtd.tcx`: `<!DOCTYPE TrainingCenterDatabase [<!ENTITY x SYSTEM "file:///etc/hosts">]>` with `&x;` used in a `Name`
  - `invalid-number.tcx`: `HeartRateBpm/Value` `abc` at trackpoint 3
  - `invalid-number-nan.tcx`: `AltitudeMeters` `NaN`
  - `invalid-time.tcx`: trackpoint 2 has `<Time>yesterday</Time>`
  - `missing-id.tcx`: an activity with no `Id`
  - `duplicate-start.tcx`: two activities with the same `Id` second

### Tests for User Story 3 ⚠️ write first, see them fail

- [X] T057 [P] [US3] Add error cases to `tests/JoakimSoftware.TcxTool.Tests/Parser/TcxParserTests.cs` (contracts/parser-api.md):
  - `malformed` gives `Malformed`
  - `not-tcx` gives `NotTcx`
  - `dtd` gives `ProhibitedContent`, and the entity text never appears in the result
  - `invalid-number` gives `InvalidValue` with `TcxError.Element` `HeartRateBpm` (a nested value element is named by its parent) and `TcxError.TrackpointNumber` 3
  - `invalid-number-nan` gives `InvalidValue`
  - `invalid-time` gives `InvalidValue` with `Element` `Time` and `TrackpointNumber` 2
  - `missing-id` gives `InvalidActivityId`
  - a non-seekable stream wrapper over `malformed.tcx` still gives `Malformed`
  - no case throws
- [X] T058 [P] [US3] Add to `tests/JoakimSoftware.TcxTool.Tests/Parser/CsvFileNameTests.cs`: `duplicate-start.tcx` gives `DuplicateActivityStart`.
- [X] T059 [P] [US3] Add failure cases to `tests/JoakimSoftware.TcxTool.Tests/Parser/TcxCsvConverterTests.cs`. Seed the `TempDirectory` with an existing `keep.csv` and check afterwards that it is unchanged, no new file exists, and no `*.tmp` remains, for:
  - each error fixture
  - `EmptyFileName` (`.tcx`) and `NotTcxFileName` (`notes.txt`)
  - `OutputFailed`, when `outputDirectory` is the path of an existing regular file
- [X] T060 [P] [US3] Write failing `tests/JoakimSoftware.TcxTool.Tests/Web/CsvConversionServiceTests.cs`, using `FormFile` over a stream that throws if read, `ListLogger`, and a `TempDirectory`, with `TcxToolOptions.MaxUploadBytes` set to 1000 (data-model.md "Web types"):
  - `Length` 1001 gives `TooLarge` without opening the stream, and the message states the limit
  - `Length` exactly 1000 is accepted
  - a failure logs one entry containing the file name and `Kind`, and no fixture content (FR-018)
  - a null `IFormFile`, or one with `Length` 0, gives `NoFile` with the message `Choose a .tcx file to convert.`
- [X] T061 [P] [US3] Add to `tests/JoakimSoftware.TcxTool.Tests/Web/ConversionResultViewTests.cs`:
  - a failure renders `section[role=alert]` containing exactly `TcxError.Message`
  - for `InvalidValue`, the rendered text is exactly the message, so the element and trackpoint number appear once
  - no `li` is rendered on failure
- [X] T062 [US3] Add failure cases to `tests/JoakimSoftware.TcxTool.Tests/Web/ConvertEndpointTests.cs`. Override `TcxTool:MaxUploadBytes` to 1000 for the size cases. Check:
  - no file chosen shows `Choose a .tcx file to convert.`
  - each error fixture shows its message, with no new file in the output directory
  - a 1,500-byte upload (under the ceiling, over the limit) shows `TooLarge`
  - a POST whose `Content-Length` is above the ceiling (limit + 1 MiB), sent without an antiforgery token, gets 303 to `/convert?error=too-large`, and following it shows the `TooLarge` message. This proves `UploadSizeGuard` runs before antiforgery (research.md R2).
  - `GET /convert?error=too-large` shows the message

### Implementation for User Story 3

- [X] T063 [US3] Update `src/JoakimSoftware.TcxTool.Parser/Tcx/TcxParser.cs` so T057 passes:
  - root name check gives `NotTcx`
  - catch `XmlException`, rewind, and run the bounded prolog scan for `<!DOCTYPE`, giving `ProhibitedContent` or `Malformed` (research.md R4)
  - copy a non-seekable stream to a `MemoryStream` first
  - `RecordedNumber` values that fail to parse or are not finite give `InvalidValue`, with element and 1-based trackpoint number
  - a missing or unparseable `TcxActivity.Id` gives `InvalidActivityId` (research.md R11)
  - each `TcxError.Message` is user-readable and contains no file content
- [X] T064 [US3] Update `src/JoakimSoftware.TcxTool.Parser/Csv/CsvFileName.cs` to report `DuplicateActivityStart` when two multi-activity names collide, so T058 passes.
- [X] T065 [US3] Update `src/JoakimSoftware.TcxTool.Parser/Conversion/TcxCsvConverter.cs` so T059 passes (research.md R8):
  - return `EmptyFileName`, `NotTcxFileName`, parse, and duplicate errors before touching the disk
  - on any write or move failure, delete this upload's temp files and already-moved final files, then return `OutputFailed`
- [X] T066 [US3] Update `src/JoakimSoftware.TcxTool.Web/Conversion/CsvConversionService.cs` so T060 passes:
  - check `IFormFile.Length` against `TcxToolOptions.MaxUploadBytes` before opening the stream, giving `TooLarge` with the limit in MB
  - return `NoFile` for a null or empty file
  - log failures through `ILogger<CsvConversionService>` with the file name and `Kind` only (FR-018)
- [X] T067 [US3] Create `src/JoakimSoftware.TcxTool.Web/Conversion/UploadSizeGuard.cs`, middleware that for `POST /convert` with `Content-Length` above `MaxUploadBytes + 1 MiB` returns 303 to `/convert?error=too-large` without reading the body (research.md R2).
- [X] T068 [US3] Update `src/JoakimSoftware.TcxTool.Web/Program.cs` so T062 passes:
  - set Kestrel `MaxRequestBodySize` and `FormOptions.MultipartBodyLengthLimit` to `MaxUploadBytes + 1 MiB`
  - register `UploadSizeGuard` and then call `app.UseRouting()` explicitly, so the guard runs before antiforgery
  - keep `UseStatusCodePagesWithReExecute("/not-found")` and the 001 behavior
- [X] T069 [US3] Update `ConversionResultView.razor` and `Convert.razor(.cs)` so T061 and T062 pass:
  - render `TcxError.Message` in `role=alert`
  - for `?error=too-large`, build the `TcxError` with `TcxErrorMessages` (the `TooLarge` message, with `TcxToolOptions.MaxUploadBytes` in MB) and render it like any other failure. Write no message text in the page itself.

**Checkpoint**: All three stories pass, and every error path leaves the output directory unchanged.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, the full gates, and manual checks with the real samples.

- [X] T070 [P] Update `CLAUDE.md`:
  - Feature history: add a `specs/002-tcx-to-csv` row
  - replace "The parser library has no types yet…" with a short description of the `Tcx/`, `Csv/`, and `Conversion/` namespaces
  - Test packages: add `Microsoft.AspNetCore.Mvc.Testing` 11.0.0 and the `ListLogger` fake
  - Repository layout: note that `data/csv/` is git-ignored and mounted at `/data/csv` in the container
  - add any lessons learned during this feature
- [X] T071 [P] Write `tests/JoakimSoftware.TcxTool.Tests/Parser/ConversionPerformanceTests.cs`. It builds a synthetic TCX of 20,000 trackpoints in memory (no checked-in file and no real coordinates), runs `TcxCsvConverter.ConvertAsync` into a `TempDirectory`, and asserts it finishes in under 10 s and writes 20,000 rows (SC-001, SC-002). This test passes from the start, so show it failing once with a temporary break (for example, assert 19,999 rows), then revert it in the same task (`CLAUDE.md` lesson).
- [X] T072 Run Quality Gates 1–3 (`dotnet build`, `dotnet format --verify-no-changes`, `dotnet test`) on `JoakimSoftware.TcxTool.slnx`, and fix any failure in the code, not the settings.
- [X] T073 Confirm `docker info` succeeds, then run Quality Gate 4: `docker compose -f compose.yaml -f compose.test.yaml up --build --abort-on-container-exit --exit-code-from tests`. Confirm `echo $?` is `0`, then run `docker compose -f compose.yaml -f compose.test.yaml down`.
- [X] T074 Run quickstart.md scenarios 2 and 3 on the host with `data/tcx/24932322230.tcx` and `24964064020.tcx`:
  - each conversion takes under 10 s (SC-001)
  - 19908 and 19914 data rows (SC-002)
  - every line has 17 fields (SC-004)
  - re-conversion is identical (SC-006)
  - each rejection shows the right error

  Record the timings in the task's completion note.

  **Completion note (2026-11-22, host, Development)**: `24932322230.tcx` converted in 0.83 s (19908 rows), `24964064020.tcx` in 0.47 s (19914 rows), both measured by `curl` end to end. Every line has 17 fields. The first row matches the contract example. HeartRateBpm in `24964064020` is empty on every row. Re-conversion gave identical `shasum`s. Rejections: no file gave `Choose a .tcx file to convert.`, `notes.txt` gave `The file name must end in .tcx.`, and a 60 MB `big.tcx` was redirected (303) to `/convert?error=too-large`, which shows `The file is larger than the 50 MB limit.`, with nothing new in `data/csv/`.
- [X] T075 Run quickstart.md scenario 4: upload a sample through `docker compose up --build app` on port 8080, and confirm the CSV appears in the host's `data/csv/`. Then run `docker compose down`, and delete the generated CSVs in `data/csv/` (real GPS).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: none.
- **Foundational (Phase 2)**: depends on Setup. Blocks all stories.
- **US1 (Phase 3)**: depends on Foundational.
- **US2 (Phase 4)**: depends on US1. It extends `TcxParser`, `ActivityCsvWriter`, and `ConversionResultView`.
- **US3 (Phase 5)**: depends on US1. It extends the parser, converter, service, page, and `Program.cs`. It can run alongside US2 only with care, because both edit `TcxParser.cs` and `ConversionResultView.razor`. Sequential order is recommended.
- **Polish (Phase 6)**: depends on all stories.

### Within Each Phase

- Fixtures before the tests that read them.
- Tests before implementation, each seen failing.
- Records (T030–T032) before the parser (T033). Parser before writer, writer before converter, converter before service. Page after service and view.
- `Program.cs` (T044, T068) and `ConvertEndpointTests` (T029, T052, T062) change in sequence, never in parallel.

### Parallel Opportunities

- Setup: T002–T005.
- Foundational: the test tasks T007, T009, T011, and T013 together, then their implementations.
- US1: fixtures T015–T019 together, then tests T020–T028 together (T029 last), then records T030–T032 and T034 together.
- US2: fixtures T046–T047, then tests T048–T051.
- US3: tests T057–T061.

---

## Parallel Example: User Story 1

```text
# Fixtures (different files):
Task: "T015 run-two-laps.tcx + Expected/run-two-laps.csv"
Task: "T016 ride-positions.tcx + Expected/ride-positions.csv"
Task: "T017 multi-activity.tcx + two Expected CSVs"
Task: "T018 fractional-times.tcx + Expected/fractional-times.csv"

# Failing tests (different files):
Task: "T020 Parser/TcxParserTests.cs"
Task: "T021 Parser/ActivityIdNameTests.cs"
Task: "T022 Parser/CsvFileNameTests.cs"
Task: "T023 Parser/ActivityCsvWriterTests.cs"
Task: "T024 Parser/TcxCsvConverterTests.cs"
Task: "T025 Web/AppSettingsTests.cs"
Task: "T026 Web/ConversionResultViewTests.cs"
Task: "T027 Web/ConvertPageTests.cs"

# Records (different files):
Task: "T030 RecordedNumber.cs, RecordedTime.cs"
Task: "T031 TcxPosition … TcxParseResult"
Task: "T032 TcxNamespaces.cs"
Task: "T034 ActivityIdName.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 Setup, then Phase 2 Foundational.
2. Phase 3 (US1): complete files convert end to end on `/convert`.
3. **Stop and validate**: run `dotnet test`, then upload `data/tcx/24932322230.tcx` by hand. That
   sample has a few trackpoints without position, so full alignment is only guaranteed after US2.

### Incremental Delivery

1. US1: the converter works for complete files (MVP).
2. US2: real-world gaps convert. After this, both samples convert correctly.
3. US3: every bad input gets a typed error and leaves no file behind.
4. Polish: `CLAUDE.md`, all four gates, and the manual sample checks.

---

## Notes

- Do not edit `specs/*/checklists/` during `/speckit-implement` (`CLAUDE.md` lesson).
- A task is done only when its tests pass. The feature is done only when T072–T073 pass all four
  Quality Gates.
- Commit after each phase checkpoint, or as the user asks.

## Phase 7: Convergence

- [X] T076 CRITICAL: Give the `/convert` submit path a `CancellationToken` in `src/JoakimSoftware.TcxTool.Web/Components/Pages/ConvertPage.razor.cs`. `private async Task OnSubmitAsync()` takes none. Restore a non-`async` `Task` handler that passes `HttpContext.RequestAborted` to a private `ConvertAsync(CancellationToken)`, and keep `@onsubmit` in `ConvertPage.razor` pointing at it, per Constitution II (`async` methods end in `Async` and accept a `CancellationToken`) (contradicts)
- [X] T077 CRITICAL: Add a `CancellationToken cancellationToken` parameter to the async test helpers and pass it through to the code under test. The helpers are `ParseFixtureAsync` and `ParseErrorAsync` (`Parser/TcxParserTests.cs`), `WriteAsync` (`Parser/ActivityCsvWriterTests.cs`), `ConvertAsync` (`Parser/TcxCsvConverterTests.cs`), and `UploadAsync` (`Web/ConvertEndpointTests.cs`). Callers pass `CancellationToken.None`. Per Constitution II (contradicts)
- [X] T078 Review public members that no contract lists, and make each `internal` unless a contract is updated to name it. The members are `CsvFileName.Date` (used only by `ActivityCsvWriter`), `UploadSizeGuard.RequestCeiling` and `UploadSizeGuard.TooLargeLocation`, and `CsvConversionService.TooLarge()`. Per contracts/parser-api.md and data-model.md "Web types" (unrequested)
- [X] T079 Make the golden files reproducible. Check the generator that computed the success fixtures and their `Expected/*.csv` into the test project under `Fixtures/`, with no project file and not part of the build, and point to it from `Fixtures/README.md`. Or change the README so it no longer refers to a script, per T019 and SC-003 (partial)
- [X] T080 Bring the design docs in line with the code (documentation only):
  - in plan.md, rename `Convert.razor` to `ConvertPage.razor` (the Constitution Check row and the structure tree) and give the reason: a component named `Convert` would hide `System.Convert`
  - in the plan.md fixture tree, describe `dtd.tcx` as a DOCTYPE with an external `SYSTEM` entity
  - in research.md R2, note that .NET 11 antiforgery renders no hidden token and rejects cross-site POSTs by `Sec-Fetch-Site` and `Origin`, which `ConvertEndpointTests` checks

  Per plan: project structure, and T029 and T042 (contradicts)
