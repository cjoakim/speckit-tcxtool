# Research: TCX to CSV Conversion

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Date**: 2026-11-22

Facts below were verified on the development machine on 2026-11-22, using throwaway projects in the
session scratchpad (SDK 11.0.100, pinned by a copy of `global.json`) and the two sample files in
`data/tcx/`:

- A static SSR Blazor form binds an uploaded file to `[SupplyParameterFromForm] IFormFile`.
  Uploading the 13,041,347-byte sample gave `FileName` and `Length` correctly.
- A 40,000,000-byte upload got HTTP 413 from Kestrel (30,000,000-byte default body limit). The
  rejection surfaced as an exception in the antiforgery middleware, not as a page.
- `WebApplicationFactory<Program>` from `Microsoft.AspNetCore.Mvc.Testing` **11.0.0** (on NuGet)
  posted a multipart form with an antiforgery token to a static SSR page and read the rendered
  result. No `public partial class Program` was needed.
- `XDocument.Load` from the secure `XmlReader`: 272 ms and about 61 MB heap for `24932322230.tcx`,
  121 ms and about 52 MB for `24964064020.tcx`. Per-lap streaming (`XNode.ReadFrom`) was no
  better (221 ms / 118 ms, 75 MB / 67 MB peak).
- `double.ToString(CultureInfo.InvariantCulture)` gives the shortest round-trip form but switches
  to exponent form below 1e-5: 0.01 m / 1609.344 prints `6.213711922373339E-06`.
- `double.TryParse(..., NumberStyles.Float, InvariantCulture)` accepts `NaN`.
- With `DtdProcessing.Prohibit`, a DOCTYPE and plain malformed XML both throw `XmlException` with
  the same HResult. They differ only in reader state that cannot be relied on.

## R1. Upload mechanism and render mode

- **Decision**: A static SSR page `/convert` holds a plain `<form method="post"
  enctype="multipart/form-data" @formname="tcx-upload">` with `<AntiforgeryToken />` and
  `<input type="file" name="TcxFile" accept=".tcx">`. The file binds to
  `[SupplyParameterFromForm(FormName = "tcx-upload")] IFormFile? TcxFile`. The page shows the
  result in the response to the post. No `@rendermode`.
- **Upload lifetime** (FR-019): ASP.NET Core buffers form files over 64 KB to temporary files and
  deletes them when the request ends. The app never copies the upload anywhere.
- **Rationale**: Principle I makes static SSR the default and allows interactivity only when a
  component needs it. Upload works without it (verified above). Static SSR also avoids streaming
  15 MB over the SignalR circuit, which `InputFile` would do.
- **Alternatives considered**:
  - `InputFile` with `@rendermode InteractiveServer` was rejected. It needs interactivity, sends
    the file over SignalR, and makes end-to-end tests harder.
  - A minimal API endpoint outside Blazor was rejected. The page would need its own form handling,
    and the result would no longer be a component.

## R2. Size limit enforcement

- **Decision**:
  - The limit is `TcxTool:MaxUploadBytes`, default 52,428,800 (FR-002). It is bound to
    `TcxToolOptions` and validated at startup (must be > 0).
  - Kestrel `MaxRequestBodySize` and `FormOptions.MultipartBodyLengthLimit` are set to
    `MaxUploadBytes` plus a 1 MiB allowance for multipart overhead (the request ceiling).
  - A small middleware, `UploadSizeGuard`, runs before routing and before antiforgery. For a
    `POST /convert` whose `Content-Length` is above the ceiling, it skips reading the body and
    redirects (303) to `/convert?error=too-large`. The page renders the `TooLarge` error with the
    limit.
  - A body under the ceiling but with `IFormFile.Length > MaxUploadBytes` returns `TooLarge` from
    the conversion service before the stream is opened.
  - Both paths reject the file before parsing begins. A file of exactly `MaxUploadBytes` is
    accepted.
- **Rationale**: Kestrel's own rejection surfaces as an exception (verified above), which the user
  would see as the error page. The guard turns every oversize upload a browser can send into the
  same typed error. Browsers always send `Content-Length` on form posts.
- **Ordering note**: `WebApplication` adds routing, then antiforgery, at the start of the pipeline
  unless `UseRouting()` is called. `Program.cs` therefore registers `UploadSizeGuard` and then
  calls `app.UseRouting()` explicitly. An integration test proves the guard runs before
  antiforgery reads the form.
- **Antiforgery on .NET 11** (found during implementation): `<AntiforgeryToken />` renders no
  hidden `__RequestVerificationToken` field. The antiforgery middleware instead rejects cross-site
  POSTs, judged by the `Sec-Fetch-Site` and `Origin` headers, with 400. The page keeps
  `<AntiforgeryToken />` as the contract requires. `ConvertEndpointTests` checks that a cross-site
  POST is rejected and writes nothing, instead of looking for a token field.
- **Alternatives considered**:
  - Unlimited Kestrel body size was rejected: any request could fill the temp disk.
  - Leaving Kestrel's 413 as-is was rejected: the spec requires an error that states the limit.
  - A client-side JavaScript size check was rejected: it adds JS interop for one check, and the
    server must enforce the limit anyway.

