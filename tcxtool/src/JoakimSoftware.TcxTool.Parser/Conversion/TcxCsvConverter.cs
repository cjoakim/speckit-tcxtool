using System.Globalization;
using System.Text;

using JoakimSoftware.TcxTool.Parser.Csv;
using JoakimSoftware.TcxTool.Parser.Tcx;

namespace JoakimSoftware.TcxTool.Parser.Conversion;

/// <summary>
/// Converts one uploaded TCX file to an activity CSV and a States CSV per activity in an output
/// directory. Every file is rendered in memory first, then written to temp files and renamed, so a
/// failure leaves the directory as it was (feature 002 research R8; feature 006 research R6).
/// </summary>
public static class TcxCsvConverter
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Converts <paramref name="tcx"/> and saves both CSVs of every activity to <paramref name="outputDirectory"/>.</summary>
    /// <param name="tcx">The TCX file content.</param>
    /// <param name="uploadedFileName">The file name as uploaded; it supplies the ActivityId.</param>
    /// <param name="outputDirectory">The directory for the CSVs; created if missing.</param>
    /// <param name="cancellationToken">Cancels the conversion.</param>
    /// <returns>The saved CSVs, or the reason nothing was saved.</returns>
    public static async Task<ConversionResult> ConvertAsync(
        Stream tcx,
        string uploadedFileName,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tcx);
        ArgumentNullException.ThrowIfNull(uploadedFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        if (!ActivityIdName.TryFromFileName(uploadedFileName, out var activityId, out var nameError))
        {
            return ConversionResult.Failure(nameError!);
        }

        var parsed = await TcxParser.ParseAsync(tcx, cancellationToken).ConfigureAwait(false);
        if (!parsed.IsSuccess)
        {
            return ConversionResult.Failure(parsed.Error!);
        }

        var activities = parsed.Document!.Activities;
        if (!CsvFileName.TryFor(activities, activityId, out var fileNames, out var namingError))
        {
            return ConversionResult.Failure(namingError!);
        }

        var saved = new List<SavedCsv>(activities.Count);
        var files = new List<(string FileName, string Content)>(activities.Count * 2);
        for (var i = 0; i < activities.Count; i++)
        {
            // The activity rows are built once; the States CSV is calculated from them (FR-010).
            var rows = ActivityCsvWriter.Rows(activities[i], activityId);
            var states = StatesCsvWriter.Rows(rows);
            var statesFileName = StatesCsvFileName.For(fileNames[i]);

            using var activityWriter = new StringWriter(CultureInfo.InvariantCulture);
            ActivityCsvWriter.Write(rows, activityWriter);
            using var statesWriter = new StringWriter(CultureInfo.InvariantCulture);
            StatesCsvWriter.Write(states, statesWriter);

            files.Add((fileNames[i], activityWriter.ToString()));
            files.Add((statesFileName, statesWriter.ToString()));
            saved.Add(new SavedCsv(fileNames[i], rows.Count, statesFileName, states.Count));
        }

        var temps = new List<string>(files.Count);
        var moved = new List<string>(files.Count);
        try
        {
            Directory.CreateDirectory(outputDirectory);
            foreach (var (fileName, content) in files)
            {
                var temp = $"{Path.Combine(outputDirectory, fileName)}.{Guid.NewGuid():N}.tmp";
                temps.Add(temp);
                await File.WriteAllTextAsync(temp, content, Utf8WithoutBom, cancellationToken).ConfigureAwait(false);
            }

            for (var i = 0; i < temps.Count; i++)
            {
                var final = Path.Combine(outputDirectory, files[i].FileName);
                File.Move(temps[i], final, overwrite: true);
                moved.Add(final);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            DeleteQuietly(temps.Concat(moved));
            return ConversionResult.Failure(TcxError.Of(TcxErrorKind.OutputFailed));
        }
        catch (OperationCanceledException)
        {
            DeleteQuietly(temps.Concat(moved));
            throw;
        }

        return ConversionResult.Success(saved);
    }

    private static void DeleteQuietly(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Best effort: the original failure is what the user needs to see.
            }
        }
    }
}
