using DTOsAndUtilities;
using Tokenizing;
using static Tokenizing.TokenType;
using Scope = DTOsAndUtilities.Scope;

namespace TokenBasedChecking;

public class IdentifierAndMethodLengthAnalyzer
{
    private enum FileModus
    { FileModusNotSet, TopLevel, FileScoped, Traditional }

    private readonly List<(string, int)> _methodNames = new();
    private readonly string _contextedFilename;
    private readonly Report _report;
    protected const bool DoShow = false;
    private readonly List<Scope> _scopes = new();
    private readonly IReadOnlyList<Token> _tokens;
    private int _currentIndex = 0;

    protected TokenType CurrentTokenType() => _tokens[_currentIndex].TokenType;

    protected Token CurrentToken() => _tokens[_currentIndex];

    protected void Proceed() => _currentIndex++;

    public void AddWarnings()
    {
        ScanMethods();
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

    private void ScanMethods()
    {
        List<Token> currentStatement = new();
        while (_currentIndex < _tokens.Count)
        {
            TokenType currentTokenType = CurrentTokenType();
            if (!currentTokenType.IsSkippable())
            {
                if (currentTokenType == SemiColon)
                {
                    currentStatement.Add(CurrentToken());
                    HandleStatement(currentStatement);
                }
                else if (currentTokenType == BracesOpen)
                {
                    bool isBlockStatement = IsBlockStatement(currentStatement);
                    currentStatement.Add(CurrentToken());
                    _currentIndex++;
                    AddScope(currentStatement);
                    UpdateMethodNames(currentStatement);
                    ScanMethods();
                    _scopes.RemoveAt(_scopes.Count - 1);
                    IfMethodScopeEndsCheckMethodLength();
                    currentStatement.Add(CurrentToken()); // should be }
                    if (!isBlockStatement) HandleStatement(currentStatement);
                }
                else if (currentTokenType == BracesClose)
                {
                    return;
                }
                else
                {
                    currentStatement.Add(CurrentToken());
                }
            }
            Proceed();
        }
    }

    private void UpdateMethodNames(List<Token> currentStatement)
    {
        int? methodIndex = CanBeMethod(currentStatement, true);
        if (methodIndex != null)
        {
            string methodName = ((ComplexToken)currentStatement[(int)methodIndex]).Info;
            _methodNames.Add((methodName, _currentIndex));
        }
        else
        {
            _methodNames.Add(("none", _currentIndex));
        }
    }

    private void IfMethodScopeEndsCheckMethodLength()
    {
        (string methodName, int tokenIndex) = _methodNames[_methodNames.Count - 1];
        if (methodName != "none")
        {
            int lineCount = CountLines(tokenIndex, _currentIndex);
            if (lineCount > 15)
            {
                _report.Warnings.Add($"Too long method: {methodName} " +
                    $"(in {_contextedFilename}) is {lineCount} lines long.");
                _report.ExtraCodeLines += lineCount - 15;
            }
        }
        _methodNames.RemoveAt(_methodNames.Count - 1);
    }

    private int CountLines(int startIndex, int endIndex)
    {
        int newlineCount = 0;
        bool newlineMode = false;
        for (int i = startIndex; i < endIndex; i++)
        {
            if (_tokens[i].TokenType == NewLine)
            {
                if (!newlineMode) newlineCount++;
                newlineMode = true;
            }
            else newlineMode = false;
        }
        return newlineCount + 1;// closing brace is also a line
    }

    protected Token? LookForNextEndingToken(List<Token> currentStatement)
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

    protected static bool IsBlockStatement(List<Token> currentStatement)
    {
        if (currentStatement.Select(t => t.TokenType).Any(tt => tt == Do || tt == New)) return true;
        if (currentStatement.Count < 1) return false;
        TokenType previousToken = currentStatement[^1].TokenType;
        if (previousToken == FatArrow || previousToken == Switch) return true;
        return false;
    }

    protected void HandleStatement(List<Token> currentStatement)
    {
        if (DoShow) Show(currentStatement);
        HandleStatementEndingWithSemicolon(currentStatement);
        currentStatement.Clear();
    }

    protected void HandleStatementEndingWithSemicolon(List<Token> currentStatement, bool postBraces = false)
    {
        TokenType currentTokenType = _tokens[_currentIndex].TokenType;
        if (currentStatement.Count > 0 && currentStatement[0].TokenType == For)
        {
            ProcessForLoopSetup(currentTokenType);
        }
        else ProcessPossibleIdentifier(currentStatement);
    }

    protected void HandleClosingBraceWithPossibleClosingParenthesis()
    {
        while (_currentIndex < _tokens.Count && (_tokens[_currentIndex].TokenType.IsSkippable() ||
            _tokens[_currentIndex].TokenType == ParenthesesClose))
            _currentIndex++;
    }

    protected void ProcessForLoopSetup(TokenType currentTokenType)
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

    public IdentifierAndMethodLengthAnalyzer(FileTokenData fileData, Report report)
    {
        _contextedFilename = fileData.ContextedFilename;
        _report = report;
        _tokens = fileData.Tokens;
        Console.WriteLine($"{_contextedFilename} is {GetFileModus()}.");
    }

    internal bool IsCall(List<Token> currentStatement, int i)
    {
        if (i == currentStatement.Count - 1) return false;
        TokenType nextType = currentStatement[i + 1].TokenType;
        return nextType == ParenthesesOpen || nextType == Period;
    }

    internal static void CheckForwardBraces(TokenType tokenType, List<TokenType> bracesStack)
    {
        TokenType topBrace = bracesStack.Last();
        int lastIndex = bracesStack.Count - 1;
        if (tokenType == Greater && topBrace != Less) return; // something like "Assert.True(a>b)"
        RemoveFishhookOpenWhichActuallyMeansLess(tokenType, bracesStack, ref topBrace, ref lastIndex);
        if (BracesArePaired(tokenType, topBrace))
        {
            bracesStack.RemoveAt(lastIndex);
        }
        else
        {
            throw new ArgumentException("Parsing error!");
        }
    }

    private static bool BracesArePaired(TokenType tokenType, TokenType topBrace) =>
        (tokenType == ParenthesesClose && topBrace == ParenthesesOpen) ||
        (tokenType == BracketsClose && topBrace == BracketsOpen) ||
        (tokenType == BracesClose && topBrace == BracesOpen) ||
        (tokenType == Greater && topBrace == Less);

    private static void RemoveFishhookOpenWhichActuallyMeansLess(TokenType tokenType,
        List<TokenType> bracesStack, ref TokenType topBrace, ref int lastIndex)
    {
        while (topBrace == Less && tokenType != Greater)
        {
            bracesStack.RemoveAt(lastIndex);
            topBrace = bracesStack.Last();
            lastIndex = bracesStack.Count - 1;
        }
    }

    internal bool RepresentsClassName(Token token, IReadOnlyList<Scope> scopes)
    {
        if (token.TokenType != Identifier) return false;
        string id = ((ComplexToken)token).Info;
        for (int i = scopes.Count - 1; i >= 0; i--)
        {
            if (scopes[i].Type == ScopeType.ClassRecordStruct) return scopes[i].Name == id;
        }
        return false;
    }

    internal void AddScope(List<Token> currentStatement)
    {
        Scope basicScope = UpdateScopeWithPossibleTypeDeclaration(currentStatement);
        if (basicScope.Type != ScopeType.ScopeTypeNotSet)
        {
            _scopes.Add(basicScope);
        }
        else
        {
            AddNonTypeScope(currentStatement);
        }
    }

    private void AddNonTypeScope(List<Token> currentStatement)
    {
        ScopeType scopeType = ScopeType.ScopeTypeNotSet;
        string name = "unknown";
        if (currentStatement.Any(t => t.TokenType == New))
        {
            scopeType = ScopeType.New;
        }
        if (CanBeMethod(currentStatement, true) != null) scopeType = ScopeType.Method;
        ScopeType possibleScopeType = ScopeType.ScopeTypeNotSet;
        possibleScopeType = GetBackupScopeType(currentStatement, possibleScopeType);
        if (possibleScopeType != ScopeType.ScopeTypeNotSet) scopeType = possibleScopeType;

        _scopes.Add(new Scope { Type = scopeType, Name = name });
    }

    private ScopeType GetBackupScopeType(List<Token> currentStatement, ScopeType possibleScopeType)
    {
        if (currentStatement.Count > 0)
        {
            possibleScopeType = GetAlternativeScopeType(currentStatement);
        }

        return possibleScopeType;
    }

    private ScopeType GetAlternativeScopeType(List<Token> currentStatement)
    {
        return currentStatement[0].TokenType switch
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

    private Scope UpdateScopeWithPossibleTypeDeclaration(List<Token> currentStatement)
    {
        if (currentStatement.Any(t => t.TokenType.IsTypeType()))
        {
            return new Scope
            {
                Type = ScopeType.ClassRecordStruct,
                Name = ((ComplexToken)currentStatement.First(t => t.TokenType == Identifier)).Info
            };
        }
        return new Scope { Type = ScopeType.ScopeTypeNotSet, Name = "unknown" };
    }

    public int? CanBeMethod(List<Token> currentStatement, bool onlyParsing)
    {
        List<TokenType> newBracesStack = new();
        List<TokenType> possibleTypeStack = new();
        for (int i = 0; i < currentStatement.Count; i++)
        {
            TokenType tokenType = currentStatement[i].TokenType;
            if (tokenType.IsModifier() || tokenType.IsDeclarer()) continue;
            possibleTypeStack.Add(tokenType);
            (int? foundIndex, bool done) =
                CheckMethodCloser(tokenType, newBracesStack, currentStatement, possibleTypeStack,
                i, onlyParsing);
            if (foundIndex != null || done) return foundIndex;
        }
        return null;
    }

    private (int? foundIndex, bool done) CheckMethodCloser(TokenType tokenType, List<TokenType> newBracesStack,
        List<Token> currentStatement, List<TokenType> possibleTypeStack, int i, bool onlyParsing)
    {
        if (tokenType.IsOpeningType()) newBracesStack.Add(tokenType);
        else if (tokenType.IsClosingType()) CheckForwardBraces(tokenType, newBracesStack);
        else
        {
            return CheckWhetherLineIsMethod(currentStatement, i, newBracesStack, possibleTypeStack,
                onlyParsing);
        }
        return (null, false);
    }

    private (int? foundIndex, bool done) CheckWhetherLineIsMethod(List<Token> currentStatement, int i,
        List<TokenType> newBracesStack, List<TokenType> possibleTypeStack, bool onlyParsing)
    {
        TokenType tokenType = currentStatement[i].TokenType;
        if (TokenIsMethodName(currentStatement, i, newBracesStack,
            possibleTypeStack, tokenType))
        {
            string methodName = ((ComplexToken)currentStatement[i]).Info;
            if (DoShow) Console.WriteLine($"Candidate method: {methodName}");
            if (!onlyParsing &&
                !char.IsUpper(methodName[0])) _report.Warnings.Add(
                $"Invalid method name: {methodName} (in {_contextedFilename}).");
            return (i, true);
        }
        if (tokenType == Assign) return (null, true);
        return (null, false);
    }

    private bool TokenIsMethodName(List<Token> currentStatement, int i, List<TokenType> newBracesStack,
        List<TokenType> possibleTypeStack, TokenType tokenType)
    {
        TokenType? prevTokenType = i > 0 ? currentStatement[i - 1].TokenType : null;
        return newBracesStack.Count == 0 && (possibleTypeStack.Count(t => t == Identifier) > 1 ||
                    possibleTypeStack.Count(t => t == Identifier) == 1 &&
                    RepresentsClassName(currentStatement[i], _scopes)) &&
                    IsDirectCall(currentStatement, i) && tokenType == Identifier && prevTokenType != Period;
    }

    private static bool IsDirectCall(List<Token> currentStatement, int i)
    {
        if (i == currentStatement.Count - 1) return false;
        TokenType nextType = currentStatement[i + 1].TokenType;
        return nextType == ParenthesesOpen;
    }

    protected void ProcessPossibleIdentifier(List<Token> currentStatement)
    {
        // can be empty through "return {..;"
        bool ready = ParseStraightforwardCases(currentStatement);
        if (ready) return;
        if (DoShow) Show(currentStatement);
        int? pos = CanBeMethod(currentStatement, false);
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
    }

    private bool CheckCurrentVariableCandidate(List<Token> currentStatement,
    List<TokenType> newBracesStack, List<TokenType> possibleTypeStack, int index)
    {
        TokenType tokenType = currentStatement[index].TokenType;
        if (tokenType.IsModifier() || tokenType.IsDeclarer()) return false;
        possibleTypeStack.Add(tokenType);
        if (tokenType == Assign) return true;
        if (tokenType.IsOpeningType()) newBracesStack.Add(tokenType);
        else if (tokenType.IsClosingType()) CheckForwardBraces(tokenType, newBracesStack);
        else
        {
            if (VariableNameAtThisLocation(currentStatement, newBracesStack,
                possibleTypeStack, index, tokenType)) return true;
        }
        return false;
    }

    private bool VariableNameAtThisLocation(List<Token> currentStatement,
    List<TokenType> newBracesStack, List<TokenType> possibleTypeStack, int i, TokenType tokenType)
    {
        if (IsValueName(currentStatement, newBracesStack, possibleTypeStack, i, tokenType))
        {
            ScopeType currentScope = GetCurrentScope();
            TokenType nextType = currentStatement[i + 1].TokenType;
            string varType = (nextType == BracesOpen || nextType == FatArrow) ? "property" : "variable";
            CheckCorrectCapitalization(currentStatement, i, currentScope, varType);
            return true;
        }
        return false;
    }

    private void CheckCorrectCapitalization(List<Token> currentStatement, int i,
        ScopeType currentScope, string varType)
    {
        if (!CapitalizationCheck(i, currentStatement, currentScope))
        {
            string warning = $"Invalid {varType} name: " +
                $"{((ComplexToken)currentStatement[i]).Info} (in {_contextedFilename} - " +
                $"{PrettyPrint(currentScope)}).";
            _report.Warnings.Add(warning);
        }
    }

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
            return CheckLocalVariable(identifierName);
        }
    }

