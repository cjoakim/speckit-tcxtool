# syntax=docker/dockerfile:1
# JoakimSoftware.TcxTool: multi-stage, non-root build (constitution Principle VI).

# ---- build: SDK only; also the stage the Compose test gate runs in (Principle VII) ----
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src

# Restore the whole solution first (cached layer), so the test container can run offline.
COPY global.json JoakimSoftware.TcxTool.slnx .editorconfig ./
COPY src/JoakimSoftware.TcxTool.Parser/JoakimSoftware.TcxTool.Parser.csproj src/JoakimSoftware.TcxTool.Parser/
COPY src/JoakimSoftware.TcxTool.Web/JoakimSoftware.TcxTool.Web.csproj src/JoakimSoftware.TcxTool.Web/
COPY tests/JoakimSoftware.TcxTool.Tests/JoakimSoftware.TcxTool.Tests.csproj tests/JoakimSoftware.TcxTool.Tests/
RUN dotnet restore JoakimSoftware.TcxTool.slnx

COPY . .
RUN dotnet publish src/JoakimSoftware.TcxTool.Web/JoakimSoftware.TcxTool.Web.csproj \
    -c Release --no-restore -o /app/publish /p:UseAppHost=false

# ---- runtime: ASP.NET Core runtime only, non-root, port 8080 ----
FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "JoakimSoftware.TcxTool.Web.dll"]
