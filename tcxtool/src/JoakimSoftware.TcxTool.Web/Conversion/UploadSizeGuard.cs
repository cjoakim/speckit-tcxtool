using Microsoft.Extensions.Options;

namespace JoakimSoftware.TcxTool.Web.Conversion;

/// <summary>
/// Redirects a <c>POST /convert</c> whose declared body is over the request ceiling to
/// <c>/convert?error=too-large</c> without reading the body. It must run before routing and
/// antiforgery, which would otherwise fail while reading the oversized form (research.md R2).
/// </summary>
public sealed class UploadSizeGuard(RequestDelegate next, IOptions<TcxToolOptions> options)
{
    /// <summary>The query that tells the convert page to show the TooLarge message.</summary>
    internal const string TooLargeLocation = "/convert?error=too-large";

    private readonly RequestDelegate _next = next;
    private readonly long _ceiling = RequestCeiling(options.Value);

    /// <summary>The largest request body accepted: the upload limit plus 1 MiB for the rest of the form.</summary>
    /// <param name="options">The TcxTool options.</param>
    /// <returns>The ceiling in bytes.</returns>
    internal static long RequestCeiling(TcxToolOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.MaxUploadBytes + (1024 * 1024);
    }

    /// <summary>Redirects an oversized upload, or passes the request on.</summary>
    /// <param name="context">The request.</param>
    /// <returns>A task that completes when the request is handled.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.Equals("/convert", StringComparison.OrdinalIgnoreCase)
            && context.Request.ContentLength > _ceiling)
        {
            context.Response.StatusCode = StatusCodes.Status303SeeOther;
            context.Response.Headers.Location = TooLargeLocation;
            return Task.CompletedTask;
        }

        return _next(context);
    }
}
