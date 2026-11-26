using JoakimSoftware.TcxTool.Web.State;

using Microsoft.AspNetCore.Components;

namespace JoakimSoftware.TcxTool.Web.Components.Pages;

/// <summary>
/// The home page. Its Select CSV File card lists the CSVs and records the chosen one as current
/// (specs/004-about-and-csv-state/contracts/pages.md "Home"). It binds the store's typed results and
/// never touches the file system itself.
/// </summary>
public partial class Home
{
    private const string FormName = "csv-select";

    [Inject]
    private CurrentCsvStore CurrentCsvStore { get; set; } = default!;

    /// <summary>The request, available because the page renders with static SSR.</summary>
    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    /// <summary>The chosen CSV's file name, bound from the <c>csv-select</c> form.</summary>
    [SupplyParameterFromForm(FormName = FormName)]
    public string? CsvFilename { get; set; }

    private CurrentCsv? Current { get; set; }

    private IReadOnlyList<string> Files { get; set; } = [];

    private CsvSelectionResult? Selection { get; set; }

    private CancellationToken RequestAborted => HttpContext?.RequestAborted ?? CancellationToken.None;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Files = CurrentCsvStore.ListCsvFiles();
        Current = await CurrentCsvStore.ReadAsync(RequestAborted);
    }

    // The form handler can't take a token, so it passes the request's own one on.
    private Task OnSubmit() => SelectAsync(RequestAborted);

    private async Task SelectAsync(CancellationToken cancellationToken)
    {
        Selection = await CurrentCsvStore.SelectAsync(CsvFilename, cancellationToken);
        Current = await CurrentCsvStore.ReadAsync(cancellationToken);
    }

    private bool IsListed(CurrentCsv current) => current.FileName is { } name && Files.Contains(name);

    private static string? MissingSuffix(CurrentCsv current) =>
        current.Status == CurrentCsvStatus.Missing ? " (file missing)" : null;
}
