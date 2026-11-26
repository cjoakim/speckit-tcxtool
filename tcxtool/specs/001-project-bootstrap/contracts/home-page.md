# Contract: Home Page (UI)

**Requirement**: FR-003, FR-004 | **Story**: US1

## Route

| Route | Render mode | Response |
|-------|-------------|----------|
| `/` | Static SSR (no `@rendermode`) | 200, HTML |
| `/not-found` and any unknown route | Static SSR | 404 status, the template's NotFound page |
| `/Error` | Static SSR | Error page (production only, through the exception handler) |
| `/counter`, `/weather` | — | 404 (template demo pages are absent) |

## Content of `/`

- The browser tab title is `JoakimSoftware.TcxTool`.
- The page has exactly one `h1` with the text `JoakimSoftware.TcxTool`.
- Directly after it is one `p` with a one-sentence description, for example: "Parse Garmin TCX
  activity files, convert them to CSV, and chart a single activity." The exact wording is free, but
  it must not be empty.
- There are no links to features that don't exist yet.

## Verified by

- `HomePageTests` (bUnit): checks the `h1` text and that the description is not empty.
- Manual or `curl` check in [quickstart.md](../quickstart.md): `/` returns 200 and contains the
  title, and `/counter` returns 404.
