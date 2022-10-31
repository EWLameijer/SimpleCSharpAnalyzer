using DTOsAndUtilities;
using TokenBasedChecking;

namespace SimpleCSharpAnalyzer.Tests;

public class MethodLengthTests
{
    private const string AlsoDetectOverlyBigTopLevel = @"
using Phoneshop.Domain.Models;
using Phoneshop.Domain.Models;
using Phoneshop.Domain.Models;

Console.WriteLine(1);
Console.WriteLine(2);
Console.WriteLine(3);
Console.WriteLine(4);
Console.WriteLine(5);
Console.WriteLine(6);
Console.WriteLine(7);
Console.WriteLine(8);
Console.WriteLine(9);
Console.WriteLine(10);
Console.WriteLine(11);
Console.WriteLine(12);
Console.WriteLine(13);
Console.WriteLine(14);
Console.WriteLine(15);
Console.WriteLine(16);";

    [Fact]
    public void Top_level_statements_should_not_sprawl_either()
    {
        // arrange
        (FileAsTokens fileTokenData, Report report) = Utilities.Setup(AlsoDetectOverlyBigTopLevel);

        // act
        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();

        // assert
        Assert.Single(report.GetWarnings());
        Assert.Equal(16, int.Parse(report.GetWarnings()[0].Text.Split()[8]));
    }

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

        // act
        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();

        // assert
        Assert.Single(report.GetWarnings());
        Assert.Equal(16, int.Parse(report.GetWarnings()[0].Text.Split()[7]));
    }
}