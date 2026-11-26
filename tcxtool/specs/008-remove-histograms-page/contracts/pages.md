# Contract: Pages and Navbar after Feature 008

**Feature**: `specs/008-remove-histograms-page/spec.md`

This replaces the page list and navbar of earlier contracts. Each page's own content contract is
unchanged.

## Routes

The Web app routes exactly these templates and no others. A test reflects over the Web assembly to
check this set, ignoring order (research.md R2):

| Template | Page |
|----------|------|
| `/` | Home |
| `/convert` | Convert |
| `/table` | Table |
| `/states` | States |
| `/timeline` | Timeline |
| `/about` | About |
| `/not-found` | Not Found, the status-code page |
| `/Error` | Error, the exception page |

## Unknown addresses

| Request | Response |
|---------|----------|
| `GET` any address with no route above, for example `/no-such-page` or the removed Histograms address in any casing or with a trailing slash | status 404, the `Not Found` heading (`h1`) and its text, inside `MainLayout` (navbar and footer) |

No redirect. The removed address has no special case (FR-002).

## Navbar

`MainLayout` renders these `a.nav-link` elements, in this order, and no others (FR-003, SC-001):

| Text | `href` |
|------|--------|
| Home | `""` |
| Convert | `convert` |
| Table | `table` |
| States | `states` |
| Timeline | `timeline` |
| About | `about` |

On each page, only that page's link is active, as before (FR-008).
