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
    private int _unapprovedComments = 0;
    private readonly string _basePath;

    public CommentAnalyzer(FileAsTokens fileData, Report report)
    {
        _contextedFilename = fileData.ContextedFilename;
        _filePath = fileData.FilePath;
        _report = report;
        _tokens = fileData.Tokens;
        _basePath = fileData.BasePath;
    }

    //  $"Commented-out code in {_contextedFilename}: {context}");
    public void AddWarnings()
    {
        for (int i = 0; i < _tokens.Count; i++)
        {
            if (_tokens[i].TokenType.IsCommentType())
            {
                i = WarnForCommentsIfNeeded(i);
            }
        }
        if (_unapprovedComments > 0) _report.Warnings.Add(
            $"Unapproved comments in {_contextedFilename}, please use the CommentChecker tool.");
    }

    public void AddCommentAnalysis()
    {
        for (int i = 0; i < _tokens.Count; i++)
        {
            if (_tokens[i].TokenType.IsCommentType())
            {
                i = HandleComment(i);
            }
        }
    }

    private int HandleComment(int commentStartPosition)
    {
        string precedingContext = new PrecedingContextGetter().Previous3Lines(commentStartPosition, _tokens, _filePath);
        (string comment, int commentEndPosition) = ExtractComment(commentStartPosition);
        (commentEndPosition, string followingContext) =
            new FollowingContextGetter().Next3Lines(commentEndPosition, _tokens, _filePath);
        CommentData commentData = new(_contextedFilename, comment, precedingContext, followingContext);
        _report.Comments.Add(commentData);
        return commentEndPosition;
    }

    private int WarnForCommentsIfNeeded(int commentStartIndex)
    {
        (string comment, int lastCommentIndex) = ExtractComment(commentStartIndex);
        if (comment.Contains("todo", StringComparison.InvariantCultureIgnoreCase))
            _report.Warnings.Add($"TODO comment in {_contextedFilename}: {comment}");
        if (!CommentArchiver.ContainsComment(_basePath, comment)) _unapprovedComments++;
        return lastCommentIndex;
    }

    private (string comment, int lastCommentIndex) ExtractComment(int commentStartPosition)
    {
        int currentPos = commentStartPosition;
        while (currentPos < _tokens.Count && (_tokens[currentPos].TokenType.IsCommentType() ||
            _tokens[currentPos].TokenType == Newline)) currentPos++;
        Token? endToken = currentPos == _tokens.Count ? null : _tokens[currentPos];
        string comment = GetStringFromFile(_filePath, _tokens[commentStartPosition], endToken);
        return (comment, currentPos - 1);
    }

    private class FollowingContextGetter
    {
        private bool _sufficientLines = false;
        private int _linesSoFar = 0;
        private bool _newlineMode = false;
        private int? _internalComment = null;

        public (int i, string following) Next3Lines(int i,
            IReadOnlyList<Token> tokens, string filePath)
        {
            int afterCommentIndex = i;
            for (; afterCommentIndex < tokens.Count && !_sufficientLines; afterCommentIndex++)
            {
                Token currentToken = tokens[afterCommentIndex];
                TokenType currentTokenType = currentToken.TokenType;
                if (currentTokenType == Newline) HandleNewline();
                else if (currentTokenType.IsCommentType()) HandleComment(afterCommentIndex);
                else _newlineMode = false;
                _sufficientLines = _linesSoFar >= 3;
            }
            int nextIndex = (_internalComment ?? afterCommentIndex) - 1;
            return (nextIndex, GetStringFromFile(filePath, tokens[i], tokens[afterCommentIndex - 1]));
        }

        private void HandleComment(int currentIndex)
        {
            _internalComment ??= currentIndex;
            _newlineMode = false;
        }

        private void HandleNewline()
        {
            if (_newlineMode) _sufficientLines = true;
            _newlineMode = true;
            _linesSoFar++;
        }
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

    private static string GetStringFromFile(string filePath, Token startToken, Token? endToken)
    {
        if (startToken.LineNumber == endToken?.LineNumber &&
            startToken.CharacterIndex == endToken?.CharacterIndex) return "";
        string[] lines = File.ReadAllLines(filePath);
        StringBuilder result = new();
        result.Append(lines[startToken.LineNumber][startToken.CharacterIndex..] + '\n');
        for (int i = startToken.LineNumber + 1; i < (endToken?.LineNumber ?? lines.Length); i++)
        {
            result.Append(lines[i] + '\n');
        }
        if (endToken != null) result.Append(lines[endToken.LineNumber][..endToken.CharacterIndex]);
        return result.ToString();
    }
}