# Data Model: About Page and Current CSV State

**Feature**: `004-about-and-csv-state` | **Spec**: [spec.md](./spec.md) | **Research**: [research.md](./research.md)

Every new type is in the Web project, in namespace `JoakimSoftware.TcxTool.Web.State`, unless
stated otherwise. There is one public type per file (Principle II). The parser library gets no new
types.

## Persisted: the state file

| Property | Value |
|----------|-------|
| Name | `tcxtool-state.json` |
| Location | `TcxTool:StateFilePath` (research R2) |
| Shape | a JSON object with exactly one property, `csvFilename` (string) |
| Written | indented, UTF-8 without BOM, trailing `\n`, replaced whole by temp-file-then-rename |
| Read | at most 64 KiB; extra properties ignored; anything invalid means "no current CSV" |

The exact format is in [contracts/state-file.md](./contracts/state-file.md).

## Types

### `TcxToolOptions` (existing, `JoakimSoftware.TcxTool.Web.Conversion`)

New property:

| Property | Type | Rule | Default |
|----------|------|------|---------|
| `StateFilePath` | `string` | `[Required(AllowEmptyStrings = false)]`. A relative path resolves against the content root. | `tcxtool-state.json` |

Existing properties `MaxUploadBytes` and `CsvOutputDirectory` are unchanged.

### `TcxToolPaths` (new, `JoakimSoftware.TcxTool.Web.Conversion`)

A `public static` helper, with XML doc comments, that is the one source of truth for resolving
configured paths. It is public so the test project can test it directly, because the Web project
has no `InternalsVisibleTo`.

- `CsvDirectory(TcxToolOptions, IHostEnvironment) → string`: full path
- `StateFile(TcxToolOptions, IHostEnvironment) → string`: full path

`CsvConversionService.OutputDirectory()` switches to it. The behavior is unchanged.

### `CsvFileNameRule` (new, static)

`IsPlainCsvName(string? name) → bool`, with the rule in research R4. It is the single gate for
names that come from the form, the state file, and the directory listing.

### `CurrentCsvStatus` (new, enum)

| Value | Meaning |
|-------|---------|
| `None` | no state file, or the file is unreadable or invalid |
| `Present` | the state names a file that exists in the CSV directory |
| `Missing` | the state names a valid name, but that file is not in the CSV directory |

### `CurrentCsv` (new, `sealed record`)

| Property | Type | Rule |
|----------|------|------|
| `FileName` | `string?` | null exactly when `Status` is `None` |
| `Status` | `CurrentCsvStatus` | |

Static members: `CurrentCsv.None` (shared instance), `Present(string)`, and `Missing(string)`.
This is what later analysis pages consume (FR-009). They use `FileName` only when the status is
`Present`.

### `CsvSelectionOutcome` (new, enum)

| Value | When | State file |
|-------|------|------------|
| `Recorded` | the name passed every check and the file was written | replaced |
| `NoneChosen` | the name was null, empty, or whitespace | unchanged |
| `InvalidName` | the name failed `CsvFileNameRule`, or resolved outside the CSV directory | unchanged |
| `NotFound` | the name was valid but no such file is in the CSV directory | unchanged |
| `WriteFailed` | the write threw an I/O or access error | unchanged (the temp file is removed) |

### `CsvSelectionResult` (new, `sealed record`)

| Property | Type | Rule |
|----------|------|------|
| `Outcome` | `CsvSelectionOutcome` | |
| `FileName` | `string?` | the requested name as given, or null for `NoneChosen` |
| `IsRecorded` | `bool` | `Outcome == Recorded` |

### `CsvSelectionMessages` (new, static)

`For(CsvSelectionOutcome) → string`. This is the only place this feature's user-facing text is
written, mirroring `TcxErrorMessages`. The exact strings are in
[contracts/pages.md](./contracts/pages.md#messages).

### `CurrentCsvStore` (new, `sealed partial class`, singleton)

Constructor: `(IOptions<TcxToolOptions>, IHostEnvironment, ILogger<CurrentCsvStore>)`.

| Member | Returns | Behavior |
|--------|---------|----------|
| `ReadAsync(CancellationToken)` | `Task<CurrentCsv>` | Reads and validates the state file (research R3). A missing file returns `None` with no log; an invalid file returns `None` and logs a warning. A valid name returns `Present` or `Missing`, depending on `File.Exists` in the CSV directory. |
| `ListCsvFiles()` | `IReadOnlyList<string>` | The top-level names in the CSV directory that pass `CsvFileNameRule`, sorted descending ordinal (research R5). A missing directory returns `[]`. |
| `SelectAsync(string? fileName, CancellationToken)` | `Task<CsvSelectionResult>` | Validates the name (`NoneChosen`, `InvalidName`, `NotFound`), then writes it. Writes are serialized by a `SemaphoreSlim`. |
| `RecordConversionAsync(ConversionResult, CancellationToken)` | `Task<CsvSelectionResult?>` | `null` when `!result.IsSuccess` or `result.Files` is empty. Otherwise `SelectAsync(result.Files[0].FileName, token)`. |

It is registered in `Program.cs` as `AddSingleton<CurrentCsvStore>()`. It never throws for bad
input or I/O failure. `OperationCanceledException` propagates, as it does elsewhere in the app.

### `TcxToolState` (new, `sealed record`, internal)

The serialization shape: `TcxToolState(string CsvFilename)`, with
`[property: JsonPropertyName("csvFilename")]`. It is used for writing only. Reads go through
`JsonDocument` (research R3).

## Lifecycle of the current CSV

| From | Event | To |
|------|-------|----|
| any | successful conversion (first saved file) | `Present` |
| any | successful selection on the home page | `Present` |
| `Present` | the named CSV is deleted from the CSV directory | `Missing` |
| `Missing` | a CSV with that name appears again | `Present` |
| any | state file deleted, corrupted, or made invalid | `None` |

- A failed conversion, a rejected selection, or a failed write leaves the state where it was.
- `Missing` is never written. It is derived on read from `File.Exists`.

## Components (Web project)

| Component | New/changed | Parameters / injections | Notes |
|-----------|-------------|-------------------------|-------|
| `Pages/AboutPage.razor` | new | none | `/about`, static markup (research R10) |
| `Pages/Home.razor` + `Home.razor.cs` | changed | injects `CurrentCsvStore`; `[CascadingParameter] HttpContext?`; `[SupplyParameterFromForm(FormName = "csv-select")] string? CsvFilename` | adds the Select CSV File card (research R6) |
| `Pages/ConvertPage.razor` + `.razor.cs` | changed | injects `CurrentCsvStore` | stores `CsvSelectionResult? Recorded` after a conversion |
| `State/CurrentCsvNotice.razor` | new | `[Parameter] CsvSelectionResult? Result` | research R9 |
| `Layout/MainLayout.razor` | changed | none | adds the About link last |
| `Conversion/ConversionResultView.razor` | unchanged | | contracts 002/003 hold |

`_Imports.razor` gains `@using JoakimSoftware.TcxTool.Web.State` and
`@using JoakimSoftware.TcxTool.Web.Components.State`.

## Test support types (test project)

| Type | Folder | Purpose |
|------|--------|---------|
| `TcxToolWebFactory` | `Support/` | Creates a `WebApplicationFactory<Program>` with the CSV directory and state file both inside its own `TempDirectory`, plus optional extra settings. Disposes both. Exposes `CsvDirectory` and `StateFilePath`. |
