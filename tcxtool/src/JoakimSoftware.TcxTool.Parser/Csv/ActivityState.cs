namespace JoakimSoftware.TcxTool.Parser.Csv;

/// <summary>
/// The runner's state on one trackpoint, decided by its run cadence in steps per minute
/// (specs/006-activity-states/contracts/csv-format.md, the CSV State field).
/// </summary>
public static class ActivityState
{
    /// <summary>The state for a cadence of 0.</summary>
    public const string Stopped = "stopped";

    /// <summary>The state for a cadence above 0 and below <see cref="RunningStepsPerMinute"/>.</summary>
    public const string Walking = "walking";

    /// <summary>The state for a cadence of <see cref="RunningStepsPerMinute"/> or more.</summary>
    public const string Running = "running";

    /// <summary>The state for a missing or negative cadence.</summary>
    public const string Unknown = "unknown";

    /// <summary>The lowest cadence, in steps per minute, that counts as running.</summary>
    public const int RunningStepsPerMinute = 150;

    /// <summary>Returns the state for a cadence.</summary>
    /// <param name="stepsPerMinute">The cadence in steps per minute (already doubled), or null when there is none.</param>
    /// <returns>One of <see cref="Stopped"/>, <see cref="Walking"/>, <see cref="Running"/>, or <see cref="Unknown"/>.</returns>
    public static string For(double? stepsPerMinute) => stepsPerMinute switch
    {
        null or < 0 => Unknown,
        0 => Stopped,
        < RunningStepsPerMinute => Walking,
        _ => Running,
    };
}
