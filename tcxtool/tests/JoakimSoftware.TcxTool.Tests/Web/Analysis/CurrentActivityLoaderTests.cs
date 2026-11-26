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
/// Verifies <see cref="CurrentActivityLoader"/> against specs/005-table-page/data-model.md
/// (<c>CurrentActivityStatus</c>, <c>CurrentActivity</c>) and research.md R2 and R11. Every path is inside
/// a test-owned temp directory (FR-022).
/// </summary>
public sealed class CurrentActivityLoaderTests : IDisposable
{
    private const string Name = "2026-11-01_fixture.csv";
    private const string MarkerLatitude = "12.3456789";
    private const string MarkerLongitude = "-65.4321987";

    private readonly TempDirectory _root = new();
    private readonly ListLogger<CurrentActivityLoader> _logger = new();

    public CurrentActivityLoaderTests()
    {
        Directory.CreateDirectory(CsvDirectory);
    }

    private string CsvDirectory => Path.Combine(_root.FullPath, "csv");

    private string CsvPath => Path.Combine(CsvDirectory, Name);

    private string StatePath => Path.Combine(_root.FullPath, "state", "tcxtool-state.json");

    [Fact]
    public async Task NoStateFileMeansNoCurrentCsvAndLogsNothingAsync()
    {
        var activity = await LoadAsync();

        activity.Status.Should().Be(CurrentActivityStatus.NoCurrentCsv);
        activity.FileName.Should().BeNull();
        ShouldHoldTheInvariants(activity);
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task AStateNamingAnAbsentFileIsMissingAsync()
    {
        ActivityCsvFixture.WriteState(StatePath, Name);

        var activity = await LoadAsync();

        activity.Should().Be(CurrentActivity.Missing(Name));
        ShouldHoldTheInvariants(activity);
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task AFileDeletedAfterTheStatusCheckIsMissingAsync()
    {
        // A dangling link passes File.Exists, so the store says Present, but the open fails (research.md "Facts").
        File.CreateSymbolicLink(CsvPath, Path.Combine(_root.FullPath, "does-not-exist.csv"));
        ActivityCsvFixture.WriteState(StatePath, Name);

        var activity = await LoadAsync();

        activity.Should().Be(CurrentActivity.Missing(Name));
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task ALockedFileIsUnreadableAndLogsOnlyTheNameAndExceptionTypeAsync()
    {
        WriteCsv(Marked(1), Marked(2));
        ActivityCsvFixture.WriteState(StatePath, Name);

        CurrentActivity activity;
        using (new FileStream(CsvPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            activity = await LoadAsync();
        }

        activity.Should().Be(CurrentActivity.Unreadable(Name));
        ShouldHoldTheInvariants(activity);
        var entry = ShouldHaveLoggedOneWarning();
        entry.Should().Contain(Name).And.Contain(nameof(IOException)).And.NotContain("being used by another process");
    }

    [Fact]
    public async Task AWrongHeaderIsWrongFormatAtLineOneAsync()
    {
        ActivityCsvFixture.WriteRaw(CsvPath, "hello\n");
        ActivityCsvFixture.WriteState(StatePath, Name);

        var activity = await LoadAsync();

        activity.Should().Be(CurrentActivity.WrongFormat(Name));
        ShouldHoldTheInvariants(activity);
        ShouldHaveLoggedOneWarning().Should().Contain(Name).And.Contain(nameof(ActivityCsvErrorKind.HeaderMismatch)).And.Contain("line 1");
    }

    [Fact]
    public async Task AWrongFieldCountIsWrongFormatWithNoRowsAsync()
    {
        WriteCsv(Marked(1), Marked(2), string.Join('|', Enumerable.Repeat("x", 18)));
        ActivityCsvFixture.WriteState(StatePath, Name);

        var activity = await LoadAsync();

        activity.Status.Should().Be(CurrentActivityStatus.WrongFormat);
        activity.Rows.Should().BeEmpty();
        ShouldHoldTheInvariants(activity);
        ShouldHaveLoggedOneWarning().Should().Contain(Name).And.Contain(nameof(ActivityCsvErrorKind.FieldCount)).And.Contain("line 4");
    }

    [Fact]
    public async Task AHeaderOnlyFileHasNoTrackpointsAndAnEmptySummaryAsync()
    {
        WriteCsv();
        ActivityCsvFixture.WriteState(StatePath, Name);

        var activity = await LoadAsync();

        activity.Status.Should().Be(CurrentActivityStatus.NoTrackpoints);
        activity.FileName.Should().Be(Name);
        activity.Summary.Should().Be(ActivitySummary.From(Name, null));
        activity.Summary!.ActivityId.Should().BeEmpty();
        activity.Summary.Date.Should().BeEmpty();
        activity.Summary.DeviceName.Should().BeEmpty();
        activity.Summary.DeviceId.Should().BeEmpty();
        activity.Summary.Sport.Should().BeEmpty();
        ShouldHoldTheInvariants(activity);
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task ThreeLinesAreLoadedInFileOrderWithTheSummaryFromTheFirstAsync()
    {
        WriteCsv(
            ActivityCsvFixture.Line(activityId: "run-a", date: "2026-10-31", deviceName: "Edge 1040", deviceId: "555", sport: "Biking", trackpointNum: "1"),
            ActivityCsvFixture.Line(activityId: "other", trackpointNum: "2"),
            ActivityCsvFixture.Line(activityId: "other", trackpointNum: "3"));
        ActivityCsvFixture.WriteState(StatePath, Name);

        var activity = await LoadAsync();

        activity.Status.Should().Be(CurrentActivityStatus.Loaded);
        activity.FileName.Should().Be(Name);
        activity.Rows.Select(row => row.TrackpointNum).Should().Equal("1", "2", "3");
        activity.Summary.Should().Be(new ActivitySummary(Name, "run-a", "2026-10-31", "Edge 1040", "555", "Biking"));
        ShouldHoldTheInvariants(activity);
        _logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadingWritesNothingAsync()
    {
        WriteCsv(Marked(1), Marked(2));
        ActivityCsvFixture.WriteState(StatePath, Name);
        var before = Snapshot();

        var activity = await LoadAsync();

        activity.Status.Should().Be(CurrentActivityStatus.Loaded);
        Snapshot().Should().BeEquivalentTo(before, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task LogsNeverCarryFieldValuesAsync()
    {
        WriteCsv(Marked(1), Marked(2), "too|few");
        ActivityCsvFixture.WriteState(StatePath, Name);
        await LoadAsync();

        WriteCsv(Marked(1));
        using (new FileStream(CsvPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            await LoadAsync();
        }

        _logger.Entries.Should().HaveCount(2);
        _logger.Entries.Should().OnlyContain(entry => !entry.Message.Contains(MarkerLatitude) && !entry.Message.Contains(MarkerLongitude));
    }

    public void Dispose() => _root.Dispose();

    private static string Marked(int trackpointNum) =>
        ActivityCsvFixture.Line(trackpointNum: trackpointNum.ToString(System.Globalization.CultureInfo.InvariantCulture), latitude: MarkerLatitude, longitude: MarkerLongitude);

    private static void ShouldHoldTheInvariants(CurrentActivity activity)
    {
        (activity.FileName is null).Should().Be(activity.Status == CurrentActivityStatus.NoCurrentCsv);
        (activity.Summary is not null).Should().Be(activity.Status is CurrentActivityStatus.NoTrackpoints or CurrentActivityStatus.Loaded);
        (activity.Rows.Count > 0).Should().Be(activity.Status == CurrentActivityStatus.Loaded);
    }

    private Task<CurrentActivity> LoadAsync()
    {
        var options = Options.Create(new TcxToolOptions { CsvOutputDirectory = CsvDirectory, StateFilePath = StatePath });
        var environment = new HostingEnvironment { ContentRootPath = _root.FullPath };
        var store = new CurrentCsvStore(options, environment, NullLogger<CurrentCsvStore>.Instance);
        return new CurrentActivityLoader(store, options, environment, _logger).LoadAsync(CancellationToken.None);
    }

    private void WriteCsv(params string[] lines) => ActivityCsvFixture.Write(CsvPath, lines);

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
