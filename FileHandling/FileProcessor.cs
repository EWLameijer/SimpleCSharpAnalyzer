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
    private readonly Report _globalReport = new();

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
        foreach (string relevantFileName in _filenames)
        {
            FileAsLines fileData = new(relevantFileName);

            Console.WriteLine($"\n***{fileData.ContextedFilename}***");

            Tokenizer tokenizer = new(fileData.Lines);
            IReadOnlyList<Token> tokens = tokenizer.Results();
            Report fileReport = PerformAnalyses(fileData, tokens, analysisMode);
            fileReport.Show();
            _globalReport.Add(fileReport);
        }
        return _globalReport;
    }

    private static Report PerformAnalyses(FileAsLines fileData, IReadOnlyList<Token> tokens,
        AnalysisMode analysisMode)
    {
        Debug.Assert(analysisMode != AnalysisMode.AnalysisModeNotSet);
        LineCounter counter = new(tokens);
        Report report = counter.CreateReport();
        IReadOnlyList<Token> tokensWithoutAttributes = new TokenFilterer().Filter(tokens);
        FileAsTokens fileTokenData = new(fileData.FilePath, tokensWithoutAttributes);
        if (analysisMode == AnalysisMode.CommentsOnly)
            new CommentAnalyzer(fileTokenData, report).AddWarnings();
        else DoFullAnalysis(fileData, report, fileTokenData);
        return report;
    }

    private static void DoFullAnalysis(FileAsLines fileData, Report report,
        FileAsTokens fileTokenData)
    {
        LineLengthChecker.AddWarnings(fileData, report);

        List<string> warnings = InappropriateAtsHandler.GetWarnings(fileTokenData);
        report.Warnings.AddRange(warnings);

        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();
        new MalapropAnalyzer(fileTokenData, report).AddWarnings();
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