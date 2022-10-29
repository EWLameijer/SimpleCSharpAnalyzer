using System.Text;
using DTOsAndUtilities;
using Tokenizing;

using static Tokenizing.TokenType;

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
                i = HandleComment(i);
            }
        }
    }

    private int HandleComment(int i)
    {
        int originalI = i;
        StringBuilder context = new();
        context.Append(new PrecedingContextGetter().Previous3Lines(i, _tokens));
        context.Append(_tokens[i].PrettyPrint());
        (i, string following, string remainingComment) = new FollowingContextGetter().Next3Lines(i, _tokens);
        context.Append(following);
        CommentData commentData = new(_contextedFilename,
            _tokens[originalI].PrettyPrint() + remainingComment, context.ToString());
        _report.Comments.Add(commentData);
        return i;
    }

    private class FollowingContextGetter
    {
        private bool _sufficientLines = false;
        private int _linesSoFar = 0;
        private bool _newlineMode = false;
        private bool _stillInComment = true;
        private readonly StringBuilder _remainingComment = new();
        private readonly StringBuilder _result = new();
        private TokenType? _previousTokenType = null;

        public (int i, string following, string remainingComment) Next3Lines(int i, IReadOnlyList<Token> tokens)
        {
            int afterCommentIndex = i + 1;
            for (; afterCommentIndex < tokens.Count && !_sufficientLines; afterCommentIndex++)
            {
                Token currentToken = tokens[afterCommentIndex];
                TokenType currentTokenType = currentToken.TokenType;
                if (currentTokenType == Newline) HandleNewline(currentToken);
                else if (currentTokenType.IsCommentType()) HandleComment(currentToken);
                else HandleRegularToken();
                UpdateResult(currentToken);
                _sufficientLines = _linesSoFar >= 3;
            }
            return (afterCommentIndex, _result.ToString(), _remainingComment.ToString().Trim());
        }

        private void UpdateResult(Token currentToken)
        {
            string text = NiceDisplay(currentToken);
            string spacing = Spacing(_previousTokenType, currentToken.TokenType);
            _result.Append(spacing + text);
            _previousTokenType = currentToken.TokenType;
        }

        private void HandleRegularToken()
        {
            _newlineMode = false;
            _stillInComment = false;
        }

        private void HandleComment(Token currentToken)
        {
            if (_stillInComment) _remainingComment.Append(NiceDisplay(currentToken));
            _newlineMode = false;
        }

        private void HandleNewline(Token currentToken)
        {
            if (_stillInComment) _remainingComment.Append(NiceDisplay(currentToken));
            if (_newlineMode && !_stillInComment) _sufficientLines = true;
            if (!_stillInComment) _linesSoFar++;
            _newlineMode = true;
        }
    }

    private static readonly List<TokenType> _noSpaceBefore = new() { BracketsClose, BracketsOpen,
        Comma, Greater, Less, ParenthesesClose, ParenthesesOpen, Period, QuestionMark, Semicolon };

    private static readonly List<TokenType> _noSpaceAfter = new() { BracketsOpen, ExclamationMark,
        Less, Newline, ParenthesesOpen, Period };

    private static string Spacing(TokenType? previousTokenType, TokenType? currentTokenType)
    {
        if (previousTokenType == null) return "";
        TokenType ptt = (TokenType)previousTokenType;
        if (_noSpaceAfter.Contains(ptt)) return "";

        if (currentTokenType == null) return "";
        TokenType ctt = (TokenType)currentTokenType;
        if (_noSpaceBefore.Contains(ctt)) return "";

        return " ";
    }

    private static string NiceDisplay(Token token)
    {
        if (token is ComplexToken ct)
        {
            string baseText = ct.Info;
            if (ct.TokenType == LineComment) baseText = "// " + baseText;
            if (ct.TokenType == BlockCommentWhole) baseText = "/* " + baseText + "*/";
            if (ct.TokenType == BlockCommentEnd) baseText += "*/";
            if (ct.TokenType == BlockCommentStart) baseText = "/* " + baseText;
            if (ct.TokenType == TokenType.String) baseText = $"\"{baseText}\"";
            return baseText;
        }
        else return token.PrettyPrint().ToLower();
    }

    /*
     *  using PhoneShop . Business ; // 1: get
 using PhoneShop . Domain . Interfaces ;
     */

    private class PrecedingContextGetter
    {
        private bool _sufficientLines = false;
        private int _linesSoFar = -1;
        private bool _newlineMode = false;
        private readonly StringBuilder _result = new();
        private TokenType? _followingTokenType = null;

        public string Previous3Lines(int i, IReadOnlyList<Token> tokens)
        {
            for (int beforeCommentIndex = i - 1; beforeCommentIndex >= 0 && !_sufficientLines; beforeCommentIndex--)
            {
                Token currentToken = tokens[beforeCommentIndex];
                if (currentToken.TokenType == TokenType.Newline) HandleNewline();
                else _newlineMode = false;
                _result.Insert(0, NiceDisplay(currentToken) + Spacing(currentToken.TokenType, _followingTokenType));
                _followingTokenType = currentToken.TokenType;
                _sufficientLines = _linesSoFar >= 3;
            }
            return _result.ToString();
        }

        private void HandleNewline()
        {
            if (_newlineMode) _sufficientLines = true;
            _linesSoFar++;
            _newlineMode = true;
        }
    }
}