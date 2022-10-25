using CSharpParser;
using DTOsAndUtilities;
using SimpleCSharpAnalyzer;
using TokenBasedChecking;
using Tokenizing;

string pathname;
string[] arguments;
const int DefaultMaxLineLength = 120;
int maxLineLength = DefaultMaxLineLength;
if (args.Length == 0)
{
    Console.Write("Geef de naam van de directory waarvan je de code-regels wilt tellen: ");
    arguments = Console.ReadLine()!.Split();
    pathname = arguments[0];
    if (arguments.Length > 1) maxLineLength = int.TryParse(arguments[1], out int max) ? max : DefaultMaxLineLength;
}
else
{
    pathname = args[0];
    if (args.Length == 2) maxLineLength = int.TryParse(args[1], out int max) ? max : DefaultMaxLineLength;
}

List<string> csFiles = Directory.GetFiles(pathname, "*.cs", SearchOption.AllDirectories).ToList();
IEnumerable<string> relevantFileNames = csFiles.Where(
    fn => !fn.Contains(@"\Debug\") && !fn.Contains(@"\Release\") &&
    !fn.Contains(@"\Migrations\") && !fn.Contains(@".Designer.cs"));
Report totalReport = new();
foreach (string relevantFileName in relevantFileNames)
{
    FileData fileData = new(relevantFileName);

    Tokenizer tokenizer = new(fileData.Lines);

    Console.WriteLine($"\n***{fileData.ContextedFilename}***");

    while (tokenizer.HasNextToken())
    {
        tokenizer.Get();
    }
    IReadOnlyList<Token> tokens = tokenizer.Results();
    IReadOnlyList<Token> tokensWithoutAttributes = new TokenFilterer().Filter(tokens);
    (IReadOnlyList<Token> atLessIdentifiers, List<string> warnings) =
        HandleInappropriateAts(fileData.ContextedFilename, tokensWithoutAttributes);
    LineCounter counter = new(tokens);
    FileTokenData fileTokenData = new(fileData.ContextedFilename, tokensWithoutAttributes);
    Report report = counter.CreateReport();
    report.Warnings.AddRange(warnings);
    LineLengthChecker.AddWarnings(fileData, report, maxLineLength);
    new IdentifierAnalyzer(fileTokenData, report).AddWarnings();
    new MalapropAnalyzer(fileTokenData, report).AddWarnings();
    //new MethodLengthAnalyzer(fileTokenData, report).AddWarnings();
    report.Show();
    totalReport.Add(report);
}

Console.WriteLine($"\n***TOTAL ({pathname})***");
totalReport.Show();

(IReadOnlyList<Token> tokens, List<string> warnings)
    HandleInappropriateAts(string contextedFilename, IReadOnlyList<Token> tokens)
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
    return (output, warnings);
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