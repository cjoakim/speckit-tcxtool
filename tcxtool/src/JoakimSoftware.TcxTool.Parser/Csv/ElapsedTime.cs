using System.Globalization;

namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>
/// Formats the time since an activity's first timed trackpoint as the CSV ElapsedSeconds and
/// ElapsedTime fields.
/// </summary>
public static class ElapsedTime
{
    /// <summary>
    /// Formats <paramref name="elapsed"/> as seconds, keeping any fraction (<c>3725</c>,
    /// <c>3725.6</c>, <c>-3.5</c>).
    /// </summary>
    /// <param name="elapsed">The time since the reference trackpoint; may be negative.</param>
    /// <returns>The formatted seconds.</returns>
    public static string FormatSeconds(TimeSpan elapsed) =>
        DerivedNumber.Format(elapsed.Ticks / (double)TimeSpan.TicksPerSecond);

    /// <summary>
    /// Formats <paramref name="elapsed"/> as <c>HH:MM:SS</c> in whole seconds, truncated toward zero.
    /// Hours have at least two digits and are not bounded; a negative value has a leading <c>-</c>.
    /// </summary>
    /// <param name="elapsed">The time since the reference trackpoint; may be negative.</param>
    /// <returns>The formatted clock time.</returns>
    public static string FormatClock(TimeSpan elapsed)
    {
        var totalSeconds = elapsed.Ticks / TimeSpan.TicksPerSecond;
        var sign = elapsed.Ticks < 0 ? "-" : string.Empty;
        var magnitude = Math.Abs(totalSeconds);

        var hours = magnitude / 3600;
        var minutes = magnitude % 3600 / 60;
        var seconds = magnitude % 60;
        return string.Create(CultureInfo.InvariantCulture, $"{sign}{hours:00}:{minutes:00}:{seconds:00}");
    }
}
