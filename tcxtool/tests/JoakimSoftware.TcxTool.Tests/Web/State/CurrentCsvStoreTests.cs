using System.Text;
using System.Text.Json;

using JoakimSoftware.TcxTool.Parser.Conversion;
using JoakimSoftware.TcxTool.Tests.Support;
using JoakimSoftware.TcxTool.Web.Conversion;
using JoakimSoftware.TcxTool.Web.State;

using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoakimSoftware.TcxTool.Tests.Web.State;

/// <summary>
/// Verifies <see cref="CurrentCsvStore"/> against specs/004-about-and-csv-state/contracts/state-file.md
/// ("Read rules", "Content written", "Write procedure"), data-model.md CsvSelectionOutcome, and the
/// <c>statesCsvFilename</c> additions of specs/006-activity-states/contracts/state-file.md.
/// </summary>
public sealed class CurrentCsvStoreTests : IDisposable
{
    private const string SeedState = "{\n  \"csvFilename\": \"seed.csv\"\n}\n";

    private readonly TempDirectory _root = new();
    private readonly ListLogger<CurrentCsvStore> _logger = new();

    public CurrentCsvStoreTests()
    {
        Directory.CreateDirectory(CsvDirectory);
    }

    private string CsvDirectory => Path.Combine(_root.FullPath, "csv");

    private string StatePath => Path.Combine(_root.FullPath, "state", "tcxtool-state.json");

    // ---- Read rules ----

