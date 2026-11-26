# D3 (vendored)

- **Version**: 7.9.2
- **Licence**: ISC, in `LICENSE` (kept as the licence requires)
- **Source**: `https://registry.npmjs.org/d3/-/d3-7.9.2.tgz`, file `package/dist/d3.min.js`
- **Tarball integrity** (npm): `sha512-ZKb/mONPVKzaO5UlcWcwN8bS4ezvQaYu+bsou+XgNABdGC800kwH651ADTbbTxMPHW13UlvbQafdmJQDk1w7Mg==`
- **`d3.min.js` SHA-256**: `912111098ea99503a6f4967361d2bbdc9dbf58cf9c678c8e27e5b02d1a07f0c1`

This is the self-contained UMD bundle, which defines the `d3` global. The package's
`d3.min.mjs` only re-exports 30 bare `d3-*` package names, which a browser cannot resolve
without an import map, so it is not used (specs/007-timeline-page/tasks.md T031 done note).
`wwwroot/js/timeline.js` adds a `<script>` for this file the first time a `<tcx-timeline>`
element is connected, so pages without a timeline never fetch it. No CDN is used, so the app
needs no network.
