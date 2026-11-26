using JoakimSoftware.TcxTool.Web.Analysis;

using Microsoft.AspNetCore.Components;

namespace JoakimSoftware.TcxTool.Web.Components.Pages;

/// <summary>
/// The States page: the current CSV's States CSV as a table, or a notice
/// (specs/006-activity-states/contracts/pages.md "States page"). It binds the loader's typed result and
/// never touches the file system itself (constitution Principle III). Static SSR, with no render mode
/// (spec FR-019).
/// </summary>
public partial class StatesPage
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
