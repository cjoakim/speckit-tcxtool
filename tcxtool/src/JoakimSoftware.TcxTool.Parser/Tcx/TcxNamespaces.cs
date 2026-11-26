using System.Xml.Linq;

namespace JoakimSoftware.TcxTool.Parser.Tcx;

/// <summary>The XML namespaces the parser reads.</summary>
public static class TcxNamespaces
{
    /// <summary>Training Center Database v2, the namespace of every core TCX element.</summary>
    public static readonly XNamespace TrainingCenterDatabase = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";

    /// <summary>Activity Extension v2, which holds per-trackpoint speed, run cadence, and watts.</summary>
    public static readonly XNamespace ActivityExtension = "http://www.garmin.com/xmlschemas/ActivityExtension/v2";
}
