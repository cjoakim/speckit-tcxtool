# Contract: Pages

**Requirements**: FR-001 to FR-004, FR-011 to FR-019 | **Stories**: US1, US2, US3

This contract extends [003 pages.md](../../003-claude-ui-enhancements/contracts/pages.md) and
[003 ui-shell.md](../../003-claude-ui-enhancements/contracts/ui-shell.md). Everything in them, and
in the 001 and 002 contracts they build on, still holds. The one exception is the navbar link list,
which this contract replaces. Every page stays static SSR, and no `@rendermode` is added.

## Navbar (`Layout/MainLayout.razor`)

`nav a.nav-link`, in this order:

| Text | `href` |
|------|--------|
| Home | `` |
| Convert | `convert` |
| Table | `table` |
| Histograms | `histograms` |
| Timeline | `timeline` |
| About | `about` |

About is a `NavLink`, so it is marked `active` on `/about` only.

## About (`/about`, `Pages/AboutPage.razor`)

| Element | Requirement |
|---------|-------------|
| `<PageTitle>` | `About` |
| Heading | exactly one `h1.h3.mb-3`: `About` |
| Intro | one `p`. Its text, with whitespace collapsed, is the FR-003 paragraph. `TcxTool`, `Chris Joakim`, `Software Factory`, `Claude Code`, and `Spec-Kit` are each in a `strong`. |
| Subheadings | `h2` elements, in order: `Spec-Kit Process`, `Spec-Kit Project Startup`, `Spec-Kit Feature/Spec Development Loop`, `Links` |
| Startup list | `ul` after "Spec-Kit Project Startup", with 1 `li`: `/speckit-constitution - Establish project principles` |
| Loop list | `ul` after "Spec-Kit Feature/Spec Development Loop", with 5 `li`, in FR-003 order and with FR-003 text |
| Links list | `ul` after "Links", with 5 `li > a`, text and `href` exactly as in the table below |

| Link text | `href` |
|-----------|--------|
| speckit.org | `https://speckit.org` |
| spec-kit @ GitHub | `https://github.com/github/spec-kit` |
| Spec-Kit Quickstart | `https://github.github.io/spec-kit/quickstart.html` |
| specify-cli @ PyPI | `https://pypi.org/project/specify-cli/` |
| GitHub repo for this project | `https://github.com/cjoakim/cj-speckit-explore` |

The page loads nothing from the network: no images, scripts, or styles beyond the layout's own.

## Home (`/`, `Pages/Home.razor` + `Home.razor.cs`)

The heading, the description, and the Convert card are unchanged (003). The row
`.row.g-3.mt-2` gains a second column after the Convert column:

| Element | Requirement |
|---------|-------------|
| Column | `.col-sm-6.col-lg-4`, the same classes as the Convert column |
| Card | `.card.h-100` > `.card-body#select-csv` |
| Title | `h2.card-title.h5`: `Select CSV File` |
| Description | `p.card-text.text-muted`: `Choose a CSV file converted earlier.` |
| Current CSV line | `p.card-text#current-csv`, with text by status (below) |
| Form (when at least 1 CSV is listed) | `form[method=post]` with `@formname="csv-select"` and `<AntiforgeryToken />`, holding `div.mb-3` > `label.form-label[for=csv-file]` "CSV file" + `select.form-select#csv-file[name=CsvFilename]`, then `button.btn.btn-primary[type=submit]` "Select" |
| Options | first `option[value=""]`: `Choose a CSV file` (`disabled`; `selected` only when no listed name is current). Then one `option` per listed CSV, with `value` = text = file name, newest first. The current CSV's option is `selected`. |
| Empty state (no CSVs listed) | no `select` and no submit button; `p.card-text`: `No CSV files yet.` followed by `a[href=convert]` `Convert a TCX file`. An empty `form` with `@formname="csv-select"` and `<AntiforgeryToken />` stays, so that a selection posted after the last CSV was deleted renders the `NotFound` notice, not a bare 400 (T037). |
| Selection result | `CurrentCsvNotice` inside the card body, after the form, when a POST was handled |

**Current CSV line text**:

| `CurrentCsvStatus` | Text |
|--------------------|------|
| `None` | `No CSV selected yet.` |
| `Present` | `Current CSV: ` + `strong` with the name |
| `Missing` | `Current CSV: ` + `strong` with the name + ` (file missing)` |

**POST `/`** (form `csv-select`, field `CsvFilename`):
- `200` with the page re-rendered. The current CSV line shows the state after the POST.
- A cross-site POST (`Sec-Fetch-Site: cross-site`) gets `400`, and the state file is unchanged.

## Convert (`/convert`)

The markup is unchanged (002/003). After a POST:

- On success, `CurrentCsvNotice` renders directly after `ConversionResultView`, with the result of
  `RecordConversionAsync`.
- On failure, no notice renders, and the state file is unchanged.

## `CurrentCsvNotice` (`Components/State/CurrentCsvNotice.razor`)

| `Result` | Markup |
|----------|--------|
| `null` | nothing |
| `Recorded` | `p.mt-3.mb-0[role=status]`: `Current CSV: ` + `strong` with the name |
| any other outcome | `div.alert.alert-warning.mt-3[role=alert]` > `p.mb-0` with `CsvSelectionMessages.For(outcome)` |

## Messages

`CsvSelectionMessages.For` is the only place these strings appear:

| Outcome | Text |
|---------|------|
| `NoneChosen` | `Choose a CSV file to select.` |
| `InvalidName` | `That is not a CSV file this app can use.` |
| `NotFound` | `That CSV file is no longer in the CSV folder.` |
| `WriteFailed` | `The current CSV could not be recorded.` |

`Recorded` has no message. Its notice shows the file name instead.

The messages say "CSV folder", not the design term "CSV output directory", on purpose: it is the
plain name for the user. Both mean `TcxTool:CsvOutputDirectory`.

## Verified by

- `Web/MainLayoutTests.NavbarLinksOnlyToPagesThatExist` (updated list)
- `Web/AboutPageTests` (bUnit) and `Web/AboutPageEndpointTests` (HTTP)
- `Web/HomePageTests` (bUnit: card, options, statuses, empty state)
- `Web/HomeSelectEndpointTests` (HTTP: select, rejections, cross-site 400, state on disk)
- `Web/ConvertEndpointTests` (+ state recorded on success, unchanged on failure)
- `Web/CurrentCsvNoticeTests` (bUnit)
