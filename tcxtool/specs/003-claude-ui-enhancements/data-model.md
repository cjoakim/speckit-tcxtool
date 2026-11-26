# Data Model: Claude UI Enhancements

**Feature**: [spec.md](./spec.md) | **Date**: 2026-11-24

## Domain entities

None. This feature changes only presentation. It adds no parser types, no CSV fields, no options,
and no services. The parser library is untouched.

## UI structure

| Component | Kind | Route | Parameters | Render mode |
|-----------|------|-------|------------|-------------|
| `Layout/MainLayout` | `LayoutComponentBase` | — | `Body` (inherited) | static SSR |
| `Pages/Home` | page | `/` | — | static SSR |
| `Pages/ConvertPage` | page (+ `.razor.cs`, unchanged) | `/convert` | form-bound `TcxFile` (feature 002) | static SSR |
| `Conversion/ConversionResultView` | component | — | `ConversionResult? Result` (feature 002) | static SSR |
| `Pages/NotFound` | page | `/not-found` | — | static SSR |
| `Pages/TablePage` | page | `/table` | — | static SSR |
| `Pages/HistogramsPage` | page | `/histograms` | — | static SSR |
| `Pages/TimelinePage` | page | `/timeline` | — | static SSR |

## Static assets

| Path under `wwwroot/` | Version | Bytes | Origin |
|-----------------------|---------|-------|--------|
| `lib/bootstrap/css/bootstrap.min.css` | 5.3.0 | 232,914 | PLog `static/bootstrap/css/` |
| `lib/bootstrap/js/bootstrap.bundle.min.js` | 5.3.0 | 80,421 | PLog `static/bootstrap/js/` |
| `app.css` | — | unchanged | Blazor template |
