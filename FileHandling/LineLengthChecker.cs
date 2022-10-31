using DTOsAndUtilities;
using TokenBasedChecking;

namespace FileHandling;

internal static class LineLengthChecker
{
    public static void AddWarnings(FileAsLines fileData, Report report)
    {
        for (int i = 0; i < fileData.Lines.Count; i++)
        {
            string line = fileData.Lines[i];
            if (!fileData.ContextedFilename.Contains("Test"))
            {
                if (line.Length > 15)
                {
                    AttentionCategory category = line.Length > 25 ? AttentionCategory.VeryVeryLongLines : AttentionCategory.VeryLongLines;
                    report.AddWarning(category, $"Too long line in {fileData.ContextedFilename} at line {i + 1}: '{line}'");
                }
                else report.ScoreCorrect(AttentionCategory.VeryLongLines);
            }
                
        }
    }
}