    private bool CheckLocalVariable(string identifierName)
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

    private static bool SuggestsUpperCase(TokenType tokenType) =>
    tokenType == Public || tokenType == Protected || tokenType == Const;

    private static string PrettyPrint(ScopeType scopeType) => scopeType switch
    {
        ScopeType.File => "top-level-scope",
        ScopeType.ClassRecordStruct => "class/record/struct",
        _ => "method"
    };

    private ScopeType GetCurrentScope()
    {
        ScopeType currentScope = _scopes.Count > 0 ? _scopes.Last().Type : ScopeType.File;
        int scopeIndex = _scopes.Count - 1;
        while (scopeIndex >= 0 && !currentScope.IsFoundational())
        {
            if (_scopes[scopeIndex].Type != ScopeType.ScopeTypeNotSet)
                currentScope = _scopes[scopeIndex].Type;
            scopeIndex--;
        }

        return currentScope;
    }

    private bool IsValueName(List<Token> currentStatement,
    List<TokenType> newBracesStack, List<TokenType> possibleTypeStack, int i, TokenType tokenType)
    {
        return newBracesStack.Count == 0 && possibleTypeStack.Count(t => t == Identifier) > 1 &&
                !IsCall(currentStatement, i)
                && tokenType == Identifier && currentStatement[i - 1].TokenType != Period
                && !currentStatement.Take(i).Any(t => t.TokenType == Where);
    }

