using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

using JoakimSoftware.TcxTool.Tests.Support;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Renders a 1,000-row synthetic States CSV, about twice the real marathon's 541 StateSets
/// (spec SC-004, server side). The rows are made up, so no real coordinates are used.
/// </summary>
public sealed class StatesPagePerformanceTests : IDisposable
{
    private const int Rows = 1_000;
    private const string Name = "2026-11-01_synthetic.csv";
    private const string StatesName = "2026-11-01_synthetic.states.csv";

    private readonly TcxToolWebFactory _factory = new();

    [Fact]
    public async Task OneThousandStateSetsRenderInUnderFiveSecondsAsync()
    {
        StatesCsvFixture.Write(Path.Combine(_factory.CsvDirectory, StatesName), StatesCsvFixture.Synthetic(Rows));
        ActivityCsvFixture.WriteState(_factory.StateFilePath, Name, StatesName);
        using var client = _factory.CreateClient();

        // Untimed warm-up: the first request starts the host and JIT-compiles the first render.
        using (var warmUp = await client.GetAsync(new Uri("/states", UriKind.Relative)))
        {
            await warmUp.Content.ReadAsStringAsync();
        }

        var stopwatch = Stopwatch.StartNew();
        using var response = await client.GetAsync(new Uri("/states", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        stopwatch.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("id=\"states\"").And.Contain("<td>10000</td>");
        Regex.Count(body, "<tr>").Should().Be(Rows + 1);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    public void Dispose() => _factory.Dispose();
}
