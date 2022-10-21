using DTOsAndUtilities;
using Tokenizing;
using static Tokenizing.TokenType;

namespace TokenBasedChecking;

public class IdentifierAnalyzer
{
    private readonly string _contextedFilename;
    private readonly IReadOnlyList<Token> _tokens;
    private readonly Report _report;
    private int _currentIndex = 0;
    private readonly List<Scope> _scopes = new();
    private const bool DoShow = true;

    private enum FileModus
    { FileModusNotSet, TopLevel, FileScoped, Traditional }

    public IdentifierAnalyzer(FileTokenData fileData, Report report)
    {
        _contextedFilename = fileData.ContextedFilename;
        _tokens = fileData.Tokens;

        // is this a top level file?
        Console.WriteLine($"{_contextedFilename} is {GetFileModus()}.");
        _report = report;
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

    private void ScanVariables()
    {
        List<Token> currentStatement = new();
        bool postBraces = false;
        while (_currentIndex < _tokens.Count && _tokens[_currentIndex].TokenType != BracesOpen)
        {
            Token currentToken = _tokens[_currentIndex];
            TokenType currentTokenType = currentToken.TokenType;
            while (currentTokenType != SemiColon && currentTokenType != BracesOpen && currentTokenType != BracesClose)
            {
                if (!currentTokenType.IsSkippable()) currentStatement.Add(_tokens[_currentIndex]);
                _currentIndex++;
                if (_currentIndex == _tokens.Count) return;
                currentToken = _tokens[_currentIndex];
                currentTokenType = currentToken.TokenType;
            }
            currentStatement.Add(currentToken);
            // type IS semicolon or bracesOpen
            if (currentTokenType == SemiColon)
            {
                if (postBraces)
                {
                    currentStatement.Clear();
                    postBraces = false;
                }
                else if (currentStatement.Count > 0 && currentStatement[0].TokenType == For)
                {
                    while (currentTokenType != SemiColon)
                    {
                        _currentIndex++;
                        currentTokenType = _tokens[_currentIndex].TokenType;
                    }
                    int depth = 0;
                    while (currentTokenType != ParenthesesClose && depth > 0)
                    {
                        if (currentTokenType == ParenthesesOpen) depth++;
                        if (currentTokenType == ParenthesesClose) depth--;
                        _currentIndex++;
                        currentTokenType = _tokens[_currentIndex].TokenType;
                    }
                }
                else ProcessPossibleIdentifier(currentStatement);
                _currentIndex++;
            }
            else if (currentTokenType == BracesClose)
            {
                _scopes.RemoveAt(_scopes.Count - 1);
                _currentIndex++;
                // handle }). // fluent interface after lambda...
                while (_currentIndex < _tokens.Count && (_tokens[_currentIndex].TokenType.IsSkippable() ||
                    _tokens[_currentIndex].TokenType == ParenthesesClose))
                    _currentIndex++;
                return;
            }
            else // opening braces
            {
                AddScope(currentStatement);
                ProcessPossibleIdentifier(currentStatement);
                _currentIndex++;
                currentStatement.Clear();
                ScanVariables();
                postBraces = true;
            }
        }
        // type IS BracesOpen
    }

    private void AddScope(List<Token> currentStatement)
    {
        ScopeType scopeType = ScopeType.ScopeTypeNotSet;
        string name = "unknown";
        if (currentStatement.Any(t => t.TokenType.IsTypeType()))
        {
            scopeType = ScopeType.ClassRecordStruct;
            name = ((ComplexToken)currentStatement.First(t => t.TokenType == Identifier)).Info;
        }
        if (currentStatement.Any(t => t.TokenType == New))
        {
            scopeType = ScopeType.New;
        }
        if (CanBeMethod(currentStatement) != null) scopeType = ScopeType.Method;
        ScopeType possibleScopeType = ScopeType.ScopeTypeNotSet;
        if (currentStatement.Count > 0)
        {
            possibleScopeType = currentStatement[0].TokenType switch
            {
                If => ScopeType.If,
                Else => ScopeType.Else,
                ForEach => ScopeType.Foreach,
                For => ScopeType.For,
                Do => ScopeType.Do,
                While => ScopeType.While,
                _ => ScopeType.ScopeTypeNotSet
            };
        }
        if (possibleScopeType != ScopeType.ScopeTypeNotSet) scopeType = possibleScopeType;

        _scopes.Add(new Scope(scopeType, name));
    }

    private void ProcessPossibleIdentifier(List<Token> currentStatement)
    {
        // can be empty through "return {..;"
        if (currentStatement.Count < 2)
        {
            currentStatement.Clear();
            return;
        }
        TokenType firstType = currentStatement[0].TokenType;

        if (firstType == If || firstType == Else || firstType == ForEach || firstType == Return
            || currentStatement.Any(t => t.TokenType == TokenType.Enum))
        {
            currentStatement.Clear();
            return;
        }
        if (DoShow) Show(currentStatement);
        int? pos = CanBeMethod(currentStatement);
        if (pos != null)
        {
            int openParenthesisPos = (int)pos + 1;
            List<Token> parameters = GetParameters(currentStatement, openParenthesisPos + 1);
            for (int index = parameters.Count - 1; index > 0; index--)
            {
                Token parameter = parameters[index];
                if (parameter.TokenType == Identifier && (
                    index == parameters.Count - 1 || parameters[index + 1].TokenType == Comma))
                {
                    string paramName = ((ComplexToken)parameter).Info;
                    char startChar = paramName[0];
                    if (startChar == '@')
                    {
                        string normalName = paramName.Substring(1);
                        if (!AllCSharpKeywords.KeyWords.Contains(normalName))
                        {
                            _report.Warnings.Add($"Unnecessary '@' in front of {paramName} " +
                                $"(in {_contextedFilename}).");
                            return;
                        }
                        startChar = paramName[1];
                    }
                    if (!char.IsLower(startChar))
                        _report.Warnings.Add($"Invalid parameter name: " +
                            $"{paramName} (in {_contextedFilename}).");
                }
            }
            return;
        }
        List<TokenType> newBracesStack = new();
        List<TokenType> possibleTypeStack = new();
        for (int i = 0; i < currentStatement.Count; i++)
        {
            TokenType tokenType = currentStatement[i].TokenType;
            if (tokenType.IsModifier() || tokenType.IsDeclarer()) continue;
            possibleTypeStack.Add(tokenType);
            if (tokenType.IsOpeningType()) newBracesStack.Add(tokenType);
            else if (tokenType.IsClosingType()) SharedUtils.CheckForwardBraces(tokenType, newBracesStack);
            else
            {
                if (newBracesStack.Count == 0 && possibleTypeStack.Count(t => t == Identifier) > 1 &&
                !SharedUtils.IsCall(currentStatement, i)
                && tokenType == Identifier && currentStatement[i - 1].TokenType != Period
                && !currentStatement.Take(i).Any(t => t.TokenType == Where))
                {
                    ScopeType currentScope = _scopes.Count > 0 ? _scopes.Last().Type : ScopeType.File;
                    int scopeIndex = _scopes.Count - 1;
                    while (scopeIndex >= 0 && currentScope == ScopeType.ScopeTypeNotSet)
                    {
                        if (_scopes[scopeIndex].Type != ScopeType.ScopeTypeNotSet)
                            currentScope = _scopes[scopeIndex].Type;
                        scopeIndex--;
                    }
                    TokenType nextType = currentStatement[i + 1].TokenType;
                    string varType = (nextType == BracesOpen || nextType == FatArrow) ? "property" : "variable";
                    if (!CapitalizationCheck(i, currentStatement, currentScope))
                    {
                        string warning = $"Invalid {varType} name: " +
                            $"{((ComplexToken)currentStatement[i]).Info} (in {_contextedFilename} - " +
                            $"{PrettyPrint(currentScope)}).";
                        // Console.WriteLine("***" + warning);
                        _report.Warnings.Add(warning);
                    }
                }
                if (tokenType == Assign) break;
            }
        }
        currentStatement.Clear();
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
                    _report.Warnings.Add($"Unnecessary '@' in front of {identifierName} " +
                        $"(in {_contextedFilename}).");
                    return true; // don't give second warning
                }
                startChar = identifierName[1];
            }
            return char.IsLower(startChar);
        }
    }

    private static bool SuggestsUpperCase(TokenType tokenType) =>
        tokenType == Public || tokenType == Protected || tokenType == Const;

    private bool IsDirectCall(List<Token> currentStatement, int i)
    {
        if (i == currentStatement.Count - 1) return false;
        TokenType nextType = currentStatement[i + 1].TokenType;
        return nextType == ParenthesesOpen;
    }

    private static void Show(List<Token> currentStatement)
    {
        List<string> readable = currentStatement.Select(t => t.PrettyPrint()).ToList();
        Console.WriteLine("STATEMENT: " + string.Join(" ", readable));
    }

    private int? CanBeMethod(List<Token> currentStatement)
    {
        List<TokenType> newBracesStack = new();
        List<TokenType> possibleTypeStack = new();
        for (int i = 0; i < currentStatement.Count; i++)
        {
            TokenType tokenType = currentStatement[i].TokenType;
            if (tokenType.IsModifier() || tokenType.IsDeclarer()) continue;
            possibleTypeStack.Add(tokenType);
            if (tokenType.IsOpeningType()) newBracesStack.Add(tokenType);
            else if (tokenType.IsClosingType()) SharedUtils.CheckForwardBraces(tokenType, newBracesStack);
            else
            {
                TokenType? prevTokenType = i > 0 ? currentStatement[i - 1].TokenType : null;
                if (newBracesStack.Count == 0 && (possibleTypeStack.Count(t => t == Identifier) > 1 ||
                    possibleTypeStack.Count(t => t == Identifier) == 1 &&
                    SharedUtils.RepresentsClassName(currentStatement[i], _scopes)) &&
                    IsDirectCall(currentStatement, i) && tokenType == Identifier && prevTokenType != Period)
                {
                    string methodName = ((ComplexToken)currentStatement[i]).Info;
                    if (DoShow) Console.WriteLine($"Candidate method: {methodName}");
                    if (!char.IsUpper(methodName[0])) _report.Warnings.Add(
                        $"Invalid method name: {methodName} (in {_contextedFilename}).");
                    return i;
                }
                if (tokenType == Assign) break;
            }
        }
        return null;
    }
}