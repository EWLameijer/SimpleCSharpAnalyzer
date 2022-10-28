using DTOsAndUtilities;
using TokenBasedChecking;

namespace SimpleCSharpAnalyzer;

internal static class LineLengthChecker
{
    public static void AddWarnings(FileData fileData, Report report)
    {
        for (int i = 0; i < fileData.Lines.Count; i++)
        {
            string line = fileData.Lines[i];
            if (line.Length > WarningSettings.MaxLineLength && !fileData.ContextedFilename.Contains("Test"))
                report.Warnings.Add($"Too long line in {fileData.ContextedFilename} at line {i + 1}: '{line}'");
        }
    }
}