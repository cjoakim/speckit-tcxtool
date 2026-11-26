using JoakimSoftware.TcxTool.Web.Analysis;
using JoakimSoftware.TcxTool.Web.Components;
using JoakimSoftware.TcxTool.Web.Conversion;
using JoakimSoftware.TcxTool.Web.State;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOptions<TcxToolOptions>()
    .Bind(builder.Configuration.GetSection(TcxToolOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddScoped<CsvConversionService>();
builder.Services.AddSingleton<CurrentCsvStore>();
builder.Services.AddSingleton<CurrentActivityLoader>();
builder.Services.AddSingleton<CurrentStatesLoader>();

// Requests above the upload limit plus 1 MiB are refused before the form is read (research.md R2).
builder.Services.AddOptions<KestrelServerOptions>()
    .Configure<IOptions<TcxToolOptions>>((kestrel, tcxTool) =>
        kestrel.Limits.MaxRequestBodySize = UploadSizeGuard.RequestCeiling(tcxTool.Value));
builder.Services.AddOptions<FormOptions>()
    .Configure<IOptions<TcxToolOptions>>((form, tcxTool) =>
        form.MultipartBodyLengthLimit = UploadSizeGuard.RequestCeiling(tcxTool.Value));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// The guard must run before routing, because antiforgery runs right after routing and reads the form.
app.UseMiddleware<UploadSizeGuard>();
app.UseRouting();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
