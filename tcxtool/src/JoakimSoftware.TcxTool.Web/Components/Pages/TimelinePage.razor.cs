using JoakimSoftware.TcxTool.Web.Analysis;

using Microsoft.AspNetCore.Components;

namespace JoakimSoftware.TcxTool.Web.Components.Pages;

/// <summary>
/// The Timeline page: the current CSV's States CSV as a header card and a drawn timeline, or the
/// States page's notice (specs/007-timeline-page/contracts/pages.md "Timeline page"). It reads only
/// the States CSV, through <see cref="CurrentStatesLoader"/>, and never touches the file system itself
/// (constitution Principle III, spec FR-007). Static SSR, with no render mode.
/// </summary>
public partial class TimelinePage
{
    [Inject]
    private CurrentStatesLoader CurrentStatesLoader { get; set; } = default!;

    /// <summary>The request, available because the page renders with static SSR.</summary>
    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    private CurrentStates? Current { get; set; }

    private CancellationToken RequestAborted => HttpContext?.RequestAborted ?? CancellationToken.None;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() =>
        Current = await CurrentStatesLoader.LoadAsync(RequestAborted);
}
