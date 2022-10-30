using System.Text;
using DTOsAndUtilities;
using Tokenizing;

using static Tokenizing.TokenType;

namespace TokenBasedChecking;

internal record Position(int LineIndex, int CharIndex);

public class CommentAnalyzer
{
    private readonly string _contextedFilename;
    private readonly Report _report;
    private readonly IReadOnlyList<Token> _tokens;
    private readonly string _filePath;

    public CommentAnalyzer(FileAsTokens fileData, Report report)
    {
        _contextedFilename = fileData.ContextedFilename;
        _filePath = fileData.FilePath;
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
        string precedingContext = new PrecedingContextGetter().Previous3Lines(i, _tokens, _filePath);
        while (_tokens[i].TokenType.IsCommentType() || _tokens[i].TokenType == Newline) i++;
        string comment = GetStringFromFile(_filePath, _tokens[originalI], _tokens[i]);
        (i, string followingContext) = new FollowingContextGetter().Next3Lines(i, _tokens, _filePath);
        CommentData commentData = new(_contextedFilename, comment, precedingContext, followingContext);
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

        public (int i, string following) Next3Lines(int i,
            IReadOnlyList<Token> tokens, string filePath)
        {
            int afterCommentIndex = i;
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
            afterCommentIndex--;
            return (afterCommentIndex, GetStringFromFile(filePath, tokens[i], tokens[afterCommentIndex]));
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

    private class PrecedingContextGetter
    {
        private bool _sufficientLines = false;
        private int _linesSoFar = -1;
        private bool _newlineMode = false;

        public string Previous3Lines(int i, IReadOnlyList<Token> tokens, string filePath)
        {
            Token startCommentToken = tokens[i];
            int beforeCommentIndex = i - 1;
            for (; beforeCommentIndex >= 0 && !_sufficientLines; beforeCommentIndex--)
            {
                Token currentToken = tokens[beforeCommentIndex];
                if (currentToken.TokenType == Newline) HandleNewline();
                else _newlineMode = false;
                _sufficientLines = _linesSoFar >= 3;
            }
            beforeCommentIndex++;
            Token startContextToken = tokens[beforeCommentIndex];
            return GetStringFromFile(filePath, startContextToken, startCommentToken);
        }

        private void HandleNewline()
        {
            if (_newlineMode) _sufficientLines = true;
            _linesSoFar++;
            _newlineMode = true;
        }
    }

    private static string GetStringFromFile(string filePath, Token startToken, Token endToken)
    {
        if (startToken.LineNumber == endToken.LineNumber &&
            startToken.CharacterIndex == endToken.CharacterIndex) return "";
        string[] lines = File.ReadAllLines(filePath);
        StringBuilder result = new();
        result.Append(lines[startToken.LineNumber][startToken.CharacterIndex..] + '\n');
        for (int i = startToken.LineNumber + 1; i < endToken.LineNumber; i++)
        {
            result.Append(lines[i] + '\n');
        }
        result.Append(lines[endToken.LineNumber][..endToken.CharacterIndex]);
        return result.ToString();
    }
}