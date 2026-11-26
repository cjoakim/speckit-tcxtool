namespace JoakimSoftware.TcxTool.Web.Timeline;

/// <summary>
/// One StateSet's value in a Timeline chart (specs/007-timeline-page/data-model.md "TimelinePoint").
/// </summary>
/// <param name="Center">The set's segment center, as a fraction of the bar.</param>
/// <param name="Value">The parsed value.</param>
public sealed record TimelinePoint(double Center, double Value);