    [Fact]
    public async Task AMissingFileMeansNoCurrentCsvAndLogsNothingAsync()
    {
        var current = await CreateStore().ReadAsync(CancellationToken.None);

        current.Should().Be(CurrentCsv.None);
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task AFileOverTheSizeCapIsIgnoredAsync()
    {
        AddCsv("a.csv");
        var content = "{\"csvFilename\": \"a.csv\"}" + new string(' ', 65_537);
        WriteState(content);

        var current = await CreateStore().ReadAsync(CancellationToken.None);

        current.Should().Be(CurrentCsv.None);
        ShouldHaveLoggedOneWarning("larger than 64 KiB", forbidden: "a.csv");
    }

    [Theory]
    [InlineData("nope", "not valid JSON")]
    [InlineData("[]", "not valid JSON")]
    [InlineData("{}", "no csvFilename string")]
    [InlineData("{\"csvFilename\": 5}", "no csvFilename string")]
    [InlineData("{\"csvFilename\": \"../x.csv\"}", "csvFilename is not a plain CSV name")]
    public async Task AnInvalidFileMeansNoCurrentCsvAndLogsTheReasonOnlyAsync(string content, string reason)
    {
        WriteState(content);

        var current = await CreateStore().ReadAsync(CancellationToken.None);

        current.Should().Be(CurrentCsv.None);
        ShouldHaveLoggedOneWarning(reason, forbidden: content);
    }

    [Fact]
    public async Task AValidNameWhoseFileExistsIsPresentAsync()
    {
        AddCsv("2026-11-01_1.csv");
        WriteState("{\"csvFilename\": \"2026-11-01_1.csv\"}");

        var current = await CreateStore().ReadAsync(CancellationToken.None);

        current.Should().Be(CurrentCsv.Present("2026-11-01_1.csv"));
        current.Status.Should().Be(CurrentCsvStatus.Present);
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task AValidNameWhoseFileIsGoneIsMissingAsync()
    {
        WriteState("{\"csvFilename\": \"2026-11-01_1.csv\"}");

        var current = await CreateStore().ReadAsync(CancellationToken.None);

        current.Should().Be(CurrentCsv.Missing("2026-11-01_1.csv"));
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtraPropertiesAreIgnoredOnReadAsync()
    {
        AddCsv("a.csv");
        WriteState("{\"csvFilename\": \"a.csv\", \"other\": [1, 2]}");

        var current = await CreateStore().ReadAsync(CancellationToken.None);

        current.Should().Be(CurrentCsv.Present("a.csv"));
    }

    [Fact]
    public async Task AStatePathThatIsADirectoryMeansNoCurrentCsvWithNoLogAsync()
    {
        // File.Exists is false for a directory, so this is the "file does not exist" read rule.
        var directory = Path.Combine(_root.FullPath, "a-directory");
        Directory.CreateDirectory(directory);

        var current = await CreateStore(directory).ReadAsync(CancellationToken.None);

        current.Should().Be(CurrentCsv.None);
        _logger.Entries.Should().BeEmpty();
    }

    // ---- Read rules added by feature 006 (statesCsvFilename) ----

    [Fact]
    public async Task NoStatesPropertyGivesNoStatesNameAndNoLogAsync()
    {
        AddCsv("a.csv");
        WriteState("{\"csvFilename\": \"a.csv\"}");

        var current = await CreateStore().ReadAsync(CancellationToken.None);

        current.StatesFileName.Should().BeNull();
        current.Status.Should().Be(CurrentCsvStatus.Present);
        _logger.Entries.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AMatchingStatesNameIsReturnedWithTheUnchangedStatusAsync(bool csvExists)
    {
        if (csvExists)
        {
            AddCsv("a.csv");
        }

        WriteState("{\"csvFilename\": \"a.csv\", \"statesCsvFilename\": \"a.states.csv\"}");

        var current = await CreateStore().ReadAsync(CancellationToken.None);

        current.Should().Be(csvExists ? CurrentCsv.Present("a.csv", "a.states.csv") : CurrentCsv.Missing("a.csv", "a.states.csv"));
        current.StatesFileName.Should().Be("a.states.csv");
        _logger.Entries.Should().BeEmpty();
    }

    [Theory]
    [InlineData("5")]
    [InlineData("null")]
    [InlineData("\"b.states.csv\"")]
    [InlineData("\"A.STATES.CSV\"")]
    [InlineData("\"../a.states.csv\"")]
    [InlineData("\"csv/a.states.csv\"")]
    public async Task AnyOtherStatesValueGivesNoStatesNameWithAWarningAsync(string value)
    {
        AddCsv("a.csv");
        WriteState("{\"csvFilename\": \"a.csv\", \"statesCsvFilename\": " + value + "}");

        var current = await CreateStore().ReadAsync(CancellationToken.None);

        current.Should().Be(CurrentCsv.Present("a.csv"));
        current.StatesFileName.Should().BeNull();
        ShouldHaveLoggedOneWarning("statesCsvFilename does not match csvFilename", forbidden: value);
    }

    [Fact]
    public async Task AStatesNameInCsvFilenameIsNoCurrentCsvAsync()
    {
        AddCsv("a.states.csv");
        WriteState("{\"csvFilename\": \"a.states.csv\"}");

        var current = await CreateStore().ReadAsync(CancellationToken.None);

        current.Should().Be(CurrentCsv.None);
        ShouldHaveLoggedOneWarning("csvFilename is not a plain CSV name", forbidden: "a.states.csv");
    }

    // ---- Select ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ABlankNameIsNoneChosenAndChangesNothingAsync(string? name)
    {
        WriteState(SeedState);

        var result = await CreateStore().SelectAsync(name, CancellationToken.None);

        result.Outcome.Should().Be(CsvSelectionOutcome.NoneChosen);
        result.FileName.Should().BeNull();
        result.IsRecorded.Should().BeFalse();
        File.ReadAllText(StatePath).Should().Be(SeedState);
    }

    [Theory]
    [InlineData("../secrets.csv")]
    [InlineData("/etc/passwd.csv")]
    [InlineData("REPORT.CSV")]
    public async Task AnInvalidNameIsRejectedAndChangesNothingAsync(string name)
    {
        WriteState(SeedState);

        var result = await CreateStore().SelectAsync(name, CancellationToken.None);

        result.Outcome.Should().Be(CsvSelectionOutcome.InvalidName);
        result.FileName.Should().Be(name);
        File.ReadAllText(StatePath).Should().Be(SeedState);
    }

    [Fact]
    public async Task AValidNameThatIsNotOnDiskIsNotFoundAndChangesNothingAsync()
    {
        WriteState(SeedState);

        var result = await CreateStore().SelectAsync("2026-11-01_gone.csv", CancellationToken.None);

        result.Outcome.Should().Be(CsvSelectionOutcome.NotFound);
        File.ReadAllText(StatePath).Should().Be(SeedState);
    }

    [Fact]
    public async Task AValidExistingNameIsRecordedInTheExactFormatAsync()
    {
        AddCsv("2026-11-01_24932322230.csv");

        var result = await CreateStore().SelectAsync("2026-11-01_24932322230.csv", CancellationToken.None);

        result.Outcome.Should().Be(CsvSelectionOutcome.Recorded);
        result.IsRecorded.Should().BeTrue();
        result.FileName.Should().Be("2026-11-01_24932322230.csv");
        File.ReadAllBytes(StatePath).Should().Equal(
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes("{\n  \"csvFilename\": \"2026-11-01_24932322230.csv\"\n}\n"));
        _logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Information && entry.Message == "Current CSV set to 2026-11-01_24932322230.csv");
    }

    [Fact]
    public async Task ChoosingACsvWithAStatesCsvRecordsBothInTheExactFormatAsync()
    {
        AddCsv("2026-11-01_24932322230.csv");
        AddCsv("2026-11-01_24932322230.states.csv");

        var result = await CreateStore().SelectAsync("2026-11-01_24932322230.csv", CancellationToken.None);

        result.Outcome.Should().Be(CsvSelectionOutcome.Recorded);
        File.ReadAllBytes(StatePath).Should().Equal(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(
            "{\n  \"csvFilename\": \"2026-11-01_24932322230.csv\",\n  \"statesCsvFilename\": \"2026-11-01_24932322230.states.csv\"\n}\n"));
        (await CreateStore().ReadAsync(CancellationToken.None)).StatesFileName.Should().Be("2026-11-01_24932322230.states.csv");
    }

    [Fact]
    public async Task ChoosingACsvWhoseStatesCsvIsAnotherCsvsLeavesItOutAsync()
    {
        AddCsv("a.csv");
        AddCsv("b.states.csv");
        WriteState("{\"csvFilename\": \"b.csv\", \"statesCsvFilename\": \"b.states.csv\"}");

        await CreateStore().SelectAsync("a.csv", CancellationToken.None);

        File.ReadAllText(StatePath).Should().Be("{\n  \"csvFilename\": \"a.csv\"\n}\n");
    }

    [Theory]
    [InlineData("a.states.csv")]
    [InlineData("A.STATES.csv")]
    public async Task AStatesNameCannotBeChosenAsync(string name)
    {
        AddCsv(name);
        WriteState(SeedState);

        var result = await CreateStore().SelectAsync(name, CancellationToken.None);

        result.Outcome.Should().Be(CsvSelectionOutcome.InvalidName);
        File.ReadAllText(StatePath).Should().Be(SeedState);
    }

    [Fact]
    public async Task ARecordedChoiceSurvivesARestartAsync()
    {
        AddCsv("a.csv");
        await CreateStore().SelectAsync("a.csv", CancellationToken.None);

        // A new store instance over the same files stands in for an app restart (SC-005).
        var current = await CreateStore().ReadAsync(CancellationToken.None);

        current.Should().Be(CurrentCsv.Present("a.csv"));
    }

    [Fact]
    public async Task ASecondChoiceReplacesTheFirstAndLeavesNoTempFilesAsync()
    {
        AddCsv("a.csv");
        AddCsv("b.csv");
        var store = CreateStore();

        await store.SelectAsync("a.csv", CancellationToken.None);
        await store.SelectAsync("b.csv", CancellationToken.None);

        (await store.ReadAsync(CancellationToken.None)).Should().Be(CurrentCsv.Present("b.csv"));
        Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(StatePath)!).Select(Path.GetFileName)
            .Should().Equal("tcxtool-state.json");
    }

    [Fact]
    public async Task AParentThatIsAFileMakesTheWriteFailAsync()
    {
        AddCsv("a.csv");
        var blocker = Path.Combine(_root.FullPath, "blocker");
        File.WriteAllText(blocker, "not a directory");
        var statePath = Path.Combine(blocker, "tcxtool-state.json");

        var result = await CreateStore(statePath).SelectAsync("a.csv", CancellationToken.None);

        result.Outcome.Should().Be(CsvSelectionOutcome.WriteFailed);
        File.ReadAllText(blocker).Should().Be("not a directory");
        var warning = _logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Warning).Subject;
        warning.Message.Should().Contain(statePath).And.Contain("Exception");
    }

    [Fact]
    public async Task AStatePathThatIsADirectoryMakesTheWriteFailAndLeavesNoTempFileAsync()
    {
        AddCsv("a.csv");
        var statePath = Path.Combine(_root.FullPath, "state-dir");
        Directory.CreateDirectory(statePath);

        var result = await CreateStore(statePath).SelectAsync("a.csv", CancellationToken.None);

        result.Outcome.Should().Be(CsvSelectionOutcome.WriteFailed);
        Directory.EnumerateFiles(_root.FullPath, "*.tmp").Should().BeEmpty();
        Directory.Exists(statePath).Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentChoicesAllRecordAndLeaveOneValidFileAsync()
    {
        AddCsv("a.csv");
        AddCsv("b.csv");
        var store = CreateStore();

        var results = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(i => store.SelectAsync(i % 2 == 0 ? "a.csv" : "b.csv", CancellationToken.None)));

        results.Should().OnlyContain(result => result.Outcome == CsvSelectionOutcome.Recorded);
        using var document = JsonDocument.Parse(File.ReadAllText(StatePath));
        document.RootElement.GetProperty("csvFilename").GetString().Should().BeOneOf("a.csv", "b.csv");
    }

    // ---- Record a conversion (US1, research R8) ----

    [Fact]
    public async Task AFailedConversionRecordsNothingAsync()
    {
        WriteState(SeedState);

        var result = await CreateStore().RecordConversionAsync(
            ConversionResult.Failure(TcxError.Of(TcxErrorKind.Malformed)), CancellationToken.None);

        result.Should().BeNull();
        File.ReadAllText(StatePath).Should().Be(SeedState);
    }

    [Fact]
    public async Task ASuccessWithNoFilesRecordsNothingAsync()
    {
        WriteState(SeedState);

        var result = await CreateStore().RecordConversionAsync(ConversionResult.Success([]), CancellationToken.None);

        result.Should().BeNull();
        File.ReadAllText(StatePath).Should().Be(SeedState);
    }

    [Fact]
    public async Task ASuccessRecordsTheFirstSavedCsvAsync()
    {
        AddCsv("2026-11-01_071500_x.csv");
        AddCsv("2026-11-01_183005_x.csv");
        AddCsv("2026-11-01_071500_x.states.csv");
        AddCsv("2026-11-01_183005_x.states.csv");
        var conversion = ConversionResult.Success(
        [
            new SavedCsv("2026-11-01_071500_x.csv", 3, "2026-11-01_071500_x.states.csv", 1),
            new SavedCsv("2026-11-01_183005_x.csv", 4, "2026-11-01_183005_x.states.csv", 2),
        ]);

        var result = await CreateStore().RecordConversionAsync(conversion, CancellationToken.None);

        result.Should().Be(new CsvSelectionResult(CsvSelectionOutcome.Recorded, "2026-11-01_071500_x.csv"));
        (await CreateStore().ReadAsync(CancellationToken.None))
            .Should().Be(CurrentCsv.Present("2026-11-01_071500_x.csv", "2026-11-01_071500_x.states.csv"));
        File.ReadAllText(StatePath).Should().Be(
            "{\n  \"csvFilename\": \"2026-11-01_071500_x.csv\",\n  \"statesCsvFilename\": \"2026-11-01_071500_x.states.csv\"\n}\n");
    }

    // ---- List (US2, research R5) ----

    [Fact]
    public void AMissingCsvDirectoryListsNothing()
    {
        Directory.Delete(CsvDirectory);

        CreateStore().ListCsvFiles().Should().BeEmpty();
    }

    [Fact]
    public void OnlyPlainCsvFilesAreListed()
    {
        AddCsv("2026-11-01_a.csv");
        File.WriteAllText(Path.Combine(CsvDirectory, "notes.txt"), "x");
        File.WriteAllText(Path.Combine(CsvDirectory, "REPORT.CSV"), "x");
        Directory.CreateDirectory(Path.Combine(CsvDirectory, "sub.csv"));

        CreateStore().ListCsvFiles().Should().Equal("2026-11-01_a.csv");
    }

    [Fact]
    public void StatesCsvFilesAreNeverListed()
    {
        AddCsv("2026-11-01_a.csv");
        AddCsv("2026-11-01_a.states.csv");
        AddCsv("x.states.csv");
        AddCsv("y.STATES.csv");

        CreateStore().ListCsvFiles().Should().Equal("2026-11-01_a.csv");
    }

    [Fact]
    public void CsvFilesAreListedNewestFirst()
    {
        AddCsv("2026-11-01_b.csv");
        AddCsv("2025-03-02_a.csv");
        AddCsv("2026-11-01_a.csv");

        CreateStore().ListCsvFiles().Should().Equal("2026-11-01_b.csv", "2026-11-01_a.csv", "2025-03-02_a.csv");
    }

    public void Dispose() => _root.Dispose();

    private CurrentCsvStore CreateStore(string? statePath = null) => new(
        Options.Create(new TcxToolOptions { CsvOutputDirectory = CsvDirectory, StateFilePath = statePath ?? StatePath }),
        new HostingEnvironment { ContentRootPath = _root.FullPath },
        _logger);

    private void AddCsv(string name) => File.WriteAllText(Path.Combine(CsvDirectory, name), "header\n");

    private void WriteState(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        File.WriteAllText(StatePath, content);
    }

    private void ShouldHaveLoggedOneWarning(string reason, string forbidden)
    {
        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain(StatePath).And.Contain(reason);
        // The temp path holds a GUID, so only the text around it is checked for the forbidden content.
        entry.Message.Replace(StatePath, string.Empty, StringComparison.Ordinal).Should().NotContain(forbidden);
    }
}
