# Data Model: Project Bootstrap

**Feature**: [spec.md](./spec.md) | **Date**: 2026-11-22

## Domain entities

None. This feature parses no TCX, produces no CSV, and stores nothing (FR-013). Activity, Lap,
Trackpoint, and the typed parse result are introduced by the first parser feature as immutable
`record` types (Principle II).

## Structural model

These are the build-time "entities" that the bootstrap creates and that the automated tests check.

| Component | Kind | Root namespace | Depends on | Must not depend on |
|-----------|------|----------------|------------|--------------------|
| `JoakimSoftware.TcxTool.Parser` | Class library (`Microsoft.NET.Sdk`) | `JoakimSoftware.TcxTool.Parser` | BCL only | ASP.NET Core, Blazor, `…Web` |
| `JoakimSoftware.TcxTool.Web` | Blazor Web App (`Microsoft.NET.Sdk.Web`) | `JoakimSoftware.TcxTool.Web` | `…Parser` | `…Tests` |
| `JoakimSoftware.TcxTool.Tests` | xUnit test project | `JoakimSoftware.TcxTool.Tests` | `…Parser`, `…Web`, test packages | — |

The Web project references the Parser project from the start, even though no parser types exist
yet. That fixes the dependency direction before feature 2.

### Validation rules (enforced by tests, see [contracts/quality-gates.md](./contracts/quality-gates.md))

- Every project in `JoakimSoftware.TcxTool.slnx` declares `Nullable=enable`,
  `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, and `AnalysisLevel=latest`.
- The Parser project has no web SDK, no framework reference, no reference to the Web project, and
  no `Microsoft.AspNetCore*` package, and its compiled assembly references none of them.
- The solution contains exactly these three projects. A fourth needs a recorded reason under
  Governance.
