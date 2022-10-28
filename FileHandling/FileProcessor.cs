using DTOsAndUtilities;
using TokenBasedChecking;
using Tokenizing;

namespace FileHandling;

public class FileProcessor
{
    private readonly List<string> _filenames;

    public FileProcessor(string pathname)
    {
        List<string> csFiles = Directory.GetFiles(pathname, "*.cs", SearchOption.AllDirectories).ToList();
        _filenames = csFiles.Where(
            fn => !fn.Contains(@"\Debug\") && !fn.Contains(@"\Release\") &&
            !fn.Contains(@"\Migrations\") && !fn.Contains(@".Designer.cs")).ToList();
    }

    public Report Process()
    {
        Report totalReport = new();
        foreach (string relevantFileName in _filenames)
        {
            FileData fileData = new(relevantFileName);

            Console.WriteLine($"\n***{fileData.ContextedFilename}***");

            Tokenizer tokenizer = new(fileData.Lines);
            IReadOnlyList<Token> tokens = tokenizer.Results();
            Report report = PerformAnalyses(fileData, tokens);
            report.Show();
            totalReport.Add(report);
        }
        return totalReport;
    }

    private static Report PerformAnalyses(FileData fileData, IReadOnlyList<Token> tokens)
    {
        IReadOnlyList<Token> tokensWithoutAttributes = new TokenFilterer().Filter(tokens);

        LineCounter counter = new(tokens);
        Report report = counter.CreateReport();
        FileTokenData fileTokenData = new(fileData.ContextedFilename, tokensWithoutAttributes);

        List<string> warnings = InappropriateAtsHandler.GetWarnings(fileData.ContextedFilename,
            tokensWithoutAttributes);
        report.Warnings.AddRange(warnings);
        LineLengthChecker.AddWarnings(fileData, report);
        // new CommentAnalyzer(fileTokenData, report).AddWarnings();
        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();
        new MalapropAnalyzer(fileTokenData, report).AddWarnings();
        return report;
    }
}