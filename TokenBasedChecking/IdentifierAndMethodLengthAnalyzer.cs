using System.Diagnostics;
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
    private readonly FileModus _fileModus;
    private bool _warningsOn = true;

    private void AddWarning(AttentionCategory category, string warning)
    {
        if (_warningsOn) _report.AddWarning(category, warning);
    }

    protected TokenType CurrentTokenType() => _tokens[_currentIndex].TokenType;

    protected Token CurrentToken() => _tokens[_currentIndex];

    protected void Proceed() => _currentIndex++;

    public IdentifierAndMethodLengthAnalyzer(FileAsTokens fileData, Report report)
    {
        _contextedFilename = fileData.ContextedFilename;
        _report = report;
        _tokens = fileData.Tokens;
        _fileModus = GetFileModus();
        Console.WriteLine($"{_contextedFilename} is {GetFileModus()}.");
    }

    public void AddWarnings()
    {
        if (_fileModus == FileModus.TopLevel)
        {
            _warningsOn = false; // to use method detection without duplicate calls
            new TopLevelScanner(_tokens, _contextedFilename, _report, this).PerformTopLevelScan();
            _warningsOn = true;
        }
        Scan();
    }

    private void Scan()
    {
        List<Token> currentStatement = new();

        while (_currentIndex < _tokens.Count)
        {
            TokenType currentTokenType = CurrentTokenType();
            if (currentTokenType == BracesClose) return;
            HandleRegularToken(currentStatement, currentTokenType);
            Proceed();
        }
    }

    private sealed class TopLevelScanner
    {
        private int _linesSoFar = 0;

        private readonly List<Token> _currentLine = new();
        private readonly IReadOnlyList<Token> _tokens;
        private readonly string _contextedFilename;
        private readonly Report _report;
        private readonly IdentifierAndMethodLengthAnalyzer _outer;

        public TopLevelScanner(IReadOnlyList<Token> tokens, string contextedFilename,
            Report report, IdentifierAndMethodLengthAnalyzer outer)
        {
            _tokens = tokens;
            _contextedFilename = contextedFilename;
            _report = report;
            _outer = outer;
        }

        public void PerformTopLevelScan()
        {
            int index = 0;
            for (; index < _tokens.Count; index++)
            {
                Token token = _tokens[index];
                bool shouldBreak = HandleToken(token);
                if (shouldBreak) break;
            }
            if (index == _tokens.Count && _currentLine.Count != 0) _linesSoFar++;
            if (_linesSoFar > WarningSettings.MaxMethodLength) Warn();
        }

        private bool HandleToken(Token token)
        {
            TokenType tokenType = token.TokenType;
            if (tokenType == Newline)
            {
                bool methodFound = HandleNewline();
                if (methodFound) return true;
            }
            else if (!tokenType.IsCommentType())
            {
                _currentLine.Add(token);
                if (tokenType.IsTypeType()) return true;
            }
            return false;
        }

        private void Warn()
        {
            _report.Warnings.Add($"Top-level statement in {_contextedFilename} is too long " +
                $"- {_linesSoFar} lines.");
        }

        private bool HandleNewline()
        {
            if (_currentLine.Count > 3 && _outer.CanBeMethod(_currentLine) != null) return true;
            if (!IsUsingOrBlankLine(_currentLine)) _linesSoFar++;
            _currentLine.Clear();
            return false;
        }

        private static bool IsUsingOrBlankLine(List<Token> lineSoFar) =>
            lineSoFar.Select(t => t.TokenType).All(
                tt => tt is Using or Period or Semicolon or Identifier);
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
        } while (nextTokenType != BracesOpen && nextTokenType != Semicolon);
        return nextTokenType == BracesOpen ? FileModus.Traditional : FileModus.FileScoped;
    }

    private void HandleRegularToken(List<Token> currentStatement, TokenType currentTokenType)
    {
        if (!currentTokenType.IsSkippable())
        {
            currentStatement.Add(CurrentToken());
            if (currentTokenType == Semicolon)
            {
                HandleStatementWithSemiColon(currentStatement);
            }
            else if (currentTokenType == BracesOpen)
            {
                HandleBracesOpen(currentStatement);
            }
        }
    }

    private void HandleBracesOpen(List<Token> currentStatement)
    {
        bool isBlockStatement = IsBlockStatement(currentStatement);
        _currentIndex++;
        int? methodIndex = UpdateMethodNames(currentStatement);
        AddScope(currentStatement, methodIndex);

        Scan();
        _scopes.RemoveAt(_scopes.Count - 1);
        IfMethodScopeEndsCheckMethodLength();
        currentStatement.Add(CurrentToken()); // should be }
        if (!isBlockStatement)
        {
            CheckVariables(currentStatement); // property?
            currentStatement.Clear();
        }
    }

    private int? UpdateMethodNames(List<Token> currentStatement)
    {
        int? methodIndex = CanBeMethod(currentStatement);
        if (methodIndex != null)
        {
            string methodName = ((ComplexToken)currentStatement[(int)methodIndex]).Info;
            _methodNames.Add((methodName, _currentIndex));
        }
        else
        {
            _methodNames.Add(("none", _currentIndex));
        }
        return methodIndex;
    }

    private void IfMethodScopeEndsCheckMethodLength()
    {
        (string methodName, int tokenIndex) = _methodNames[^1];
        int maxLineLength = WarningSettings.MaxMethodLength;
        if (methodName != "none")
        {
            int lineCount = new MethodLineCounter(tokenIndex, _currentIndex, _tokens).Count();
            if (lineCount > maxLineLength)
            {
                AddWarning($"Too long method: {methodName} " +
                    $"(in {_contextedFilename}) is {lineCount} lines long.");
                _report.ExtraCodeLines += lineCount - maxLineLength;
            }
        }
        _methodNames.RemoveAt(_methodNames.Count - 1);
    }

    private sealed class MethodLineCounter
    {
        private int _newlineCount = 0;
        private bool _newlineMode = false;
        private bool _commentLineMode = false;
        private readonly int _startIndex;
        private readonly int _endIndex;
        private readonly IReadOnlyList<Token> _tokens;

        public MethodLineCounter(int startIndex, int endIndex, IReadOnlyList<Token> tokens)
        {
            _startIndex = startIndex;
            _endIndex = endIndex;
            _tokens = tokens;
        }

        public int Count()
        {
            Debug.Assert(_tokens[_startIndex - 1].TokenType == BracesOpen);
            for (int i = _startIndex; i < _endIndex; i++)
            {
                TokenType currentTokenType = _tokens[i].TokenType;
                if (currentTokenType == Newline) HandleNewline();
                else if (currentTokenType.IsCommentType()) _newlineMode = false;
                else HandleRegularToken();
            }
            return _newlineCount + 1; // closing brace is also a line
        }

        private void HandleRegularToken()
        {
            _commentLineMode = false;
            _newlineMode = false;
        }

        private void HandleNewline()
        {
            if (!_newlineMode && !_commentLineMode) _newlineCount++;
            _newlineMode = true;
            _commentLineMode = true;
        }
    }

    protected Token? LookForNextEndingToken(List<Token> currentStatement)
    {
        Token currentToken = _tokens[_currentIndex];
        TokenType currentTokenType = currentToken.TokenType;
        while (currentTokenType != Semicolon && currentTokenType != BracesOpen && currentTokenType != BracesClose)
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
        if (currentStatement.Count < 2) return false;
        TokenType previousTokenType = currentStatement[^2].TokenType;
        if (previousTokenType == FatArrow || previousTokenType == TokenType.Switch) return true;
        return false;
    }

    protected void HandleStatementWithSemiColon(List<Token> currentStatement)
    {
        if (DoShow) Show(currentStatement);
        HandleStatementEndingWithSemicolon(currentStatement);
        currentStatement.Clear();
    }

    protected void HandleStatementEndingWithSemicolon(List<Token> currentStatement)
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
        while (currentTokenType != Semicolon)
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

    internal static bool IsCall(List<Token> currentStatement, int i)
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

    internal static bool RepresentsClassName(Token token, IReadOnlyList<Scope> scopes)
    {
        if (token.TokenType != Identifier) return false;
        string id = ((ComplexToken)token).Info;
        for (int i = scopes.Count - 1; i >= 0; i--)
        {
            if (scopes[i].Type == ScopeType.ClassRecordStruct) return scopes[i].Name == id;
        }
        return false;
    }

    internal void AddScope(List<Token> currentStatement, int? methodIndex)
    {
        Scope basicScope = UpdateScopeWithPossibleTypeDeclaration(currentStatement);
        if (basicScope.Type != ScopeType.ScopeTypeNotSet)
        {
            _scopes.Add(basicScope);
        }
        else
        {
            AddNonTypeScope(currentStatement, methodIndex);
        }
    }

    private void AddNonTypeScope(List<Token> currentStatement, int? methodIndex)
    {
        ScopeType scopeType = ScopeType.ScopeTypeNotSet;
        string name = "unknown";
        if (currentStatement.Any(t => t.TokenType == New))
        {
            scopeType = ScopeType.New;
        }
        if (methodIndex != null) scopeType = ScopeType.Method;
        ScopeType possibleScopeType = ScopeType.ScopeTypeNotSet;
        possibleScopeType = GetBackupScopeType(currentStatement, possibleScopeType);
        if (possibleScopeType != ScopeType.ScopeTypeNotSet) scopeType = possibleScopeType;

        _scopes.Add(new Scope { Type = scopeType, Name = name });
    }

    private static ScopeType GetBackupScopeType(List<Token> currentStatement, ScopeType possibleScopeType)
    {
        if (currentStatement.Count > 0)
        {
            possibleScopeType = GetAlternativeScopeType(currentStatement);
        }

        return possibleScopeType;
    }

    private static ScopeType GetAlternativeScopeType(List<Token> currentStatement)
    {
        return currentStatement[0].TokenType switch
        {
            Catch => ScopeType.Catch,
            Do => ScopeType.Do,
            If => ScopeType.If,
            Else => ScopeType.Else,
            Finally => ScopeType.Finally,
            ForEach => ScopeType.Foreach,
            For => ScopeType.For,
            Try => ScopeType.Try,
            While => ScopeType.While,
            _ => ScopeType.ScopeTypeNotSet
        };
    }

    private static Scope UpdateScopeWithPossibleTypeDeclaration(List<Token> currentStatement)
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

    public int? CanBeMethod(List<Token> currentStatement)
    {
        int i = 0;
        i = GetFirstIdentifier(currentStatement, i);

        (int newI, bool canBeCorrect) = GetCSharpType(currentStatement, i);
        if (!canBeCorrect) return null;
        if (IsConstructor(currentStatement, i)) return i;
        i = newI + 1;
        (int furtherI, bool canAlsoBeCorrect) = GetCSharpType(currentStatement, i);
        if (!canAlsoBeCorrect) return null;
        return IsAMethodAndCheckNamingStyle(currentStatement, i, furtherI);
    }

    private int? IsAMethodAndCheckNamingStyle(List<Token> currentStatement, int i, int furtherI)
    {
        if (currentStatement.Count > furtherI + 2 && currentStatement[furtherI + 1].TokenType == ParenthesesOpen)
        {
            string methodName = ((ComplexToken)currentStatement[i]).Info;
            if (DoShow) Console.WriteLine($"Candidate method: {methodName}");
            if (!char.IsUpper(methodName[0])) AddWarning(
                $"Invalid method name: {methodName} (in {_contextedFilename}).");
            int methodIndex = _currentIndex - currentStatement.Count + furtherI;
            CheckBlankLineBeforeMethod(methodName, methodIndex);
            ProcessParameter(currentStatement, furtherI + 1);
            return i;
        }
        return null;
    }

    private void CheckBlankLineBeforeMethod(string methodName, int methodIndex)
    {
        bool subsequentNewlines = false;
        bool lastWasNewline = false;
        for (int i = methodIndex; i >= 0; i--)
        {
            TokenType currentTokenType = _tokens[i].TokenType;
            (bool done, subsequentNewlines, lastWasNewline) = HandleBlankLineCheckingToken(
                currentTokenType, subsequentNewlines, lastWasNewline, methodName);
            if (done) return;
        }
    }

    private (bool done, bool subsequentNewlines, bool lastNewline) HandleBlankLineCheckingToken(
        TokenType currentTokenType, bool subsequentNewlines, bool lastWasNewline, string methodName)
    {
        if (currentTokenType == BracesOpen) return (true, subsequentNewlines, lastWasNewline);
        if (currentTokenType is Semicolon or BracesClose)
        {
            if (!subsequentNewlines) AddWarning(
                $"Missing blank line before {methodName} in {_contextedFilename}.");
            return (true, subsequentNewlines, lastWasNewline);
        }
        if (currentTokenType == Newline)
        {
            if (lastWasNewline) subsequentNewlines = true;
            lastWasNewline = true;
        }
        return (false, subsequentNewlines, lastWasNewline);
    }

    private static int GetFirstIdentifier(List<Token> currentStatement, int i)
    {
        TokenType tokenType = currentStatement[i].TokenType;
        while (tokenType.IsModifier() || tokenType.IsDeclarer())
        {
            i++;
            tokenType = currentStatement[i].TokenType;
        }

        return i;
    }

    private bool IsConstructor(List<Token> currentStatement, int i)
    {
        if (currentStatement[i].TokenType != Identifier || currentStatement.Count < i + 2
            || currentStatement[i + 1].TokenType != ParenthesesOpen) return false;
        string identifier = ((ComplexToken)currentStatement[i]).Info;
        for (int scopeIndex = _scopes.Count - 1; scopeIndex >= 0; scopeIndex--)
        {
            if (_scopes[scopeIndex].Type == ScopeType.ClassRecordStruct)
            {
                return CheckConstructor(currentStatement, i, identifier, scopeIndex);
            }
        }
        return false;
    }

    private bool CheckConstructor(List<Token> currentStatement, int i, string identifier, int scopeIndex)
    {
        if (_scopes[scopeIndex].Name == identifier)
        {
            CheckBlankLineBeforeMethod(identifier, _currentIndex - currentStatement.Count + i);
            return true;
        }
        return false;
    }

    // should handle simple types like int as well as List<T> or (int x, bool y)
    private static (int newIndex, bool canBeCorrect) GetCSharpType(List<Token> currentStatement, int i)
    {
        TokenType currentTokenType = currentStatement[i].TokenType;
        if (currentTokenType == Identifier)
        {
            return HandleIdentifier(currentStatement, i);
        }
        else if (currentTokenType == ParenthesesOpen)
        {
            return TupleType(currentStatement, i);
        }
        else
        {
            return (i, false);
        }
    }

    private static (int newIndex, bool canBeCorrect) HandleIdentifier(List<Token> currentStatement, int i)
    {
        if (i < currentStatement.Count - 1)
        {
            TokenType nextTokenType = currentStatement[i + 1].TokenType;
            if (nextTokenType == Less) return TypeParameterContents(currentStatement, i);
            else if (nextTokenType == BracketsOpen) return (i + 2, true);
            else if (nextTokenType == QuestionMark) return (i + 1, true);
            else return (i, true);
        }
        else return (i, false);
    }

    private static (int newIndex, bool canBeCorrect) TupleType(List<Token> currentStatement, int i)
    {
        int depth = 0;
        for (int j = i + 1; j < currentStatement.Count; j++)
        {
            TokenType currentTokenType = currentStatement[j].TokenType;
            if (currentTokenType == ParenthesesOpen) depth++;
            (depth, bool shouldReturn, int newJ) =
                HandleClosingToken(currentTokenType, depth, currentStatement, j, ParenthesesClose);
            if (shouldReturn) return (newJ, true);
        }
        return (i, false);
    }

    private static (int depth, bool shouldReturn, int newJ) HandleClosingToken(
        TokenType currentTokenType, int depth, List<Token> currentStatement, int j, TokenType endingToken)
    {
        if (currentTokenType == endingToken)
        {
            if (depth == 0)
            {
                j = AddQuestionMarkIfNeeded(currentStatement, j);

                return (depth, true, j);
            }
            return (depth - 1, false, j);
        }
        return (depth, false, j);
    }

    private static int AddQuestionMarkIfNeeded(List<Token> currentStatement, int j)
    {
        if (j + 1 < currentStatement.Count && currentStatement[j + 1].TokenType == QuestionMark)
        {
            j++;
        }

        return j;
    }

    private static (int newIndex, bool canBeCorrect) TypeParameterContents(List<Token> currentStatement, int i)
    {
        int depth = 0;
        for (int j = i + 2; j < currentStatement.Count; j++)
        {
            TokenType currentTokenType = currentStatement[j].TokenType;
            if (currentTokenType == Less) depth++;
            (depth, bool shouldReturn, int newJ) =
                HandleClosingToken(currentTokenType, depth, currentStatement, j, Greater);
            if (shouldReturn) return (newJ, true);
        }
        return (i, false);
    }

    protected void ProcessPossibleIdentifier(List<Token> currentStatement)
    {
        // can be empty through "return {..;"
        bool ready = ParseStraightforwardCases(currentStatement);
        if (ready) return;
        if (DoShow) Show(currentStatement);
        int? pos = CanBeMethod(currentStatement);
        if (pos != null)
        {
            // CAN be A<T>( method)
            while (currentStatement[(int)pos].TokenType != ParenthesesOpen) pos++;
        }
        else CheckVariables(currentStatement);
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
        if (tokenType == TokenType.Enum) return true;
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
            AddWarning(warning);
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
            string normalName = identifierName[1..];
            if (!AllCSharpKeywords.KeyWords.Contains(normalName))
            {
                AddWarning($"Unnecessary '@' in front of {identifierName} " +
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

    private static bool IsValueName(List<Token> currentStatement,
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

    private static List<Token> GetParameters(List<Token> currentStatement, int openingPos)
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

    protected void ProcessParameter(List<Token> currentStatement, int openParenthesisPos)
    {
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
            AddWarning($"Invalid parameter name: " +
                $"{paramName} (in {_contextedFilename}).");
    }

    private (bool isFinished, char startChar) GetStartCharAndName(string paramName)
    {
        char startChar = paramName[0];
        if (startChar == '@')
        {
            string normalName = paramName[1..];
            if (!AllCSharpKeywords.KeyWords.Contains(normalName))
            {
                AddWarning($"Unnecessary '@' in front of {paramName} " +
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