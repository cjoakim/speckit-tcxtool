namespace JoakimSoftware.TcxTool.Parser.Conversion;

/// <summary>
/// A typed, user-readable reason a TCX upload could not be converted. The message never contains
/// file content beyond an element name.
/// </summary>
/// <param name="Kind">The kind of failure.</param>
/// <param name="Message">The user-readable message, from <see cref="TcxErrorMessages"/>.</param>
/// <param name="Element">For <see cref="TcxErrorKind.InvalidValue"/>, the element that holds the bad value.</param>
/// <param name="TrackpointNumber">For <see cref="TcxErrorKind.InvalidValue"/>, the 1-based trackpoint number within its activity.</param>
public sealed record TcxError(TcxErrorKind Kind, string Message, string? Element, int? TrackpointNumber)
{
    /// <summary>Creates an error of <paramref name="kind"/> with its standard message.</summary>
    /// <param name="kind">The kind of failure.</param>
    /// <returns>The error.</returns>
    public static TcxError Of(TcxErrorKind kind) => new(kind, TcxErrorMessages.For(kind), null, null);
}
