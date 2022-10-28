using System.Diagnostics;
using DTOsAndUtilities;
using TokenBasedChecking;
using Tokenizing;

namespace FileHandling;

public enum AnalysisMode
{ AnalysisModeNotSet, CommentsOnly, Full }

public class FileProcessor
{
    private readonly List<string> _filenames;

    public string PathName { get; private set; } = null!;

    public FileProcessor(string[] args, string query)
    {
        GetPathName(args, query);
        List<string> csFiles = Directory.GetFiles(PathName, "*.cs", SearchOption.AllDirectories).ToList();
        _filenames = csFiles.Where(
            fn => !fn.Contains(@"\Debug\") && !fn.Contains(@"\Release\") &&
            !fn.Contains(@"\Migrations\") && !fn.Contains(@".Designer.cs")).ToList();
    }

    public Report Process(AnalysisMode analysisMode)
    {
        Report totalReport = new();
        foreach (string relevantFileName in _filenames)
        {
            FileData fileData = new(relevantFileName);

            Console.WriteLine($"\n***{fileData.ContextedFilename}***");

            Tokenizer tokenizer = new(fileData.Lines);
            IReadOnlyList<Token> tokens = tokenizer.Results();
            Report report = PerformAnalyses(fileData, tokens, analysisMode);
            report.Show();
            totalReport.Add(report);
        }
        return totalReport;
    }

    private static Report PerformAnalyses(FileData fileData, IReadOnlyList<Token> tokens,
        AnalysisMode analysisMode)
    {
        Debug.Assert(analysisMode != AnalysisMode.AnalysisModeNotSet);
        LineCounter counter = new(tokens);
        Report report = counter.CreateReport();
        IReadOnlyList<Token> tokensWithoutAttributes = new TokenFilterer().Filter(tokens);
        FileTokenData fileTokenData = new(fileData.ContextedFilename, tokensWithoutAttributes);
        if (analysisMode == AnalysisMode.CommentsOnly)
        {
            new CommentAnalyzer(fileTokenData, report).AddWarnings();
        }
        else
        {
            LineLengthChecker.AddWarnings(fileData, report);

            List<string> warnings = InappropriateAtsHandler.GetWarnings(fileData.ContextedFilename,
                tokensWithoutAttributes);
            report.Warnings.AddRange(warnings);

            new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();
            new MalapropAnalyzer(fileTokenData, report).AddWarnings();
        }
        return report;
    }

    public void GetPathName(string[] args, string query)
    {
        if (args.Length == 0)
        {
            Console.Write(query);
            PathName = Console.ReadLine()!;
        }
        else
        {
            PathName = args[0];
        }
    }
}