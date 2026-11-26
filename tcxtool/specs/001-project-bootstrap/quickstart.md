# Quickstart & Validation: Project Bootstrap

**Feature**: [spec.md](./spec.md) | **Contracts**: [home-page](./contracts/home-page.md),
[quality-gates](./contracts/quality-gates.md)

Run every command from `tcxtool/`, not from the git root (`cj-speckit-explore/`).

## Prerequisites

- macOS with zsh
- .NET SDK 11.0.100 or a later 11.0 feature band (`dotnet --list-sdks`)
- Docker Desktop **running** (`docker info` succeeds), with Compose v2
- Network access for the first restore and base-image pull

## Scenario 1: Local run (US1, SC-001)

```zsh
dotnet build JoakimSoftware.TcxTool.slnx
dotnet run --project src/JoakimSoftware.TcxTool.Web
```

Expected:
- The build ends with `0 Warning(s)` and `0 Error(s)`.
- Opening the printed URL shows the `JoakimSoftware.TcxTool` heading and a one-line description.
- Appending `/counter` to the URL shows the not-found page.

## Scenario 2: Quality gates on the host (US2, SC-002, SC-003)

```zsh
dotnet format JoakimSoftware.TcxTool.slnx --verify-no-changes
dotnet test JoakimSoftware.TcxTool.slnx
```

Expected: both commands exit 0. The test output includes `HomePageTests`,
`ParserIndependenceTests`, and `ProjectSettingsTests`.

Negative checks (revert each one afterwards):
- Add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to the Parser `.csproj`.
  `dotnet test` should then fail in `ParserIndependenceTests`.
- Add an unused local variable to `Program.cs`. `dotnet build` should then fail because warnings
  are errors.

## Scenario 3: Containers (US3, SC-004, SC-005, SC-006)

```zsh
docker compose build app
docker compose up -d app; start=$SECONDS
until curl -fs -o /dev/null http://localhost:8080/ || (( SECONDS - start >= 30 )); do sleep 1; done
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:8080/      # expect 200
echo "$(( SECONDS - start ))s"                                        # expect < 30s (SC-005)
docker compose exec app id -u                                         # expect non-zero (1654)
docker compose exec app dotnet --list-sdks                            # expect no output
docker compose exec app sh -c 'find / -name "*.tcx" -o -name ".git" 2>/dev/null | head'  # expect no output
docker compose down
```

The gate (constitution Principle VII):

```zsh
docker compose -f compose.yaml -f compose.test.yaml up \
  --build --abort-on-container-exit --exit-code-from tests
echo $?   # expect 0
```

Negative check: add `[Fact] public void Fail() => false.Should().BeTrue();` to any test class, run
the gate again, and `echo $?` should print a non-zero value. Remove the failing test afterwards.

Port in use: `TCXTOOL_HOST_PORT=8081 docker compose up --build app` serves the app on port 8081.

## Done

The feature is complete when scenarios 1–3 all match their expected results and the `CLAUDE.md`
"Build and test commands" section lists these same commands (FR-012).
