using System.Text.Json;

using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies the shipped <c>TcxTool</c> settings (spec FR-002, research R9, data-model.md
/// TcxToolOptions.MaxUploadBytes and TcxToolOptions.CsvOutputDirectory; spec 004 research R2,
/// TcxToolOptions.StateFilePath).
/// </summary>
public class AppSettingsTests
{
    private const string WebProject = "src/JoakimSoftware.TcxTool.Web";

    [Fact]
    public void DefaultSettingsSetTheFiftyMegabyteLimitAndOutputDirectory()
    {
        var section = ReadTcxToolSection("appsettings.json");

        section.GetProperty("MaxUploadBytes").GetInt64().Should().Be(52_428_800);
        section.GetProperty("CsvOutputDirectory").GetString().Should().Be("data/csv");
    }

    [Fact]
    public void DevelopmentSettingsPointAtTheRepositoryDataDirectory()
    {
        var section = ReadTcxToolSection("appsettings.Development.json");

        section.GetProperty("CsvOutputDirectory").GetString().Should().Be("../../data/csv");
    }

    [Fact]
    public void DefaultSettingsPutTheStateFileInTheContentRoot()
    {
        ReadTcxToolSection("appsettings.json").GetProperty("StateFilePath").GetString()
            .Should().Be("tcxtool-state.json");
    }

    [Fact]
    public void DevelopmentSettingsPutTheStateFileInTheProjectDirectory()
    {
        ReadTcxToolSection("appsettings.Development.json").GetProperty("StateFilePath").GetString()
            .Should().Be("../../tcxtool-state.json");
    }

    [Fact]
    public void ComposeSetsAWritableStateFilePathForTheAppContainer()
    {
        // compose.yaml is in the Docker build context, so this also runs in the Compose gate.
        var lines = File.ReadAllLines(RepositoryRoot.Combine("compose.yaml"));
        var app = lines.SkipWhile(line => line != "  app:").Skip(1).TakeWhile(line => line.Length == 0 || line.StartsWith("    ", StringComparison.Ordinal));

        app.Select(line => line.Trim()).Should().Contain("TcxTool__StateFilePath: /tmp/tcxtool/tcxtool-state.json");
    }

    private static JsonElement ReadTcxToolSection(string fileName)
    {
        var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        using var document = JsonDocument.Parse(File.ReadAllText(RepositoryRoot.Combine($"{WebProject}/{fileName}")), options);
        return document.RootElement.GetProperty("TcxTool").Clone();
    }
}