## R3. XML loading strategy

- **Decision**: `TcxParser.ParseAsync` opens one `XmlReader.Create(stream, settings)` with
  `DtdProcessing.Prohibit`, `XmlResolver = null`, `MaxCharactersFromEntities = 1024`, and
  `Async = true`. It loads `XDocument.LoadAsync(reader, LoadOptions.None, ct)` and maps elements
  to records with LINQ to XML.
- **Rationale**: this matches the Principle IV wording exactly: `XDocument` loaded from the secure
  reader. Measured cost is well under SC-001's 10 s. At the 50 MB limit, the heap stays around
  200 MB, which is acceptable for a local single-user tool.
- **Alternatives considered**: per-lap streaming with `XNode.ReadFrom` was rejected because it
  measured no better on memory and adds reader-state code. `XmlSerializer` against the TCX schema
  was rejected because it needs generated types for a large schema, of which we use a small part.

## R4. Telling a DTD apart from malformed XML

- **Decision**: when the secure reader throws `XmlException`, the parser rewinds the stream and
  scans only the prolog, the text before the first element start tag. The scan skips whitespace,
  `<?…?>`, and `<!--…-->`. If the next markup is `<!DOCTYPE`, the error is `ProhibitedContent`;
  otherwise it is `Malformed`. The scan reads at most the first 64 KiB, as UTF-8. Nothing outside
  the file is read, because the secure reader already refused the DTD before the scan runs.
- **Rationale**: SC-005 requires a separate message for each invalid fixture. Exception messages
  are localized and not a contract. A bounded prolog scan is deterministic.
- **Stream handling**: the parser needs a seekable stream. `IFormFile.OpenReadStream()` is
  seekable. For a non-seekable stream, the parser first copies it to a `MemoryStream`, which the
  size limit already bounds.
- **Alternatives considered**: matching the `XmlException.Message` text was rejected because it
  depends on the locale and the runtime version. `DtdProcessing.Ignore` was rejected because
  Principle IV requires `Prohibit`.

## R5. Number fidelity and formatting

- **Decision**:
  - Numeric elements are read into `RecordedNumber(string Text, double Value)`. `Text` is the
    element text trimmed of XML whitespace; `Value` is
    `double.Parse(Text, NumberStyles.Float, InvariantCulture)`. Non-finite results (`NaN`, `INF`)
    and parse failures are `InvalidValue` errors (FR-014).
  - Copied fields write `Text` (FR-011). Derived fields compute from `Value`.
  - Derived numbers are formatted by `DerivedNumber.Format(double)`, in four steps:
    1. Format with `ToString("R", InvariantCulture)`, which gives the shortest round-trip digits.
    2. If the result has an exponent (`E±nn`), expand it to plain decimal digits by moving the
       decimal point. This is a string operation only, so no digits change.
    3. Write `-0` as `0`.
    4. Never append a trailing `.0`.
  - Constants: miles = meters / 1609.344; feet = meters / 0.3048 (FR-011a).
- **Rationale**: copying text satisfies "exactly as written" without guessing how a `double`
  would re-print `0.0` or `1609.34`. Expanding the exponent meets "shortest decimal" (FR-011a)
  without the 15-significant-digit loss of custom format strings.
- **Alternatives considered**: `decimal` conversion was rejected because it rounds to 28–29
  digits and changes the value. Custom format strings such as `0.#####…` were rejected because
  they cap at 15 significant digits, which is not round-trip.

## R6. Times, elapsed values, and file-name time parts

- **Decision**:
  - A trackpoint `Time` is `RecordedTime(string Text, DateTimeOffset Value)`, parsed with
    `DateTimeOffset.Parse(Text, InvariantCulture, DateTimeStyles.RoundtripKind)`. A parse failure
    is `InvalidValue`. The `Time` field writes `Text`.
  - The elapsed reference is the first trackpoint in the activity that has a time.
  - `ElapsedSeconds` = `(t - t0).Ticks / 1e7`, formatted with `DerivedNumber.Format`.
  - `ElapsedTime` comes from the whole seconds of the same duration, truncated toward zero:
    `HH:MM:SS`, with hours at least 2 digits and unbounded (`100:00:00`).
  - A trackpoint earlier than the reference is written with a leading `-` on both
    (`-3.5` / `-00:00:03`).
  - The activity `Id` must parse the same way, or the conversion fails with `InvalidActivityId`.
  - `Date` (`yyyy-MM-dd`) and the multi-activity `HHmmss` come from that value as written, in its
    own offset, not converted to UTC.
- **Rationale**: tick arithmetic is exact for TCX's millisecond times, so 3725.6 s prints `3725.6`
  (verified). Using the Id's own offset makes Date equal "the YYYY-MM-DD portion of ID" in the
  spec, including non-UTC offsets.
- **Alternatives considered**: `TimeSpan.ToString(@"hh\:mm\:ss")` was rejected because it wraps at
  24 hours. Converting to UTC was rejected because it would give a Date that is not the date
  written in the Id.

## R7. CSV encoding and determinism