    private static void Show(List<Token> currentStatement)
    {
        List<string> readable = currentStatement.Select(t => t.PrettyPrint()).ToList();
        Console.WriteLine("STATEMENT: " + string.Join(" ", readable));
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

    protected void ProcessParameter(List<Token> currentStatement, int? pos)
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
            _report.Warnings.Add($"Invalid parameter name: " +
                $"{paramName} (in {_contextedFilename}).");
    }

    private (bool isFinished, char startChar) GetStartCharAndName(string paramName)
    {
        char startChar = paramName[0];
        if (startChar == '@')
        {
            string normalName = paramName.Substring(1);
            if (!AllCSharpKeywords.KeyWords.Contains(normalName))
            {
                _report.Warnings.Add($"Unnecessary '@' in front of {paramName} " +
                    $"(in {_contextedFilename}).");
                return (true, startChar);
            }
            startChar = paramName[1];
        }
        return (false, startChar);
    }

    protected static bool ParseStraightforwardCases(List<Token> currentStatement)
    {
        if (currentStatement.Count < 2)
        {
            return true;
        }
        TokenType firstType = currentStatement[0].TokenType;

        if (firstType == If || firstType == Else || firstType == ForEach || firstType == Return
            || firstType == While || currentStatement.Any(t => t.TokenType == TokenType.Enum))
        {
            return true;
        }
        return false;
    }
}