using JoakimSoftware.TcxTool.Parser.Conversion;
using JoakimSoftware.TcxTool.Tests.Support;
using JoakimSoftware.TcxTool.Web.Conversion;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies <see cref="CsvConversionService"/>: size and empty-file checks before reading, and
/// failure logging without file content (spec FR-002, FR-018; data-model.md "Web types").
/// </summary>
public sealed class CsvConversionServiceTests : IDisposable
{
    private const long Limit = 1000;

    private readonly TempDirectory _output = new();
    private readonly ListLogger<CsvConversionService> _logger = new();
    private readonly CsvConversionService _service;

    public CsvConversionServiceTests()
    {
        var options = Options.Create(new TcxToolOptions { MaxUploadBytes = Limit, CsvOutputDirectory = _output.FullPath });
        _service = new CsvConversionService(options, new HostingEnvironment { ContentRootPath = Path.GetTempPath() }, _logger);
    }

    [Fact]
    public async Task AFileOverTheLimitIsRejectedWithoutReadingItAsync()
    {
        var file = new FormFile(new UnreadableStream(), 0, Limit + 1, "TcxFile", "big.tcx");

        var result = await _service.ConvertAsync(file, CancellationToken.None);

        result.Error!.Kind.Should().Be(TcxErrorKind.TooLarge);
        result.Error.Message.Should().Be(TcxErrorMessages.For(TcxErrorKind.TooLarge, Limit));
    }

    [Fact]
    public async Task AFileExactlyAtTheLimitIsAcceptedAsync()
    {
        using var content = new MemoryStream(new byte[Limit]);
        var file = new FormFile(content, 0, Limit, "TcxFile", "limit.tcx");

        var result = await _service.ConvertAsync(file, CancellationToken.None);

        result.Error!.Kind.Should().NotBe(TcxErrorKind.TooLarge);
    }

    [Fact]
    public async Task NoFileOrAnEmptyFileAsksForAFileAsync()
    {
        var empty = new FormFile(new UnreadableStream(), 0, 0, "TcxFile", "");

        foreach (var file in new IFormFile?[] { null, empty })
        {
            var result = await _service.ConvertAsync(file, CancellationToken.None);

            result.Error!.Kind.Should().Be(TcxErrorKind.NoFile);
            result.Error.Message.Should().Be("Choose a .tcx file to convert.");
        }
    }

    [Fact]
    public async Task AFailureLogsTheFileNameAndKindButNoContentAsync()
    {
        var bytes = File.ReadAllBytes(Fixture.Path("invalid-number.tcx"));
        using var content = new MemoryStream(bytes);
        var file = new FormFile(content, 0, bytes.Length, "TcxFile", "invalid-number.tcx");
        var service = new CsvConversionService(
            Options.Create(new TcxToolOptions { CsvOutputDirectory = _output.FullPath }),
            new HostingEnvironment { ContentRootPath = Path.GetTempPath() },
            _logger);

        await service.ConvertAsync(file, CancellationToken.None);

        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Message.Should().Contain("invalid-number.tcx").And.Contain(nameof(TcxErrorKind.InvalidValue));
        entry.Message.Should().NotContainAny("abc", "Trackpoint", "2026-11-01T09");
    }

    public void Dispose() => _output.Dispose();

    private sealed class UnreadableStream : MemoryStream
    {
        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("The stream must not be read.");

        public override int Read(Span<byte> buffer) => throw new InvalidOperationException("The stream must not be read.");

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The stream must not be read.");
    }
}
