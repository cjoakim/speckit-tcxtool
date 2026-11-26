using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Tests.Support;
using JoakimSoftware.TcxTool.Web.Analysis;
using JoakimSoftware.TcxTool.Web.Conversion;
using JoakimSoftware.TcxTool.Web.State;

using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JoakimSoftware.TcxTool.Tests.Web.Analysis;

/// <summary>
/// Verifies <see cref="CurrentStatesLoader"/> against specs/006-activity-states/contracts/pages.md
/// "States page" and data-model.md (<c>CurrentStates.Status</c>, <c>CurrentStates.CsvFileName</c>,
/// <c>CurrentStates.StatesFileName</c>, <c>CurrentStates.Rows</c>). Every path is inside a test-owned
/// temp directory (FR-026).
/// </summary>
public sealed class CurrentStatesLoaderTests : IDisposable
{
    private const string Name = "2026-11-01_fixture.csv";
    private const string StatesName = "2026-11-01_fixture.states.csv";
    private const string MarkerLatitude = "12.3456789";
    private const string MarkerLongitude = "-65.4321987";

    private readonly TempDirectory _root = new();
    private readonly ListLogger<CurrentStatesLoader> _logger = new();

    public CurrentStatesLoaderTests()
    {
        Directory.CreateDirectory(CsvDirectory);
    }

    private string CsvDirectory => Path.Combine(_root.FullPath, "csv");

    private string CsvPath => Path.Combine(CsvDirectory, Name);

    private string StatesPath => Path.Combine(CsvDirectory, StatesName);

    private string StatePath => Path.Combine(_root.FullPath, "state", "tcxtool-state.json");

