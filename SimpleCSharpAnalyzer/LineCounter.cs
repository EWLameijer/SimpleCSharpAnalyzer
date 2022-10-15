using Tokenizing;

namespace CSharpParser;

internal class LineCounter
{
    private readonly IReadOnlyList<Token> tokens;
    private int _codeLines;
    private int _commentLines;
    private int _braceLines;
    private int _setupLines;

    public LineCounter(IReadOnlyList<Token> tokens)
    {
        this.tokens = tokens;
    }

    internal Report CreateReport()
    {
        List<Token> lineTokens = new();
        int emptyLines = 0;
        bool openingBraceEncountered = false;

        foreach (Token token in tokens)
        {
            if (token.TokenType == TokenType.NewLine)
            {
                if (lineTokens.Count == 0) emptyLines++; else ClassifyLine(lineTokens, openingBraceEncountered);
                lineTokens.Clear();
            }
            else
            {
                lineTokens.Add(token);
                if (token.TokenType == TokenType.BracesOpen) openingBraceEncountered = true;
            }
        }
        if (lineTokens.Count == 0) emptyLines++; else ClassifyLine(lineTokens, openingBraceEncountered);
        return new Report
        {
            SetupLines = _setupLines,
            EmptyLines = emptyLines,
            BraceLines = _braceLines,
            CommentLines = _commentLines,
            CodeLines = _codeLines
        };
    }

    private void ClassifyLine(List<Token> lineTokens, bool openingBraceEncountered)
    {
        if (lineTokens.All(t => t.TokenType.IsCommentType())) _commentLines++;
        else if (lineTokens.All(t => t.TokenType.IsBraceType())) _braceLines++;
        else if (openingBraceEncountered) _codeLines++;
        else
        {
            TokenType firstTokenType = lineTokens[0].TokenType;
            if (firstTokenType == TokenType.Namespace || firstTokenType == TokenType.Using)
                _setupLines++;
            else _codeLines++;
        }
    }
}