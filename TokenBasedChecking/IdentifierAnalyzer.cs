using DTOsAndUtilities;
using Tokenizing;
using static Tokenizing.TokenType;

namespace TokenBasedChecking;

public class IdentifierAnalyzer : BaseAnalyzer
{
    private readonly IReadOnlyList<Token> _tokens;

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

    private void ProcessPossibleIdentifier(List<Token> currentStatement)
    {
        // can be empty through "return {..;"
        bool ready = ParseStraightforwardCases(currentStatement);
        if (ready) return;
        if (DoShow) Show(currentStatement);
        int? pos = CanBeMethod(currentStatement);
        if (pos != null)
        {
            ProcessParameter(currentStatement, pos);
            return;
        }
        CheckVariables(currentStatement);
    }

    private void CheckVariables(List<Token> currentStatement)
    {
        List<TokenType> newBracesStack = new();
        List<TokenType> possibleTypeStack = new();
        for (int i = 0; i < currentStatement.Count; i++)
        {
            bool endLoop = CheckCurrentVariableCandidate(currentStatement, newBracesStack, possibleTypeStack, i);
            if (endLoop) break;
        }
        currentStatement.Clear();
    }

    private bool CheckCurrentVariableCandidate(List<Token> currentStatement,
        List<TokenType> newBracesStack, List<TokenType> possibleTypeStack, int index)
    {
        TokenType tokenType = currentStatement[index].TokenType;
        if (tokenType.IsModifier() || tokenType.IsDeclarer()) return false;
        possibleTypeStack.Add(tokenType);
        if (tokenType.IsOpeningType()) newBracesStack.Add(tokenType);
        else if (tokenType.IsClosingType()) CheckForwardBraces(tokenType, newBracesStack);
        else
        {
            CheckPropertiesAndVariables(currentStatement, newBracesStack, possibleTypeStack, index, tokenType);
            if (tokenType == Assign) return true;
        }
        return false;
    }

    private static bool ParseStraightforwardCases(List<Token> currentStatement)
    {
        if (currentStatement.Count < 2)
        {
            currentStatement.Clear();
            return true;
        }
        TokenType firstType = currentStatement[0].TokenType;

        if (firstType == If || firstType == Else || firstType == ForEach || firstType == Return
            || currentStatement.Any(t => t.TokenType == TokenType.Enum))
        {
            currentStatement.Clear();
            return true;
        }
        return false;
    }

    private void CheckPropertiesAndVariables(List<Token> currentStatement,
        List<TokenType> newBracesStack, List<TokenType> possibleTypeStack, int i, TokenType tokenType)
    {
        if (IsValueName(currentStatement, newBracesStack, possibleTypeStack, i, tokenType))
        {
            ScopeType currentScope = GetCurrentScope();
            TokenType nextType = currentStatement[i + 1].TokenType;
            string varType = (nextType == BracesOpen || nextType == FatArrow) ? "property" : "variable";
            if (!CapitalizationCheck(i, currentStatement, currentScope))
            {
                string warning = $"Invalid {varType} name: " +
                    $"{((ComplexToken)currentStatement[i]).Info} (in {ContextedFilename} - " +
                    $"{PrettyPrint(currentScope)}).";
                Report.Warnings.Add(warning);
            }
        }
    }

    private bool IsValueName(List<Token> currentStatement,
        List<TokenType> newBracesStack, List<TokenType> possibleTypeStack, int i, TokenType tokenType)
    {
        return newBracesStack.Count == 0 && possibleTypeStack.Count(t => t == Identifier) > 1 &&
                !IsCall(currentStatement, i)
                && tokenType == Identifier && currentStatement[i - 1].TokenType != Period
                && !currentStatement.Take(i).Any(t => t.TokenType == Where);
    }

