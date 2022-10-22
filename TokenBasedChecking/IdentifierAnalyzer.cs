using DTOsAndUtilities;
using Tokenizing;
using static Tokenizing.TokenType;

namespace TokenBasedChecking;

public class IdentifierAnalyzer : BaseAnalyzer
{

    private int _currentIndex = 0;

    private enum FileModus
    { FileModusNotSet, TopLevel, FileScoped, Traditional }

    public IdentifierAnalyzer(FileTokenData fileData, Report report) : base(fileData, report)
    {
        _tokens = fileData.Tokens;

        // is this a top level file?
        Console.WriteLine($"{ContextedFilename} is {GetFileModus()}.");
    }

    public void AddWarnings()
    {
        ScanVariables();
    }

    private FileModus GetFileModus()
    {
        if (!_tokens.Any(t => t.TokenType == Namespace)) return FileModus.TopLevel;
        int tokenIndex = _tokens.TakeWhile(t => t.TokenType != Namespace).Count();
        TokenType nextTokenType;
        int indexToScan = tokenIndex;
        do
        {
            indexToScan++;
            nextTokenType = _tokens[indexToScan].TokenType;
        } while (nextTokenType != BracesOpen && nextTokenType != SemiColon);
        return nextTokenType == BracesOpen ? FileModus.Traditional : FileModus.FileScoped;
    }

    private void ScanVariables() //' 28 lines
    {
        List<Token> currentStatement = new();
        bool postBraces = false;
        while (_currentIndex < _tokens.Count && _tokens[_currentIndex].TokenType != BracesOpen)
        {
            Token? currentToken = LookForNextEndingToken(currentStatement);
            if (currentToken == null) return;
            TokenType currentTokenType = currentToken.TokenType;
            currentStatement.Add(currentToken);
            // type IS semicolon or bracesOpen
            bool? postBracesOrQuit = HandleEndingToken(currentTokenType, currentStatement, postBraces);
            if (postBracesOrQuit == null) return;
        }
        // type IS BracesOpen
    }

    private bool? HandleEndingToken(TokenType currentTokenType, List<Token> currentStatement, bool postBraces)
    {
        switch (currentTokenType)
        {
            case SemiColon:
                HandleStatementEndingWithSemicolon(currentStatement, postBraces);
                return false;

            case BracesClose:
                HandleStatementEndingWithClosingBraces();
                return null;

            default:
                HandleStatementEndingWithOpeningBraces(currentStatement);
                return true;
        };
    }

    private Token? LookForNextEndingToken(List<Token> currentStatement)
    {
        Token currentToken = _tokens[_currentIndex];
        TokenType currentTokenType = currentToken.TokenType;
        while (currentTokenType != SemiColon && currentTokenType != BracesOpen && currentTokenType != BracesClose)
        {
            if (!currentTokenType.IsSkippable()) currentStatement.Add(_tokens[_currentIndex]);
            _currentIndex++;
            if (_currentIndex == _tokens.Count) return null;
            currentToken = _tokens[_currentIndex];
            currentTokenType = currentToken.TokenType;
        }
        return currentToken;
    }

    private void HandleStatementEndingWithSemicolon(List<Token> currentStatement, bool postBraces)
    {
        TokenType currentTokenType = _tokens[_currentIndex].TokenType;
        if (postBraces)
        {
            currentStatement.Clear();
        }
        else if (currentStatement.Count > 0 && currentStatement[0].TokenType == For)
        {
            ProcessForLoopSetup(currentTokenType);
        }
        else ProcessPossibleIdentifier(currentStatement);
        _currentIndex++;
    }

    private void ProcessForLoopSetup(TokenType currentTokenType)
    {
        while (currentTokenType != SemiColon)
        {
            _currentIndex++;
            currentTokenType = _tokens[_currentIndex].TokenType;
        }
        int depth = 0;
        while (currentTokenType != ParenthesesClose || depth > 0)
        {
            if (currentTokenType == ParenthesesOpen) depth++;
            if (currentTokenType == ParenthesesClose) depth--;
            _currentIndex++;
            currentTokenType = _tokens[_currentIndex].TokenType;
        }
    }

    private void HandleStatementEndingWithClosingBraces()
    {
        Scopes.RemoveAt(Scopes.Count - 1);
        _currentIndex++;
        // handle }). // fluent interface after lambda...
        while (_currentIndex < _tokens.Count && (_tokens[_currentIndex].TokenType.IsSkippable() ||
            _tokens[_currentIndex].TokenType == ParenthesesClose))
            _currentIndex++;
    }

    private void HandleStatementEndingWithOpeningBraces(List<Token> currentStatement)
    {
        AddScope(currentStatement);
        ProcessPossibleIdentifier(currentStatement);
        _currentIndex++;
        currentStatement.Clear();
        ScanVariables();
    }
}