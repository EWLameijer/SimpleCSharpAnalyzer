using System.Diagnostics;
using DTOsAndUtilities;
using TokenBasedChecking;
using Tokenizing;

namespace FileHandling;

public enum AnalysisMode
{ AnalysisModeNotSet, CommentsOnly, Full }

public class FileProcessor
{
    private readonly Report _globalReport = new();

    private readonly IReadOnlyList<string> _filenames;

    public string PathName { get; private set; } = null!;

    public FileProcessor(FileRepository fileRepository)
    {
        _filenames = fileRepository.Filenames;
        PathName = fileRepository.PathName;
    }

    public Report Process(AnalysisMode analysisMode)
    {
        foreach (string relevantFileName in _filenames)
        {
            FileAsLines fileData = new(relevantFileName);

            Console.WriteLine($"\n***{fileData.ContextedFilename}***");

            Tokenizer tokenizer = new(fileData.Lines);
            IReadOnlyList<Token> tokens = tokenizer.Results();
            Report fileReport = PerformAnalyses(fileData, tokens, analysisMode, PathName);
            fileReport.Show();
            _globalReport.Add(fileReport);
        }
        return _globalReport;
    }

    private static Report PerformAnalyses(FileAsLines fileData, IReadOnlyList<Token> tokens,
        AnalysisMode analysisMode, string basePath)
    {
        Debug.Assert(analysisMode != AnalysisMode.AnalysisModeNotSet);
        LineCounter counter = new(tokens);
        Report report = counter.CreateReport();
        IReadOnlyList<Token> tokensWithoutAttributes = new TokenFilterer().Filter(tokens);
        FileAsTokens fileTokenData = new(fileData.FilePath, tokensWithoutAttributes, basePath);
        if (analysisMode == AnalysisMode.CommentsOnly)
            new CommentAnalyzer(fileTokenData, report).AddCommentAnalysis();
        else DoFullAnalysis(fileData, report, fileTokenData);
        return report;
    }

    private static void DoFullAnalysis(FileAsLines fileData, Report report,
        FileAsTokens fileTokenData)
    {
        LineLengthChecker.AddWarnings(fileData, report);

        List<string> warnings = InappropriateAtsHandler.GetWarnings(fileTokenData);
        report.AddNonScoredWarnings(warnings);
        new CommentAnalyzer(fileTokenData, report).AddWarnings();
        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();
        new MalapropAnalyzer(fileTokenData, report).AddWarnings();
    }
}