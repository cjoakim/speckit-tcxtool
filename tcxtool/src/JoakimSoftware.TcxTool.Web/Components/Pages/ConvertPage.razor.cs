using JoakimSoftware.TcxTool.Parser.Conversion;
using JoakimSoftware.TcxTool.Web.Conversion;
using JoakimSoftware.TcxTool.Web.State;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace JoakimSoftware.TcxTool.Web.Components.Pages;

/// <summary>
/// The static SSR upload page (specs/002-tcx-to-csv/contracts/convert-page.md). It binds the
/// uploaded file and renders the service's result; it never reads XML itself.
/// </summary>
public partial class ConvertPage
{
    private const string FormName = "tcx-upload";

    [Inject]
    private CsvConversionService ConversionService { get; set; } = default!;

    [Inject]
    private IOptions<TcxToolOptions> Options { get; set; } = default!;

    [Inject]
    private CurrentCsvStore CurrentCsvStore { get; set; } = default!;

    /// <summary>The request, available because the page renders with static SSR.</summary>
    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    /// <summary>The uploaded TCX file, bound from the multipart form.</summary>
    [SupplyParameterFromForm(FormName = FormName)]
    public IFormFile? TcxFile { get; set; }

    /// <summary>Set to <c>too-large</c> by <see cref="UploadSizeGuard"/>'s redirect.</summary>
    [SupplyParameterFromQuery(Name = "error")]
    public string? Error { get; set; }

    private ConversionResult? Result { get; set; }

    /// <summary>The outcome of recording the conversion's first CSV as current (spec 004 FR-011, FR-012).</summary>
    private CsvSelectionResult? Recorded { get; set; }

    private string LimitText => TcxErrorMessages.Megabytes(Options.Value.MaxUploadBytes);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Error == "too-large" && HttpMethods.IsGet(HttpContext?.Request.Method ?? HttpMethods.Get))
        {
            Result = ConversionService.TooLarge();
        }
    }

    // The form handler can't take a token, so it passes the request's own one on.
    private Task OnSubmit() => ConvertAsync(HttpContext?.RequestAborted ?? CancellationToken.None);

    private async Task ConvertAsync(CancellationToken cancellationToken)
    {
        Result = await ConversionService.ConvertAsync(TcxFile, cancellationToken);
        Recorded = await CurrentCsvStore.RecordConversionAsync(Result, cancellationToken);
    }
}
