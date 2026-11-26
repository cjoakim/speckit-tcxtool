using System.Text.Json.Serialization;

namespace JoakimSoftware.TcxTool.Web.State;

/// <summary>
/// The shape written to <c>tcxtool-state.json</c> (specs/004-about-and-csv-state/contracts/state-file.md
/// "Content written", with the addition in specs/006-activity-states/contracts/state-file.md).
/// </summary>
/// <param name="CsvFilename">The current CSV's file name.</param>
/// <param name="StatesCsvFilename">Its States CSV's file name, or null to leave the property out.</param>
internal sealed record TcxToolState(
    [property: JsonPropertyName("csvFilename")] string CsvFilename,
    [property: JsonPropertyName("statesCsvFilename"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? StatesCsvFilename);
