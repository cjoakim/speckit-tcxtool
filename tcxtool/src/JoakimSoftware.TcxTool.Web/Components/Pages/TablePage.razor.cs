using JoakimSoftware.TcxTool.Web.Analysis;

using Microsoft.AspNetCore.Components;

namespace JoakimSoftware.TcxTool.Web.Components.Pages;

/// <summary>
/// The Table page: the current CSV's activity card and its trackpoints
/// (specs/005-table-page/contracts/pages.md). It binds the loader's typed result and never touches the
/// file system itself (constitution Principle III). Static SSR, with no render mode (spec FR-020).
/// </summary>
public partial class TablePage
{
    [Inject]
    private CurrentActivityLoader CurrentActivityLoader { get; set; } = default!;

    /// <summary>The request, available because the page renders with static SSR.</summary>
    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    private CurrentActivity? Current { get; set; }

    private CancellationToken RequestAborted => HttpContext?.RequestAborted ?? CancellationToken.None;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() =>
        Current = await CurrentActivityLoader.LoadAsync(RequestAborted);

    private static string Message(CurrentActivityStatus status) => CurrentActivityMessages.For(status);
}
