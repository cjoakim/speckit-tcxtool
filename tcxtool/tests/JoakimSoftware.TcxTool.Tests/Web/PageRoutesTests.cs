using System.Net;

using JoakimSoftware.TcxTool.Tests.Support;

using Microsoft.AspNetCore.Components;

namespace JoakimSoftware.TcxTool.Tests.Web;

/// <summary>
/// Verifies the app's whole set of routes, and what an address with no route gets
/// (feature 008 contracts/pages.md "Routes" and "Unknown addresses").
/// </summary>
public sealed class PageRoutesTests : IDisposable
{
    private readonly TcxToolWebFactory _factory = new();

    // A page added or removed without updating this list fails here, like the navbar list in
    // MainLayoutTests.
    [Fact]
    public void TheAppRoutesExactlyItsPages()
    {
        var templates = typeof(Program).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes(typeof(RouteAttribute), inherit: false))
            .Cast<RouteAttribute>()
            .Select(route => route.Template);

        templates.Should().BeEquivalentTo(
            "/", "/convert", "/table", "/states", "/timeline", "/about", "/not-found", "/Error");
    }

    [Fact]
    public async Task AnUnknownAddressGetsTheNotFoundPageInTheLayoutAsync()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/no-such-page", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.Should().Contain(">Not Found</h1>").And.Contain("navbar");
    }

    public void Dispose() => _factory.Dispose();
}
