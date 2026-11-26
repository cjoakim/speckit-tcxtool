namespace JoakimSoftware.TcxTool.Web.Timeline;

/// <summary>
/// One of the Timeline's three charts (specs/007-timeline-page/data-model.md "TimelineChart",
/// research.md R6). <see cref="Min"/> and <see cref="Max"/> are null exactly when there are no points.
/// </summary>
/// <param name="Label">"Run Cadence", "Heart Rate", or "Altitude".</param>
/// <param name="Field">The States CSV field the chart shows.</param>
/// <param name="Min">The smallest value, or <c>v − 1</c> when every value is <c>v</c>.</param>
/// <param name="Max">The largest value, or <c>v + 1</c> when every value is <c>v</c>.</param>
/// <param name="Points">One per set with a finite value, in set order.</param>
public sealed record TimelineChart(string Label, string Field, double? Min, double? Max, IReadOnlyList<TimelinePoint> Points);
