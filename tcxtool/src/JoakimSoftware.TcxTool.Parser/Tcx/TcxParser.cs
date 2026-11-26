using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using JoakimSoftware.TcxTool.Parser.Conversion;

namespace JoakimSoftware.TcxTool.Parser.Tcx;

/// <summary>
/// Parses Garmin TCX (Training Center Database v2) files into immutable records, always through a
/// secure <see cref="XmlReader"/> (constitution Principle IV).
/// </summary>
public static class TcxParser
{
    private const long MaxCharactersFromEntities = 1024;
    private const int PrologScanBytes = 64 * 1024;

    private static readonly XNamespace Tcx = TcxNamespaces.TrainingCenterDatabase;
    private static readonly XNamespace Ext = TcxNamespaces.ActivityExtension;

    /// <summary>
    /// Parses the TCX document in <paramref name="tcx"/>. Invalid input is returned as a failed
    /// <see cref="TcxParseResult"/>, never thrown.
    /// </summary>
    /// <param name="tcx">The TCX file content. A non-seekable stream is buffered in memory first.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The parsed document or the reason it could not be parsed.</returns>
    public static async Task<TcxParseResult> ParseAsync(Stream tcx, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tcx);

        var input = tcx;
        MemoryStream? buffer = null;
        if (!tcx.CanSeek)
        {
            buffer = new MemoryStream();
            await tcx.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            buffer.Position = 0;
            input = buffer;
        }