    [Fact]
    public async Task NoStateFileMeansNoCurrentCsvAsync()
    {
        var states = await LoadAsync();

        states.Status.Should().Be(CurrentStatesStatus.NoCurrentCsv);
        states.CsvFileName.Should().BeNull();
        states.StatesFileName.Should().BeNull();
        ShouldHoldTheInvariant(states);
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task NoStatesPropertyIsNoStatesCsvWithTheCsvNameAsync()
    {
        WriteActivityCsv();
        ActivityCsvFixture.WriteState(StatePath, Name);

        var states = await LoadAsync();

        states.Should().Be(CurrentStates.NoStatesCsv(Name));
        states.CsvFileName.Should().Be(Name);
        states.StatesFileName.Should().BeNull();
        ShouldHoldTheInvariant(states);
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task AMismatchedStatesNameIsNoStatesCsvAsync()
    {
        WriteActivityCsv();
        StatesCsvFixture.Write(Path.Combine(CsvDirectory, "other.states.csv"), [StatesCsvFixture.Line()]);
        ActivityCsvFixture.WriteState(StatePath, Name, "other.states.csv");

        var states = await LoadAsync();

        states.Should().Be(CurrentStates.NoStatesCsv(Name));
    }

    [Fact]
    public async Task AnAbsentStatesCsvIsMissingAsync()
    {
        WriteActivityCsv();
        ActivityCsvFixture.WriteState(StatePath, Name, StatesName);

        var states = await LoadAsync();

        states.Should().Be(CurrentStates.Missing(Name, StatesName));
        ShouldHoldTheInvariant(states);
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task ADanglingLinkIsMissingAsync()
    {
        WriteActivityCsv();
        File.CreateSymbolicLink(StatesPath, Path.Combine(_root.FullPath, "does-not-exist.states.csv"));
        ActivityCsvFixture.WriteState(StatePath, Name, StatesName);

        var states = await LoadAsync();

        states.Should().Be(CurrentStates.Missing(Name, StatesName));
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task ALockedStatesCsvIsUnreadableAndLogsOnlyTheNameAndExceptionTypeAsync()
    {
        WriteActivityCsv();
        StatesCsvFixture.Write(StatesPath, [Marked("1")]);
        ActivityCsvFixture.WriteState(StatePath, Name, StatesName);

        CurrentStates states;
        using (new FileStream(StatesPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            states = await LoadAsync();
        }

        states.Should().Be(CurrentStates.Unreadable(Name, StatesName));
        ShouldHoldTheInvariant(states);
        ShouldHaveLoggedOneWarning().Should().Contain(StatesName).And.Contain(nameof(IOException)).And.NotContain("being used by another process");
    }

    [Fact]
    public async Task ABadHeaderIsWrongFormatAtLineOneAsync()
    {
        WriteActivityCsv();
        ActivityCsvFixture.WriteRaw(StatesPath, "hello\n");
        ActivityCsvFixture.WriteState(StatePath, Name, StatesName);

        var states = await LoadAsync();

        states.Should().Be(CurrentStates.WrongFormat(Name, StatesName));
        ShouldHoldTheInvariant(states);
        ShouldHaveLoggedOneWarning().Should().Contain(StatesName).And.Contain(nameof(ActivityCsvErrorKind.HeaderMismatch)).And.Contain("line 1");
    }

    // A States CSV written by feature 006, with its 13-field header, is the wrong format (feature 007
    // FR-006, research.md R3). The values are made up.
    [Fact]
    public async Task AFeature006StatesCsvIsWrongFormatAsync()
    {
        WriteActivityCsv();
        ActivityCsvFixture.WriteRaw(StatesPath,
            "ActivityId|State|StateEndDate|StateEndTrackpointNum|StateElapsedSeconds|ActivityElapsedTime|"
            + "AverageLatitude|AverageLongitude|AverageHeartRateBpm|AverageRunCadence|DistanceMiles|"
            + "AverageAltitudeMeters|AverageAltitudeFeet\n"
            + "fixture-run|walking|2026-11-01T06:00:09.000Z|10|9|00:00:09|1.5|2.5|120|140|0.01|10|32.8\n");
        ActivityCsvFixture.WriteState(StatePath, Name, StatesName);

        var states = await LoadAsync();

        states.Should().Be(CurrentStates.WrongFormat(Name, StatesName));
        ShouldHaveLoggedOneWarning().Should().Contain(StatesName).And.Contain(nameof(ActivityCsvErrorKind.HeaderMismatch)).And.Contain("line 1");
    }

    [Fact]
    public async Task AHeaderOnlyFileHasNoStateSetsAsync()
    {
        WriteActivityCsv();
        StatesCsvFixture.Write(StatesPath, []);
        ActivityCsvFixture.WriteState(StatePath, Name, StatesName);

        var states = await LoadAsync();

        states.Should().Be(CurrentStates.Read(Name, StatesName, []));
        states.Status.Should().Be(CurrentStatesStatus.NoStateSets);
        ShouldHoldTheInvariant(states);
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task ThreeLinesAreLoadedInFileOrderAsync()
    {
        WriteActivityCsv();
        StatesCsvFixture.Write(StatesPath,
            [StatesCsvFixture.Line(stateEndTrackpointNum: "3"), StatesCsvFixture.Line(stateEndTrackpointNum: "7"), StatesCsvFixture.Line(stateEndTrackpointNum: "9")]);
        ActivityCsvFixture.WriteState(StatePath, Name, StatesName);

        var states = await LoadAsync();

        states.Status.Should().Be(CurrentStatesStatus.Loaded);
        states.CsvFileName.Should().Be(Name);
        states.StatesFileName.Should().Be(StatesName);
        states.Rows.Select(row => row.StateEndTrackpointNum).Should().Equal("3", "7", "9");
        ShouldHoldTheInvariant(states);
        _logger.Entries.Should().BeEmpty();
    }

    // contracts/pages.md: a current CSV whose own file is missing does not stop the States page.
    [Fact]
    public async Task AMissingActivityCsvStillLoadsTheStatesCsvAsync()
    {
        StatesCsvFixture.Write(StatesPath, [StatesCsvFixture.Line()]);
        ActivityCsvFixture.WriteState(StatePath, Name, StatesName);

        var states = await LoadAsync();

        states.Status.Should().Be(CurrentStatesStatus.Loaded);
        states.Rows.Should().ContainSingle();
    }

    [Fact]
    public async Task LoadingWritesNothingAsync()
    {
        WriteActivityCsv();
        StatesCsvFixture.Write(StatesPath, [Marked("1"), Marked("2")]);
        ActivityCsvFixture.WriteState(StatePath, Name, StatesName);
        var before = Snapshot();

        await LoadAsync();
        var states = await LoadAsync();

        states.Status.Should().Be(CurrentStatesStatus.Loaded);
        Snapshot().Should().BeEquivalentTo(before, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task LogsNeverCarryFieldValuesAsync()
    {
        WriteActivityCsv();
        StatesCsvFixture.Write(StatesPath, [Marked("1"), "too|few"]);
        ActivityCsvFixture.WriteState(StatePath, Name, StatesName);
        await LoadAsync();

        StatesCsvFixture.Write(StatesPath, [Marked("1")]);
        using (new FileStream(StatesPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            await LoadAsync();
        }

        _logger.Entries.Should().HaveCount(2);
        _logger.Entries.Should().OnlyContain(entry => !entry.Message.Contains(MarkerLatitude) && !entry.Message.Contains(MarkerLongitude));
    }

    public void Dispose() => _root.Dispose();

    private static string Marked(string trackpointNum) =>
        StatesCsvFixture.Line(stateEndTrackpointNum: trackpointNum, averageLatitude: MarkerLatitude, averageLongitude: MarkerLongitude);

    private static void ShouldHoldTheInvariant(CurrentStates states) =>
        (states.Rows.Count > 0).Should().Be(states.Status == CurrentStatesStatus.Loaded);

    private void WriteActivityCsv() => ActivityCsvFixture.Write(CsvPath, [ActivityCsvFixture.Line()]);

    private Task<CurrentStates> LoadAsync()
    {
        var options = Options.Create(new TcxToolOptions { CsvOutputDirectory = CsvDirectory, StateFilePath = StatePath });
        var environment = new HostingEnvironment { ContentRootPath = _root.FullPath };
        var store = new CurrentCsvStore(options, environment, NullLogger<CurrentCsvStore>.Instance);
        return new CurrentStatesLoader(store, options, environment, _logger).LoadAsync(CancellationToken.None);
    }

    private string ShouldHaveLoggedOneWarning()
    {
        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Warning);
        return entry.Message;
    }

    private List<(string Path, byte[] Bytes, DateTime Written)> Snapshot() =>
        [.. Directory.EnumerateFiles(_root.FullPath, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(path => (path, File.ReadAllBytes(path), File.GetLastWriteTimeUtc(path)))];
}
