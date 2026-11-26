# Contract: Convert Page (`/convert`)

| Route | Render mode | Method |
|-------|-------------|--------|
| `/convert` | Static SSR (no `@rendermode`) | `GET` shows the form; `POST` (multipart, antiforgery) converts and shows the result |
| `/convert?error=too-large` | Static SSR | `GET` shows the form plus the `TooLarge` error (target of the `UploadSizeGuard` redirect) |

The home page (`/`) keeps its `h1` and description (feature 001 contract) and adds one link to
`/convert`.

## Markup

| Element | Requirement |
|---------|-------------|
| Heading | exactly one `h1`: `Convert TCX to CSV` |
| Form | `method="post"`, `enctype="multipart/form-data"`, form name `tcx-upload`, contains `<AntiforgeryToken />` |
| File input | `type="file"`, `name="TcxFile"`, `accept=".tcx"`, `id="tcx-file"`, with `<label for="tcx-file">` |
| Limit hint | text near the input stating the limit, for example `Maximum file size: 50 MB` |
| Submit | `<button type="submit">Convert</button>` |

## Result region (`ConversionResultView`, takes `[Parameter] ConversionResult? Result`)

| State | Rendering |
|-------|-----------|
| No result (`GET`) | nothing |
| Success | `<section role="status">` with a list: one `<li>` per saved file, reading `<FileName>: <RowCount> rows` |
| Success, an activity with 0 rows | that `<li>` also says `contains no trackpoints` (FR-017) |
| Failure | `<section role="alert">` rendering `TcxError.Message` as given. For `InvalidValue`, the message already names the element and trackpoint number. No file list. |
| No file chosen | the `NoFile` error from `CsvConversionService`, rendered like any failure: `Choose a .tcx file to convert.` |

Each `TcxErrorKind` has its own message, defined once in `TcxErrorMessages` (SC-005). The view never
composes messages. Messages never include file content.

## Behavior

- `POST` → `CsvConversionService.ConvertAsync` → render `ConversionResultView`. No redirect after
  a successful post. Refreshing the page re-posts, and re-converting replaces the file.
- The uploaded TCX is never saved. Only the request's buffered form data holds it, and ASP.NET Core
  disposes that at the end of the request (FR-019).

## Verification

- bUnit (`Web/ConversionResultViewTests`, `Web/ConvertPageTests`): markup and each result state.
- Integration (`Web/ConvertEndpointTests`, `WebApplicationFactory`):
  - a fixture upload writes the expected CSV to the overridden output directory, and the
    response lists it
  - an upload over the limit (by `Length`, and by `Content-Length` above the ceiling) shows
    `TooLarge`, with no file written
  - an invalid fixture shows its error, with no file written
