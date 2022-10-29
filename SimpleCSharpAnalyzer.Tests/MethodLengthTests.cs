using DTOsAndUtilities;
using TokenBasedChecking;

namespace SimpleCSharpAnalyzer.Tests;

public class MethodLengthTests
{
    private const string MethodOf16Lines = @"
using DTOsAndUtilities;

namespace CommentChecker;

internal static class CommentMerger
{
    private static Dictionary<string, List<string>> MergeComments(Report report)
    {
        // example input line:
        // ""Commented-out code in SimpleCSharpAnalyzer\CommentChecker\CommentMerger.cs: // raw strings are NOT numbered""
        const int FilePathIndex = 3;

        Dictionary<string, List<string>> mergedComments = new();
        foreach (string warning in report.Warnings)
        {
            string[] parts = warning.Split(' ');
            string path = parts[FilePathIndex][..^1];
            string comment = string.Join("" "", parts.Skip(FilePathIndex + 1)).Trim();
            if (!mergedComments.ContainsKey(comment))
            {
                mergedComments[comment] = new List<string>();
            }
            mergedComments[comment].Add(path);
        }

        return mergedComments;
    }
}";

    [Fact]
    public void Lines_should_be_counted_correctly()
    {
        // arrange
        (FileAsTokens fileTokenData, Report report) = Utilities.Setup(MethodOf16Lines);

        //act
        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();

        // assert
        Assert.Single(report.Warnings);
        Assert.Equal(16, int.Parse(report.Warnings[0].Split()[7]));
    }
}