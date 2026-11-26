# Contract: Developer Commands and Quality Gates

**Requirement**: FR-005–FR-012 | **Stories**: US2, US3

Every command runs from `tcxtool/`. Exit status is the contract: 0 means pass, anything else means
fail.

## Gate commands (constitution Quality Gates 1–4)

| # | Command | Pass condition |
|---|---------|----------------|
| 1 | `dotnet build JoakimSoftware.TcxTool.slnx` | Exit 0, `0 Warning(s)`, `0 Error(s)` |
| 2 | `dotnet format JoakimSoftware.TcxTool.slnx --verify-no-changes` | Exit 0 |
| 3 | `dotnet test JoakimSoftware.TcxTool.slnx` | Exit 0, all tests pass |
| 4 | `docker compose -f compose.yaml -f compose.test.yaml up --build --abort-on-container-exit --exit-code-from tests` | Exit 0 (the `tests` service's exit code) |

## Run commands

| Purpose | Command | Result |
|---------|---------|--------|
| Run locally | `dotnet run --project src/JoakimSoftware.TcxTool.Web` | The app is served on the http URL printed in the console |
| Run in a container | `docker compose up --build app` | The app is served on `http://localhost:${TCXTOOL_HOST_PORT:-8080}` |
| Use another host port | `TCXTOOL_HOST_PORT=8081 docker compose up --build app` | The app is served on port 8081 |

## Compose services

| Service | File | Build target | Network | Notes |
|---------|------|--------------|---------|-------|
| `app` | `compose.yaml` | `runtime` | default | Non-root (`$APP_UID`), container port 8080 |
| `tests` | `compose.test.yaml` | `build` | `none` | `dotnet test --no-restore`. Fixtures mounted read-only. No `depends_on`. |

## Automated checks inside gate 3/4

| Test class | Asserts | Fails when |
|------------|---------|------------|
| `HomePageTests` | Home `h1` = `JoakimSoftware.TcxTool`, and the description is not empty | The home page content changes or goes missing |
| `ParserIndependenceTests` | The Parser `.csproj` has no web SDK, `FrameworkReference`, Web `ProjectReference`, or `Microsoft.AspNetCore*` package, and its assembly references no web assembly | Any web dependency is added to Parser, even an unused one |
| `ProjectSettingsTests` | Every `.slnx` project declares the four Principle II settings | A project omits or changes any of them |

## Runtime image content rules (SC-006)

The `runtime` image contains only the publish output under `/app` plus the base ASP.NET runtime. It
contains no SDK (`dotnet --list-sdks` prints nothing), no `.tcx` or `.csv` files, no `.git`, no
`obj` or `bin` intermediates, and no secrets.