- **Decision**: UTF-8 without BOM, and `\n` line endings, including after the last row. Every
  line, the header included, has 17 fields joined by `|`, with no quoting. Text values (Sport,
  DeviceName, DeviceId, and the copied numeric and time text) go through `CsvText.Clean`: `|`
  becomes a space, and `\r` and `\n` are removed (FR-011b). The writer takes a `TextWriter`, so
  tests compare against golden files byte for byte.
- **Rationale**: fixed line endings and no culture-dependent formatting make SC-006 (identical
  output twice) hold on macOS and in the Linux container.
- **Alternatives considered**: `Environment.NewLine` was rejected because output would differ by
  OS. `\r\n` was rejected because nothing in the spec asks for it, and LF is native on the
  development machine and in the container.

## R8. Writing to `data/csv/` atomically

- **Decision**:
  - `TcxCsvConverter` first renders every activity's CSV into memory (`StringWriter`). If any
    activity fails, it returns an error and writes nothing.
  - It then creates the output directory if needed and writes each CSV to
    `<name>.csv.<random>.tmp` in that directory.
  - It then moves each temp file onto its final name with `File.Move(..., overwrite: true)`.
  - If a write fails, it deletes this upload's temp files and any final files it had already
    moved, then returns `OutputFailed`.
  - Re-converting replaces an existing CSV of the same name (spec edge case).
- **Rationale**: rendering in memory before touching the disk makes parse and validation failures
  leave nothing behind (FR-015, User Story 3 scenario 5). Temp files in the same directory make
  each rename atomic on one file system. In-memory size is bounded: a CSV row is about 200 bytes,
  so 20k rows is about 4 MB.
- **Alternatives considered**: streaming rows straight to the final path was rejected because a
  mid-file failure would leave a partial CSV. A temp directory elsewhere was rejected because a
  move across file systems is not atomic.

## R9. Output directory configuration

- **Decision**:
  - `TcxTool:CsvOutputDirectory`. A relative path resolves against the content root.
  - `appsettings.json` sets `data/csv`.
  - `appsettings.Development.json` sets `../../data/csv`, so `dotnet run` from
    `src/JoakimSoftware.TcxTool.Web` writes to `tcxtool/data/csv/`.
  - In the container, `compose.yaml` sets `TcxTool__CsvOutputDirectory=/data/csv` and bind-mounts
    `./data/csv` to `/data/csv` on the `app` service.
  - `data/csv/` is git-ignored, because CSVs contain GPS positions.
- **Rationale**: one setting covers host and container (deferred item from clarify). The
  `.dockerignore` already excludes `data/`, so no sample or output file enters the image
  (Principle VI).
- **Alternatives considered**: hard-coding a path relative to the repository was rejected because
  it breaks in the container. A named Docker volume was rejected because the user wants the files
  in `data/csv/` on the host.

## R10. Test strategy and the new test package

- **Decision**:
  - **Parser tests** (`tests/…/Parser/`) use `.tcx` fixtures from `Fixtures/` and golden CSVs
    from `Fixtures/Expected/`, all read through `RepositoryRoot`.
  - **Converter tests** write to a per-test temp directory.
  - **bUnit tests** (`Web/`) render `ConversionResultView` from result records.
  - **Integration tests** (`Web/ConvertEndpointTests`) use `WebApplicationFactory<Program>`, with
    `CsvOutputDirectory` overridden to a temp directory and a small `MaxUploadBytes`, so the
    oversize cases need only small payloads. They cover upload, rendering, the guard order, and
    that no file is left behind.
  - **New package**: `Microsoft.AspNetCore.Mvc.Testing` 11.0.0, in the test project only. It runs
    in-process (TestServer), so it uses no network and works under `network_mode: none` after
    restore.
- **Sample files**: the `data/tcx/` row-count and timing checks (SC-001, SC-002) are manual steps
  in [quickstart.md](./quickstart.md). `data/` is excluded from the build context, so those files
  are never in the test container.
- **Rationale**: Principle V wants tests that run without a host or network. The upload path can
  only be proven end to end through the HTTP pipeline. bUnit cannot post a form.
- **Alternatives considered**: bUnit for the whole page was rejected because it cannot exercise
  form binding, antiforgery, or middleware order. Copying sample files into fixtures was rejected
  because the constitution and `CLAUDE.md` forbid it (size, real GPS).

## R11. Spec outstanding items settled here

- **Missing or unparseable activity `Id`**: `InvalidActivityId` error, no CSV (R6).
- **Out-of-order times**: written as negative elapsed values (R6). Not an error.
- **Two uploads of the same file at once**: each upload writes its own temp files and renames them
  last. The last rename wins, and both CSVs are complete. No locking is needed for a local
  single-user tool.
- **Accessibility**: the file input has a visible `<label>`, the result and error regions use
  `role="status"` and `role="alert"`, and the page has one `h1`. These are checked in the bUnit
  tests.
- **Non-`.tcx` extension** (for example `run.xml`): rejected as `NotTcxFileName` before parsing.
  The `.tcx` check ignores letter case. This follows from FR-001 and FR-007.
