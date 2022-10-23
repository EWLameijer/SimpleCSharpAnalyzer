using DTOsAndUtilities;
using Tokenizing;
using static Tokenizing.TokenType;

namespace TokenBasedChecking;

public class IdentifierAnalyzer : BaseAnalyzer
{
    private enum FileModus
    { FileModusNotSet, TopLevel, FileScoped, Traditional }

    public IdentifierAnalyzer(FileTokenData fileData, Report report) : base(fileData, report)
    {
        // is this a top level file?
        Console.WriteLine($"{ContextedFilename} is {GetFileModus()}.");
    }

    public void AddWarnings()
    {
        ScanVariables();
    }

    private FileModus GetFileModus()
    {
        if (!Tokens.Any(t => t.TokenType == Namespace)) return FileModus.TopLevel;
        int tokenIndex = Tokens.TakeWhile(t => t.TokenType != Namespace).Count();
        TokenType nextTokenType;
        int indexToScan = tokenIndex;
        do
        {
            indexToScan++;
            nextTokenType = Tokens[indexToScan].TokenType;
        } while (nextTokenType != BracesOpen && nextTokenType != SemiColon);
        return nextTokenType == BracesOpen ? FileModus.Traditional : FileModus.FileScoped;
    }

    private void ScanVariables() //' 28 lines
    {
        List<Token> currentStatement = new();
        bool postBraces = false;
        while (CurrentIndex < Tokens.Count && Tokens[CurrentIndex].TokenType != BracesOpen)
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
        }
    }

    private void HandleStatementEndingWithClosingBraces()
    {
        Scopes.RemoveAt(Scopes.Count - 1);
        CurrentIndex++;
        // handle }). // fluent interface after lambda...
        while (CurrentIndex < Tokens.Count && (Tokens[CurrentIndex].TokenType.IsSkippable() ||
            Tokens[CurrentIndex].TokenType == ParenthesesClose))
            CurrentIndex++;
    }

    private void HandleStatementEndingWithOpeningBraces(List<Token> currentStatement)
    {
        AddScope(currentStatement);
        ProcessPossibleIdentifier(currentStatement);
        CurrentIndex++;
        currentStatement.Clear();
        ScanVariables();
    }
}