using DTOsAndUtilities;
using SimpleCSharpAnalyzer;
using TokenBasedChecking;
using Tokenizing;

string pathname;
if (args.Length == 0)
{
    Console.WriteLine("Code-analyzator (edit de settings.txt om de gevoeligheid aan te passen)");
    Console.Write("Geef de naam van de directory waarvan je de code wilt analyseren: ");
    pathname = Console.ReadLine()!;
}
else
{
    pathname = args[0];
}

List<string> csFiles = Directory.GetFiles(pathname, "*.cs", SearchOption.AllDirectories).ToList();
IEnumerable<string> relevantFileNames = csFiles.Where(
    fn => !fn.Contains(@"\Debug\") && !fn.Contains(@"\Release\") &&
    !fn.Contains(@"\Migrations\") && !fn.Contains(@".Designer.cs"));
Report totalReport = new();
foreach (string relevantFileName in relevantFileNames)
{
    FileData fileData = new(relevantFileName);

    Console.WriteLine($"\n***{fileData.ContextedFilename}***");

    Tokenizer tokenizer = new(fileData.Lines);
    IReadOnlyList<Token> tokens = tokenizer.Results();
    IReadOnlyList<Token> tokensWithoutAttributes = new TokenFilterer().Filter(tokens);
    List<string> warnings = HandleInappropriateAts(fileData.ContextedFilename, tokensWithoutAttributes);
    LineCounter counter = new(tokens);
    FileTokenData fileTokenData = new(fileData.ContextedFilename, tokensWithoutAttributes);
    Report report = counter.CreateReport();
    report.Warnings.AddRange(warnings);
    LineLengthChecker.AddWarnings(fileData, report);
    new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();
    new MalapropAnalyzer(fileTokenData, report).AddWarnings();
    report.Show();
    totalReport.Add(report);
}

Console.WriteLine($"\n***TOTAL ({pathname})***");
totalReport.Show();

List<string> HandleInappropriateAts(string contextedFilename, IReadOnlyList<Token> tokens)
{
    List<Token> output = new();
    List<string> warnings = new();
    for (int i = 0; i < tokens.Count; i++)
    {
        Token current = tokens[i];
        if (current.TokenType == TokenType.Identifier)
        {
            WarnIfIdentifierInappropiatelyStartsWithAt(contextedFilename, warnings, current);
        }
    }
    return warnings;
}

static void WarnIfIdentifierInappropiatelyStartsWithAt(string contextedFilename, List<string> warnings, Token current)
{
    string identifierName = ((ComplexToken)current).Info;
    if (identifierName[0] == '@')
    {
        string restOfName = identifierName.Substring(1);
        if (!AllCSharpKeywords.KeyWords.Contains(restOfName))
        {
            warnings.Add($"Unnecessary '@' in {identifierName} (in {contextedFilename}).");
        }
    }
}