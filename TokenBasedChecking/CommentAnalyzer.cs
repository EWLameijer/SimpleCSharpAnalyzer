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
    private readonly int _unapprovedComments = 0;
    private readonly string _basePath;

    public CommentAnalyzer(FileAsTokens fileData, Report report)
    {
        _contextedFilename = fileData.ContextedFilename;
        _filePath = fileData.FilePath;
        _report = report;
        _tokens = fileData.Tokens;
        _basePath = fileData.BasePath;
    }

    public void AddWarnings()
    {
        for (int i = 0; i < _tokens.Count; i++)
        {
            if (_tokens[i].TokenType.IsCommentType())
            {
                i = WarnForCommentsIfNeeded(i);
            }
        }
        if (_unapprovedComments > 0) _report.AddNonScoredWarning(
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
            new FollowingContextGetter().Next3Lines(commentEndPosition + 1, _tokens, _filePath);
        CommentData commentData = new(_contextedFilename, comment, precedingContext, followingContext);
        _report.Comments.Add(commentData);
        return commentEndPosition;
    }

    private int WarnForCommentsIfNeeded(int commentStartIndex)
    {
        (string comment, int lastCommentIndex) = ExtractComment(commentStartIndex);

        WarnForMissingSpace(comment);
        if (comment.Contains("todo", StringComparison.InvariantCultureIgnoreCase))
            _report.AddWarning(AttentionCategory.ToDoComments,
                $"TODO comment in {_contextedFilename}: {comment}");
        else _report.ScoreCorrect(AttentionCategory.ToDoComments);
        if (!CommentArchiver.ContainsComment(_basePath, comment)) _report.AddWarning(
            AttentionCategory.UncheckedComments,
            $"Unapproved comments in {_contextedFilename}, please use the CommentChecker tool.");
        else _report.ScoreCorrect(AttentionCategory.UncheckedComments);
        return lastCommentIndex;
    }

    private void WarnForMissingSpace(string comment)
    {
        bool isCorrect = true;
        if (comment.Trim().Length == 2)
        {
            WarnForEmptyComment();
            isCorrect = false;
        }
        if (NoSpaceAfterCommentOpeningMark(comment))
        {
            WarnForCommentWithoutOpeningSpace(comment);
            isCorrect = false;
        }
        if (isCorrect) _report.ScoreCorrect(AttentionCategory.BadlyFormattedComments);
    }

    private void WarnForCommentWithoutOpeningSpace(string comment)
    {
        _report.AddWarning(AttentionCategory.BadlyFormattedComments,
            $"Need space after comment in {_contextedFilename}: {comment}");
    }

    private void WarnForEmptyComment()
    {
        _report.AddWarning(
            AttentionCategory.BadlyFormattedComments, $"Empty comment in {_contextedFilename}");
    }

    private static bool NoSpaceAfterCommentOpeningMark(string comment) =>
        (comment.StartsWith("///") && comment.Length > 3 && comment[3] != ' ') ||
                     (comment.Length > 2 && !char.IsWhiteSpace(comment[2]) && comment[2] != '/');

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
            if (i >= tokens.Count) return (i, "");
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