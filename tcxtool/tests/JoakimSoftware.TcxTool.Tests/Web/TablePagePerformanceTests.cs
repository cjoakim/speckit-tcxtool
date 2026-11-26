using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Renders a 20,000-row synthetic CSV, about the size of the real marathon CSV (spec SC-002, server
/// side; research R4). The rows are made up, so no real coordinates are used.
/// </summary>
public sealed class TablePagePerformanceTests : IDisposable
{
    private const int Rows = 20_000;
    private const string Name = "2026-11-01_synthetic.csv";

    private readonly TcxToolWebFactory _factory = new();

    [Fact]
    public async Task TwentyThousandRowsRenderInUnderFiveSecondsAsync()
    {
        ActivityCsvFixture.Write(Path.Combine(_factory.CsvDirectory, Name), ActivityCsvFixture.Synthetic(Rows));
        ActivityCsvFixture.WriteState(_factory.StateFilePath, Name);
        using var client = _factory.CreateClient();

        // Untimed warm-up: the first request starts the host and JIT-compiles the first render, which
        // SC-002 does not measure (tasks.md T021).
        using (var warmUp = await client.GetAsync(new Uri("/table", UriKind.Relative)))
        {
            await warmUp.Content.ReadAsStringAsync();
        }

        var stopwatch = Stopwatch.StartNew();
        using var response = await client.GetAsync(new Uri("/table", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        stopwatch.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("id=\"trackpoints\"").And.Contain("<td>20000</td>");
        Regex.Count(body, "<tr>").Should().Be(Rows + 1);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    public void Dispose() => _factory.Dispose();
}
