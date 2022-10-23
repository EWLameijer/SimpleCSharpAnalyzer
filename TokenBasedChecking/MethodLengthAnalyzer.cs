using DTOsAndUtilities;
using Tokenizing;
using static Tokenizing.TokenType;

namespace TokenBasedChecking;

public class MethodLengthAnalyzer : BaseAnalyzer
{
    private readonly List<(string, int)> _methodNames = new();

    public MethodLengthAnalyzer(FileTokenData fileData, Report report) : base(fileData, report)
    {
    }

    public void AddWarnings()
    {
        ScanMethods();
    }

    // the algorithm is as follows:
    // 1. At a certain point, you have opening braces "{"
    // 2. If those belong to a method, count the opening braces as one line.
    // 3. Scan the rest of the file for the belonging closing braces
    // 4. if you have the matching bracepairs, consolidate subsequent newlines into a single one
    // 5. count the newlines = number of lines, return that value!

    private void ScanMethods()
    {
        List<Token> currentStatement = new();
        bool postBraces = false;
        while (CurrentIndex < Tokens.Count && Tokens[CurrentIndex].TokenType != BracesOpen)
        {
            Token? currentToken = LookForNextEndingToken(currentStatement);
            if (currentToken == null) return;
            TokenType currentTokenType = currentToken.TokenType;
            currentStatement.Add(currentToken);
            bool? processResult = HandleStatement(currentStatement, postBraces, currentTokenType);
            if (processResult == null) return; else postBraces = (bool)processResult;
        }
    }

    private bool? HandleStatement(List<Token> currentStatement, bool postBraces,
        TokenType currentTokenType)
    {
        if (currentTokenType == SemiColon)
        {
            HandleStatementEndingInSemicolon(currentStatement, postBraces, currentTokenType);
            return false;
        }
        else if (currentTokenType == BracesClose)
        {
            HandleStatementEndingInClosingBraces();
            return null;
        }
        else // opening braces
        {
            HandleStatementEndingInOpeningBraces(currentStatement);
            return true;
        }
    }

    private void HandleStatementEndingInOpeningBraces(List<Token> currentStatement)
    {
        AddScope(currentStatement);
        UpdateMethodNames(currentStatement);
        CurrentIndex++;
        currentStatement.Clear();
        ScanMethods();
    }

    private void UpdateMethodNames(List<Token> currentStatement)
    {
        int? methodIndex = CanBeMethod(currentStatement);
        if (methodIndex != null)
        {
            string methodName = ((ComplexToken)currentStatement[(int)methodIndex]).Info;
            _methodNames.Add((methodName, CurrentIndex));
        }
        else
        {
            _methodNames.Add(("none", CurrentIndex));
        }
    }

    private void HandleStatementEndingInClosingBraces()
    {
        IfMethodScopeEndsCheckMethodLength();
        Scopes.RemoveAt(Scopes.Count - 1);
        CurrentIndex++;
        // duplicate code!
        HandleClosingBraceWithPossibleClosingParenthesis();
    }

    private void IfMethodScopeEndsCheckMethodLength()
    {
        (string methodName, int tokenIndex) = _methodNames[_methodNames.Count - 1];
        if (methodName != "none")
        {
            int lineCount = CountLines(tokenIndex, CurrentIndex);
            if (lineCount > 15)
            {
                Report.Warnings.Add($"Too long method: {methodName} " +
                    $"(in {ContextedFilename}) is {lineCount} lines long.");
                Report.ExtraCodeLines += lineCount - 15;
            }
        }
        _methodNames.RemoveAt(_methodNames.Count - 1);
    }

    private void HandleStatementEndingInSemicolon(List<Token> currentStatement,
        bool postBraces, TokenType currentTokenType)
    {
        if (postBraces)
        {
            currentStatement.Clear();
        }
        else if (currentStatement.Count > 0 && currentStatement[0].TokenType == For)
        {
            HandleForStatement(currentTokenType);
        }
        else ProcessPossibleIdentifier(currentStatement);
        CurrentIndex++;
    }

    private void HandleForStatement(TokenType currentTokenType)
    {
        while (currentTokenType != SemiColon)
        {
            CurrentIndex++;
            currentTokenType = Tokens[CurrentIndex].TokenType;
        }
        int depth = 0;
        while (currentTokenType != ParenthesesClose || depth > 0)
        {
            if (currentTokenType == ParenthesesOpen) depth++;
            if (currentTokenType == ParenthesesClose) depth--;
            CurrentIndex++;
            currentTokenType = Tokens[CurrentIndex].TokenType;
        }
    }

    private int CountLines(int startIndex, int endIndex)
    {
        int newlineCount = 0;
        bool newlineMode = false;
        for (int i = startIndex; i < endIndex; i++)
        {
            if (Tokens[i].TokenType == NewLine)
            {
                if (!newlineMode) newlineCount++;
                newlineMode = true;
            }
            else newlineMode = false;
        }
        return newlineCount + 1;// closing brace is also a line
    }
}