    private ScopeType GetCurrentScope()
    {
        ScopeType currentScope = Scopes.Count > 0 ? Scopes.Last().Type : ScopeType.File;
        int scopeIndex = Scopes.Count - 1;
        while (scopeIndex >= 0 && currentScope == ScopeType.ScopeTypeNotSet)
        {
            if (Scopes[scopeIndex].Type != ScopeType.ScopeTypeNotSet)
                currentScope = Scopes[scopeIndex].Type;
            scopeIndex--;
        }

        return currentScope;
    }

    private void ProcessParameter(List<Token> currentStatement, int? pos)
    {
        int openParenthesisPos = (int)pos + 1;
        List<Token> parameters = GetParameters(currentStatement, openParenthesisPos + 1);
        for (int index = parameters.Count - 1; index > 0; index--)
        {
            Token parameter = parameters[index];
            if (parameter.TokenType == Identifier && (
                index == parameters.Count - 1 || parameters[index + 1].TokenType == Comma))
            {
                CheckParameterName(parameter);
            }
        }
    }

    private void CheckParameterName(Token parameter)
    {
        string paramName = ((ComplexToken)parameter).Info;
        (bool isFinished, char startChar) = GetStartCharAndName(paramName);

        if (!isFinished && !char.IsLower(startChar))
            Report.Warnings.Add($"Invalid parameter name: " +
                $"{paramName} (in {ContextedFilename}).");
    }

    private (bool isFinished, char startChar) GetStartCharAndName(string paramName)
    {
        char startChar = paramName[0];
        if (startChar == '@')
        {
            string normalName = paramName.Substring(1);
            if (!AllCSharpKeywords.KeyWords.Contains(normalName))
            {
                Report.Warnings.Add($"Unnecessary '@' in front of {paramName} " +
                    $"(in {ContextedFilename}).");
                return (true, startChar);
            }
            startChar = paramName[1];
        }
        return (false, startChar);
    }

    private List<Token> GetParameters(List<Token> currentStatement, int openingPos)
    {
        int depth = 0;
        int currentPos = openingPos;
        List<Token> parameters = new();
        while (currentStatement[currentPos].TokenType != ParenthesesClose || depth > 0)
        {
            TokenType currentType = currentStatement[currentPos].TokenType;
            if (currentType == ParenthesesOpen) depth++;
            if (currentType == ParenthesesClose) depth--;
            if (depth == 0 && !currentType.IsSkippable()) parameters.Add(currentStatement[currentPos]);
            currentPos++;
        }
        return parameters;
    }

    private static string PrettyPrint(ScopeType scopeType) => scopeType switch
    {
        ScopeType.File => "top-level-scope",
        ScopeType.ClassRecordStruct => "class/record/struct",
        _ => "method"
    };

    private bool CapitalizationCheck(int i, List<Token> currentStatement, ScopeType scope)
    {
        string identifierName = ((ComplexToken)currentStatement[i]).Info;
        TokenType nextTokenType = currentStatement[i + 1].TokenType;
        if ((nextTokenType == BracesOpen || nextTokenType == FatArrow) || // is property
            currentStatement.Any(t => SuggestsUpperCase(t.TokenType)))
            return char.IsUpper(identifierName[0]);
        if (scope == ScopeType.ClassRecordStruct)
            return identifierName[0] == '_' && char.IsLower(identifierName[1]);
        else
        {
            char startChar = identifierName[0];
            if (startChar == '@')
            {
                string normalName = identifierName.Substring(1);
                if (!AllCSharpKeywords.KeyWords.Contains(normalName))
                {
                    Report.Warnings.Add($"Unnecessary '@' in front of {identifierName} " +
                        $"(in {ContextedFilename}).");
                    return true; // don't give second warning
                }
                startChar = identifierName[1];
            }
            return char.IsLower(startChar);
        }
    }

    private static bool SuggestsUpperCase(TokenType tokenType) =>
        tokenType == Public || tokenType == Protected || tokenType == Const;

    private static void Show(List<Token> currentStatement)
    {
        List<string> readable = currentStatement.Select(t => t.PrettyPrint()).ToList();
        Console.WriteLine("STATEMENT: " + string.Join(" ", readable));
    }
}