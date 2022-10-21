using DTOsAndUtilities;
using Tokenizing;

namespace CSharpParser;

internal class LineCounter
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _codeLines;
    private int _commentLines;
    private int _emptyLines;
    private int _braceLines;
    private int _setupLines;
    private bool _openingBraceEncountered;

    public LineCounter(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens;
    }

    internal Report CreateReport()
    {
        ClassifyLines();
        return new Report
        {
            SetupLines = _setupLines,
            EmptyLines = _emptyLines,
            BraceLines = _braceLines,
            CommentLines = _commentLines,
            CodeLines = _codeLines
        };
    }

    private void ClassifyLines()
    {
        List<Token> lineTokens = new();

        foreach (Token token in _tokens)
        {
            ProcessToken(lineTokens, token);
        }
        ClassifyLine(lineTokens);
    }

    private void ProcessToken(List<Token> lineTokens, Token token)
    {
        if (token.TokenType == TokenType.NewLine)
        {
            ClassifyLine(lineTokens);
            lineTokens.Clear();
        }
        else
        {
            lineTokens.Add(token);
            if (token.TokenType == TokenType.BracesOpen) _openingBraceEncountered = true;
        }
    }

    private void ClassifyLine(List<Token> lineTokens)
    {
        if (lineTokens.Count == 0) _emptyLines++;
        else if (lineTokens.All(t => t.TokenType.IsCommentType())) _commentLines++;
        else if (lineTokens.All(t => t.TokenType.IsBraceType())) _braceLines++;
        else if (_openingBraceEncountered) _codeLines++;
        else
        {
            TokenType firstTokenType = lineTokens[0].TokenType;
            if (firstTokenType == TokenType.Namespace || firstTokenType == TokenType.Using)
                _setupLines++;
            else _codeLines++;
        }
    }
}