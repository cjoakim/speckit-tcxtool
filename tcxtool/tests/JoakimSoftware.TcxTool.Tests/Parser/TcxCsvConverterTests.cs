using JoakimSoftware.TcxTool.Parser.Conversion;
using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Parser;

/// <summary>
/// Verifies <see cref="TcxCsvConverter"/> writing to a real directory (feature 002 spec FR-015, SC-006,
/// research R8), with an activity CSV and a States CSV per activity (feature 006 FR-007, FR-012,
/// research R6, R7).
/// </summary>
public sealed class TcxCsvConverterTests : IDisposable
{
    private readonly TempDirectory _output = new();

    [Fact]
    public async Task WritesTheGoldenCsvForOneActivityAsync()
    {
        var result = await ConvertAsync("run-two-laps.tcx", "run-two-laps.tcx", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Files.Should().Equal(new SavedCsv("2026-11-01_run-two-laps.csv", 10, "2026-11-01_run-two-laps.states.csv", 1));
        _output.FileNames().Should().Equal("2026-11-01_run-two-laps.csv", "2026-11-01_run-two-laps.states.csv");
        ReadOutput("2026-11-01_run-two-laps.csv").Should().Equal(ExpectedBytes("run-two-laps.csv"));
        ReadOutput("2026-11-01_run-two-laps.states.csv").Should().Equal(ExpectedBytes("run-two-laps.states.csv"));
    }

    [Fact]
    public async Task WritesTheGoldenNineteenFieldCsvForTheStatesFixtureAsync()
    {
        var result = await ConvertAsync("run-states.tcx", "run-states.tcx", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Files.Should().Equal(new SavedCsv("2026-11-01_run-states.csv", 14, "2026-11-01_run-states.states.csv", 7));
        ReadOutput("2026-11-01_run-states.csv").Should().Equal(ExpectedBytes("run-states.csv"));
        ReadOutput("2026-11-01_run-states.states.csv").Should().Equal(ExpectedBytes("run-states.states.csv"));
    }

    [Fact]
    public async Task WritesUtf8WithoutByteOrderMarkAsync()
    {
        await ConvertAsync("run-two-laps.tcx", "run-two-laps.tcx", CancellationToken.None);

        ReadOutput("2026-11-01_run-two-laps.csv").Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
    }

    [Fact]
    public async Task WritesOneFilePerActivityAsync()
    {
        var result = await ConvertAsync("multi-activity.tcx", "multi-activity.tcx", CancellationToken.None);

        result.Files.Select(file => file.FileName)
            .Should().Equal("2026-11-01_071500_multi-activity.csv", "2026-11-01_183005_multi-activity.csv");
        result.Files.Select(file => file.StatesFileName)
            .Should().Equal("2026-11-01_071500_multi-activity.states.csv", "2026-11-01_183005_multi-activity.states.csv");
        _output.FileNames().Should().HaveCount(4);
        foreach (var file in result.Files)
        {
            ReadOutput(file.FileName).Should().Equal(ExpectedBytes(file.FileName));
            ReadOutput(file.StatesFileName).Should().Equal(ExpectedBytes(file.StatesFileName));
        }
    }

    [Fact]
    public async Task ConvertingTwiceGivesIdenticalBytesAndReplacesTheFileAsync()
    {
        await ConvertAsync("run-two-laps.tcx", "run-two-laps.tcx", CancellationToken.None);
        var first = ReadOutput("2026-11-01_run-two-laps.csv");
        var firstStates = ReadOutput("2026-11-01_run-two-laps.states.csv");

        var second = await ConvertAsync("run-two-laps.tcx", "run-two-laps.tcx", CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        ReadOutput("2026-11-01_run-two-laps.csv").Should().Equal(first);
        ReadOutput("2026-11-01_run-two-laps.states.csv").Should().Equal(firstStates);
        _output.FileNames().Should().Equal("2026-11-01_run-two-laps.csv", "2026-11-01_run-two-laps.states.csv");
    }

    [Fact]
    public async Task CreatesAMissingOutputDirectoryAsync()
    {
        var nested = Path.Combine(_output.FullPath, "data", "csv");

        await using var stream = Fixture.OpenRead("run-two-laps.tcx");
        var result = await TcxCsvConverter.ConvertAsync(stream, "run-two-laps.tcx", nested, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        File.Exists(Path.Combine(nested, "2026-11-01_run-two-laps.csv")).Should().BeTrue();
        File.Exists(Path.Combine(nested, "2026-11-01_run-two-laps.states.csv")).Should().BeTrue();
    }

    [Fact]
    public async Task LeavesNoTemporaryFilesAsync()
    {
        await ConvertAsync("multi-activity.tcx", "multi-activity.tcx", CancellationToken.None);

        _output.FileNames().Should().NotContain(name => name.EndsWith(".tmp", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnActivityWithoutTrackpointsSavesAHeaderOnlyFileAsync()
    {
        var result = await ConvertAsync("no-trackpoints.tcx", "no-trackpoints.tcx", CancellationToken.None);

        result.Files.Should().Equal(new SavedCsv("2026-11-01_no-trackpoints.csv", 0, "2026-11-01_no-trackpoints.states.csv", 0));
        ReadOutput("2026-11-01_no-trackpoints.csv").Should().Equal(ExpectedBytes("no-trackpoints.csv"));
        ReadOutput("2026-11-01_no-trackpoints.states.csv").Should().Equal(ExpectedBytes("no-trackpoints.states.csv"));
    }

    // Both files are written or neither is (research R6): a States CSV that cannot be moved into place
    // leaves no new activity CSV and no temp file.
    [Fact]
    public async Task AStatesCsvThatCannotBeWrittenLeavesNeitherFileAsync()
    {
        var keep = SeedKeepFile();
        var blocked = Path.Combine(_output.FullPath, "2026-11-01_run-two-laps.states.csv");
        Directory.CreateDirectory(blocked);

        var result = await ConvertAsync("run-two-laps.tcx", "run-two-laps.tcx", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(TcxErrorKind.OutputFailed);
        _output.FileNames().Should().Equal("keep.csv");
        File.ReadAllText(keep).Should().Be(KeepContent);
        Directory.Exists(blocked).Should().BeTrue();
    }

    [Theory]
    [InlineData("malformed.tcx", "malformed.tcx", TcxErrorKind.Malformed)]
    [InlineData("not-tcx.tcx", "not-tcx.tcx", TcxErrorKind.NotTcx)]
    [InlineData("dtd.tcx", "dtd.tcx", TcxErrorKind.ProhibitedContent)]
    [InlineData("invalid-number.tcx", "invalid-number.tcx", TcxErrorKind.InvalidValue)]
    [InlineData("invalid-number-nan.tcx", "invalid-number-nan.tcx", TcxErrorKind.InvalidValue)]
    [InlineData("invalid-time.tcx", "invalid-time.tcx", TcxErrorKind.InvalidValue)]
    [InlineData("missing-id.tcx", "missing-id.tcx", TcxErrorKind.InvalidActivityId)]
    [InlineData("duplicate-start.tcx", "duplicate-start.tcx", TcxErrorKind.DuplicateActivityStart)]
    [InlineData("run-two-laps.tcx", ".tcx", TcxErrorKind.EmptyFileName)]
    [InlineData("run-two-laps.tcx", "notes.txt", TcxErrorKind.NotTcxFileName)]
    public async Task AFailureLeavesTheOutputDirectoryUnchangedAsync(string fixture, string uploadedName, TcxErrorKind expected)
    {
        var keep = SeedKeepFile();

        var result = await ConvertAsync(fixture, uploadedName, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Kind.Should().Be(expected);
        result.Files.Should().BeEmpty();
        _output.FileNames().Should().Equal("keep.csv");
        File.ReadAllText(keep).Should().Be(KeepContent);
    }

    [Fact]
    public async Task AnOutputDirectoryThatIsAFileGivesOutputFailedAsync()
    {
        var keep = SeedKeepFile();

        await using var stream = Fixture.OpenRead("run-two-laps.tcx");
        var result = await TcxCsvConverter.ConvertAsync(stream, "run-two-laps.tcx", keep, CancellationToken.None);

        result.Error!.Kind.Should().Be(TcxErrorKind.OutputFailed);
        _output.FileNames().Should().Equal("keep.csv");
        File.ReadAllText(keep).Should().Be(KeepContent);
    }

    public void Dispose() => _output.Dispose();

    private const string KeepContent = "existing|file\n";

    private string SeedKeepFile()
    {
        Directory.CreateDirectory(_output.FullPath);
        var keep = Path.Combine(_output.FullPath, "keep.csv");
        File.WriteAllText(keep, KeepContent);
        return keep;
    }

    internal async Task<ConversionResult> ConvertAsync(string fixture, string uploadedName, CancellationToken cancellationToken)
    {
        await using var stream = Fixture.OpenRead(fixture);
        return await TcxCsvConverter.ConvertAsync(stream, uploadedName, _output.FullPath, cancellationToken);
    }

    private byte[] ReadOutput(string name) => File.ReadAllBytes(Path.Combine(_output.FullPath, name));

    private static byte[] ExpectedBytes(string name) => File.ReadAllBytes(Fixture.ExpectedPath(name));
}
