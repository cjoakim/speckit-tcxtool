# Contract: Page Markup

**Requirement**: FR-005 to FR-009 | **Stories**: US2, US3

This contract adds styling to the page contracts of earlier features. It does not replace them.
Everything in [001 home-page.md](../../001-project-bootstrap/contracts/home-page.md) and
[002 convert-page.md](../../002-tcx-to-csv/contracts/convert-page.md) still holds. Where 002 shows
`<button type="submit">Convert</button>`, the button now also carries `class="btn btn-primary"`.

## Headings (all pages)

Exactly one `h1`, styled `class="h3 mb-3"`. Home uses `class="h3 mb-0"`, because its description
paragraph follows directly.

## Home (`/`, `Pages/Home.razor`)

| Element | Requirement |
|---------|-------------|
| Heading | `h1.h3.mb-0`: `JoakimSoftware.TcxTool` |
| Description | `p.text-muted`, directly after the `h1` (`h1 + p`) |
| Convert card | `.row.g-3.mt-2` > `.col-sm-6.col-lg-4` > `.card.h-100` > `.card-body`, holding `h2.card-title.h5` "Convert TCX to CSV", `p.card-text.text-muted`, and `a.btn.btn-primary[href=convert]` "Convert" |

## Convert (`/convert`, `Pages/ConvertPage.razor`)

| Element | Requirement |
|---------|-------------|
| Heading | `h1.h3.mb-3`: `Convert TCX to CSV` |
| Wrapper | the form sits in `.card > .card-body` |
| Field group | `div.mb-3` holding the label, input, and hint |
| Label | `label.form-label[for=tcx-file]` |
| File input | `input.form-control#tcx-file`, attributes per contract 002 |
| Limit hint | `div.form-text`: `Maximum file size: <limit>` |
| Submit | `button.btn.btn-primary[type=submit]`: `Convert` |

## Conversion result (`Conversion/ConversionResultView.razor`)

| State | Element |
|-------|---------|
| No result | renders nothing (unchanged) |
| Success | `section[role=status].alert.alert-success.mt-3` > `ul.mb-0` > one `li` per file (text unchanged) |
| Failure | `section[role=alert].alert.alert-danger.mt-3` > `p.mb-0` with the error message |

## Not Found (`/not-found`, `Pages/NotFound.razor`)

`h1.h3.mb-3` "Not Found", then `p.text-muted` with the template sentence. Rendered in `MainLayout`.

## Placeholder pages

| Page | Route | Component | `<PageTitle>` | Content |
|------|-------|-----------|---------------|---------|
| Table | `/table` | `Pages/TablePage.razor` | `Table` | `<h1 class="h3 mb-3">Table</h1>` only |
| Histograms | `/histograms` | `Pages/HistogramsPage.razor` | `Histograms` | `<h1 class="h3 mb-3">Histograms</h1>` only |
| Timeline | `/timeline` | `Pages/TimelinePage.razor` | `Timeline` | `<h1 class="h3 mb-3">Timeline</h1>` only |

The pages are static SSR and have no parameters, services, or code-behind. Later features replace
their content.

## Verified by

- `HomePageTests.ConvertLinkIsAPrimaryButtonOnACard`
- `ConvertPageTests.UsesBootstrapFormStyles`
- `ConversionResultViewTests.ASuccessIsABootstrapSuccessAlert` and
  `AFailureIsABootstrapDangerAlert`
- `PlaceholderPageTests` (bUnit): one `h1` with the page name, and the route attribute
- `PlaceholderPageEndpointTests` (`WebApplicationFactory`): 200, `<title>`, `h1`, and the navbar
- The unchanged 001 and 002 page tests, which check the contracts above still hold
