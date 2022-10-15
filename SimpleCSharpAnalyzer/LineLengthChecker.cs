using CSharpParser;

namespace SimpleCSharpAnalyzer;

internal static class LineLengthChecker
{
    private static readonly int MaxLineLength = 120;

    public static void AddWarnings(FileData fileData, Report report)
    {
        for (int i = 0; i < fileData.Lines.Count; i++)
        {
            string line = fileData.Lines[i];
            if (line.Length > MaxLineLength)
                report.Warnings.Add($"Too long line in {fileData.ContextedFilename} at line {i}: '{line}'");
        }
    }
}