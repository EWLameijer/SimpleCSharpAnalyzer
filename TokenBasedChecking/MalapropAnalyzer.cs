using DTOsAndUtilities;
using Tokenizing;

namespace TokenBasedChecking;

public class MalapropAnalyzer
{
    private readonly List<string> _malapropisms = new() { "String", "Decimal", "Char", "Int32", "Double" };
    private readonly IReadOnlyList<Token> _tokens;
    private readonly string _contextedFilename;
    private readonly Report _report;

    public MalapropAnalyzer(FileTokenData fileData, Report report)
    {
        _tokens = fileData.Tokens;
        _contextedFilename = fileData.ContextedFilename;
        _report = report;
    }

    public void AddWarnings()
    {
        for (int i = 0; i < _tokens.Count; i++)
        {
            Token currentToken = _tokens[i];
            if (currentToken.TokenType == TokenType.Identifier && currentToken is ComplexToken ct)
            {
                WarnIfIdentifierHasWrongTypeName(ct, i);
            }
        }
    }

    private void WarnIfIdentifierHasWrongTypeName(ComplexToken ct, int i)
    {
        string identifierContents = ct.Info;
        if (_malapropisms.Contains(identifierContents)
            && _tokens[i - 1].TokenType != TokenType.Period
            && _tokens[i + 1].TokenType != TokenType.Comma)
        {
            _report.Warnings.Add(
                $"In {_contextedFilename} use regular type instead of '{identifierContents}'");
        }
    }
}