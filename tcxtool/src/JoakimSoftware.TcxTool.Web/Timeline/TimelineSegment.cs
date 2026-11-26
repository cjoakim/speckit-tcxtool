namespace JoakimSoftware.TcxTool.Web.Timeline;

/// <summary>
/// One StateSet drawn as part of the Timeline bar (specs/007-timeline-page/data-model.md
/// "TimelineSegment"). Positions are fractions of the bar, from 0 to 1, so they don't depend on pixels.
/// </summary>
/// <param name="State">The set's State, as written.</param>
/// <param name="Start">Where the segment begins: 0 for the first segment.</param>
/// <param name="End">Where it ends: the next segment's <paramref name="Start"/>, and exactly 1 for the last.</param>
/// <param name="Center">The midpoint, where the set's chart points are drawn.</param>
/// <param name="Values">The set's 16 fields as written, in <see cref="TimelineLayout.FieldNames"/> order.</param>
public sealed record TimelineSegment(string State, double Start, double End, double Center, IReadOnlyList<string> Values);
