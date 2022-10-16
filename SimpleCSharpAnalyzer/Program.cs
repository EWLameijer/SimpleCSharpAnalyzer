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
    IReadOnlyList<Token> tokensWithoutAttributes = HandleDecimalLiterals(FilterOutAttributes(tokens));
    LineCounter counter = new(tokens);
    FileTokenData fileTokenData = new(fileData.ContextedFilename, tokensWithoutAttributes);
    Report report = counter.CreateReport();
    LineLengthChecker.AddWarnings(fileData, report, maxLineLength);
    new IdentifierAnalyzer(fileTokenData, report).AddWarnings();
    new MalapropAnalyzer(fileTokenData, report).AddWarnings();
    report.Show();
    totalReport.Add(report);
}

IReadOnlyList<Token> HandleDecimalLiterals(IReadOnlyList<Token> tokens)
{
    List<Token> output = new();
    for (int i = 0; i < tokens.Count; i++)
    {
        Token current = tokens[i];
        int lastOutputIndex = output.Count - 1;
        if (current.TokenType == TokenType.Identifier &&
            ((ComplexToken)current).Info.ToLower() == "m"
            && output[lastOutputIndex].TokenType == TokenType.Number)
        {
            output[lastOutputIndex].TokenType = TokenType.DecimalLiteral;
        }
        else
        {
            output.Add(current);
        }
    }
    return output;
}

Console.WriteLine($"\n***TOTAL ({pathname})***");
totalReport.Show();

IReadOnlyList<Token> FilterOutAttributes(IReadOnlyList<Token> tokens)
{
    List<Token> output = new();
    for (int i = 0; i < tokens.Count; i++)
    {
        TokenType currentType = tokens[i].TokenType;
        if (currentType == TokenType.BracketsOpen && LastRealType(tokens, i) != TokenType.Identifier)
        {
            int depth = 0;
            int newIndex = i + 1;
            TokenType newToken = tokens[newIndex].TokenType;
            while (newToken != TokenType.BracketsClose || depth > 0)
            {
                if (newToken == TokenType.BracketsOpen) depth++;
                if (newToken == TokenType.BracketsClose) depth--;
                newIndex++;
                newToken = tokens[newIndex].TokenType;
            }
            i = newIndex; // index of ']'
        }
        else output.Add(tokens[i]);
    }
    return output;
}

TokenType LastRealType(IReadOnlyList<Token> tokens, int i)
{
    for (int investigatedIndex = i - 1; investigatedIndex > 0; investigatedIndex--)
    {
        TokenType tokenType = tokens[investigatedIndex].TokenType;
        if (!tokenType.IsSkippable()) return tokenType;
    }
    return TokenType.Identifier;
}