using System.Text;
using DTOsAndUtilities;
using Tokenizing;

namespace TokenBasedChecking;

public class CommentAnalyzer
{
    private readonly string _contextedFilename;
    private readonly Report _report;
    private readonly IReadOnlyList<Token> _tokens;

    public CommentAnalyzer(FileAsTokens fileData, Report report)
    {
        _contextedFilename = fileData.ContextedFilename;
        _report = report;
        _tokens = fileData.Tokens;
    }

    //  $"Commented-out code in {_contextedFilename}: {context}");
    public void AddWarnings()
    {
        for (int i = 0; i < _tokens.Count; i++)
        {
            if (_tokens[i].TokenType.IsCommentType())
            {
                int originalI = i;
                StringBuilder context = new();
                context.Append(GetPrevious3LinesOrLess(i));
                context.Append(_tokens[i].PrettyPrint());
                (i, string following, string remainingComment) = GetNext3LinesOrLess(i);
                context.Append(following);
                CommentData commentData = new(_contextedFilename,
                    _tokens[originalI].PrettyPrint() + remainingComment, context.ToString());
                _report.Comments.Add(commentData);
            }
        }
    }

    private (int i, string following, string remainingComment) GetNext3LinesOrLess(int i)
    {
        bool sufficientLines = false;
        int linesSoFar = 0;
        bool newlineMode = false;
        bool stillInComment = true;
        string remainingComment = "";
        StringBuilder result = new();
        int afterCommentIndex = i + 1;
        for (; afterCommentIndex < _tokens.Count && !sufficientLines; afterCommentIndex++)
        {
            Token currentToken = _tokens[afterCommentIndex];
            TokenType currentTokenType = currentToken.TokenType;
            if (currentTokenType == TokenType.NewLine)
            {
                if (stillInComment) remainingComment += NiceDisplay(currentToken);
                if (newlineMode && !stillInComment) sufficientLines = true;
                if (!stillInComment) linesSoFar++;
                newlineMode = true;
            }
            else if (currentTokenType.IsCommentType())
            {
                if (stillInComment) remainingComment += NiceDisplay(currentToken);
                newlineMode = false;
            }
            else
            {
                newlineMode = false;
                stillInComment = false;
            }
            string text = NiceDisplay(currentToken);
            result.Append(text + " ");
            sufficientLines = linesSoFar >= 3;
        }
        return (afterCommentIndex, result.ToString(), remainingComment.Trim());
    }

    private string NiceDisplay(Token token)
    {
        if (token is ComplexToken ct)
        {
            string baseText = ct.Info;
            if (ct.TokenType == TokenType.LineComment) baseText = "// " + baseText;
            if (ct.TokenType == TokenType.BlockCommentWhole) baseText = "/* " + baseText + "*/";
            if (ct.TokenType == TokenType.BlockCommentEnd) baseText += "*/";
            if (ct.TokenType == TokenType.BlockCommentStart) baseText = "/* " + baseText;
            return baseText;
        }
        else
        {
            return token.PrettyPrint().ToLower();
        }
    }

    private string GetPrevious3LinesOrLess(int i)
    {
        bool sufficientLines = false;
        int linesSoFar = -1;
        bool newlineMode = false;
        StringBuilder result = new();
        for (int beforeCommentIndex = i - 1; beforeCommentIndex >= 0 && !sufficientLines; beforeCommentIndex--)
        {
            Token currentToken = _tokens[beforeCommentIndex];
            if (currentToken.TokenType == TokenType.NewLine)
            {
                if (newlineMode) sufficientLines = true;
                linesSoFar++;
                newlineMode = true;
            }
            else
            {
                newlineMode = false;
            }
            string text = currentToken is ComplexToken ct ? ct.Info : currentToken.PrettyPrint().ToLower();
            result.Insert(0, text + " ");
            sufficientLines = linesSoFar >= 3;
        }
        return result.ToString();
    }
}