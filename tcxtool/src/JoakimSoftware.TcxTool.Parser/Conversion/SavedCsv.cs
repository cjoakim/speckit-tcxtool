namespace JoakimSoftware.TcxTool.Parser.Conversion;

/// <summary>The two files a conversion saved for one activity: its activity CSV and its States CSV.</summary>
/// <param name="FileName">The activity CSV's file name only, for example <c>2026-11-01_24932322230.csv</c>.</param>
/// <param name="RowCount">The number of data rows; 0 means the activity had no trackpoints.</param>
/// <param name="StatesFileName">The States CSV's file name, <c>StatesCsvFileName.For(FileName)</c>.</param>
/// <param name="StateSetCount">The number of StateSet rows in the States CSV; 0 exactly when <paramref name="RowCount"/> is 0.</param>
public sealed record SavedCsv(string FileName, int RowCount, string StatesFileName, int StateSetCount);