        try
        {
            var start = input.Position;
            XDocument document;
            try
            {
                document = await LoadAsync(input, cancellationToken).ConfigureAwait(false);
            }
            catch (XmlException)
            {
                input.Position = start;
                var kind = await PrologHasDoctypeAsync(input, cancellationToken).ConfigureAwait(false)
                    ? TcxErrorKind.ProhibitedContent
                    : TcxErrorKind.Malformed;
                return TcxParseResult.Failure(TcxError.Of(kind));
            }

            if (document.Root?.Name != Tcx + "TrainingCenterDatabase")
            {
                return TcxParseResult.Failure(TcxError.Of(TcxErrorKind.NotTcx));
            }

            try
            {
                var activities = document.Root
                    .Elements(Tcx + "Activities")
                    .Elements(Tcx + "Activity")
                    .Select(ReadActivity)
                    .ToList();
                return TcxParseResult.Success(new TcxDocument(activities));
            }
            catch (InvalidTcxException invalid)
            {
                return TcxParseResult.Failure(invalid.Error);
            }
        }
        finally
        {
            if (buffer is not null)
            {
                await buffer.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static async Task<XDocument> LoadAsync(Stream input, CancellationToken cancellationToken)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = MaxCharactersFromEntities,
            CloseInput = false,
            Async = true,
        };

        using var reader = XmlReader.Create(input, settings);
        return await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken).ConfigureAwait(false);
    }

    // research.md R4: skip whitespace, processing instructions, and comments before the first
    // element; the file is prohibited when the next markup is a DOCTYPE.
    private static async Task<bool> PrologHasDoctypeAsync(Stream input, CancellationToken cancellationToken)
    {
        var bytes = new byte[PrologScanBytes];
        var length = 0;
        int read;
        while (length < bytes.Length
            && (read = await input.ReadAsync(bytes.AsMemory(length), cancellationToken).ConfigureAwait(false)) > 0)
        {
            length += read;
        }

        var prolog = Encoding.UTF8.GetString(bytes, 0, length).AsSpan().TrimStart('﻿');
        while (true)
        {
            prolog = prolog.TrimStart();
            if (prolog.StartsWith("<?", StringComparison.Ordinal))
            {
                prolog = SkipPast(prolog, "?>");
            }
            else if (prolog.StartsWith("<!--", StringComparison.Ordinal))
            {
                prolog = SkipPast(prolog, "-->");
            }
            else
            {
                return prolog.StartsWith("<!DOCTYPE", StringComparison.Ordinal);
            }
        }
    }

    private static ReadOnlySpan<char> SkipPast(ReadOnlySpan<char> text, string end)
    {
        var index = text.IndexOf(end, StringComparison.Ordinal);
        return index < 0 ? [] : text[(index + end.Length)..];
    }

    private static TcxActivity ReadActivity(XElement activity)
    {
        var idText = Text(activity.Element(Tcx + "Id"));
        if (idText is null || !TryParseTime(idText, out var id))
        {
            throw new InvalidTcxException(TcxError.Of(TcxErrorKind.InvalidActivityId));
        }

        var creator = activity.Element(Tcx + "Creator");
        var trackpointNumber = 0;
        var laps = activity.Elements(Tcx + "Lap").Select(lap => ReadLap(lap, ref trackpointNumber)).ToList();

        return new TcxActivity(
            new RecordedTime(idText, id),
            Text(activity.Attribute("Sport")?.Value),
            creator is null ? null : new TcxDevice(Text(creator.Element(Tcx + "Name")), Text(creator.Element(Tcx + "UnitId"))),
            laps);
    }

    private static TcxLap ReadLap(XElement lap, ref int trackpointNumber)
    {
        var trackpoints = new List<TcxTrackpoint>();
        foreach (var trackpoint in lap.Elements(Tcx + "Track").Elements(Tcx + "Trackpoint"))
        {
            trackpointNumber++;
            trackpoints.Add(ReadTrackpoint(trackpoint, trackpointNumber));
        }

        return new TcxLap(
            Time(Text(lap.Attribute("StartTime")?.Value), "StartTime", null),
            Number(lap, Tcx + "TotalTimeSeconds", null),
            Number(lap, Tcx + "DistanceMeters", null),
            Number(lap, Tcx + "MaximumSpeed", null),
            Number(lap, Tcx + "Calories", null),
            Value(lap, Tcx + "AverageHeartRateBpm", null),
            Value(lap, Tcx + "MaximumHeartRateBpm", null),
            Text(lap.Element(Tcx + "Intensity")),
            Text(lap.Element(Tcx + "TriggerMethod")),
            trackpoints);
    }

    private static TcxTrackpoint ReadTrackpoint(XElement trackpoint, int number)
    {
        var position = trackpoint.Element(Tcx + "Position");
        var latitude = position is null ? null : Number(position, Tcx + "LatitudeDegrees", number);
        var longitude = position is null ? null : Number(position, Tcx + "LongitudeDegrees", number);
        var tpx = trackpoint.Element(Tcx + "Extensions")?.Element(Ext + "TPX");

        return new TcxTrackpoint(
            Time(Text(trackpoint.Element(Tcx + "Time")), "Time", number),
            latitude is null || longitude is null ? null : new TcxPosition(latitude, longitude),
            Number(trackpoint, Tcx + "AltitudeMeters", number),
            Number(trackpoint, Tcx + "DistanceMeters", number),
            Value(trackpoint, Tcx + "HeartRateBpm", number),
            tpx is null ? null : Number(tpx, Ext + "RunCadence", number),
            tpx is null ? null : Number(tpx, Ext + "Speed", number),
            tpx is null ? null : Number(tpx, Ext + "Watts", number));
    }

    // A missing element and an empty one both count as absent.
    private static string? Text(XElement? element) => Text(element?.Value);

    private static string? Text(string? value)
    {
        var text = value?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    // A value nested as <Parent><Value>…</Value></Parent> is reported under the parent's name.
    private static RecordedNumber? Value(XElement parent, XName name, int? trackpointNumber)
    {
        var element = parent.Element(name);
        return element is null ? null : Number(Text(element.Element(Tcx + "Value")), name.LocalName, trackpointNumber);
    }

    private static RecordedNumber? Number(XElement parent, XName name, int? trackpointNumber) =>
        Number(Text(parent.Element(name)), name.LocalName, trackpointNumber);

    private static RecordedNumber? Number(string? text, string element, int? trackpointNumber)
    {
        if (text is null)
        {
            return null;
        }

        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
        {
            throw InvalidValue(element, trackpointNumber);
        }

        return new RecordedNumber(text, value);
    }

    private static RecordedTime? Time(string? text, string element, int? trackpointNumber)
    {
        if (text is null)
        {
            return null;
        }

        return TryParseTime(text, out var value) ? new RecordedTime(text, value) : throw InvalidValue(element, trackpointNumber);
    }

    // A time written without an offset is read as UTC, so the result never depends on the machine's zone.
    private static bool TryParseTime(string text, out DateTimeOffset value) =>
        DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out value);

    private static InvalidTcxException InvalidValue(string element, int? trackpointNumber) =>
        new(new TcxError(
            TcxErrorKind.InvalidValue,
            TcxErrorMessages.For(TcxErrorKind.InvalidValue, element: element, trackpointNumber: trackpointNumber),
            element,
            trackpointNumber));

    private sealed class InvalidTcxException(TcxError error) : Exception(error.Message)
    {
        public TcxError Error { get; } = error;
    }
}
