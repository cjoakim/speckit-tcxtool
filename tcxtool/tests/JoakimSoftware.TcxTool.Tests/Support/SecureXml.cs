using System.Xml;
using System.Xml.Linq;

namespace JoakimSoftware.TcxTool.Tests.Support;

/// <summary>
/// Loads XML files through the secure <see cref="XmlReader"/> settings required by
/// constitution Principle IV, so test code uses the same XML pattern as production code.
/// </summary>
public static class SecureXml
{
    private const long MaxCharactersFromEntities = 1024;

    /// <summary>Loads the XML file at <paramref name="path"/> into an <see cref="XDocument"/>.</summary>
    public static XDocument Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = MaxCharactersFromEntities,
        };

        using var stream = File.OpenRead(path);
        using var reader = XmlReader.Create(stream, settings);
        return XDocument.Load(reader);
    }
}
