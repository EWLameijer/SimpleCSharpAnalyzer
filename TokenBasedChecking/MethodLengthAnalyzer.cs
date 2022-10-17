using DTOsAndUtilities;
using Tokenizing;
using static Tokenizing.TokenType;

namespace TokenBasedChecking;

public class MethodLengthAnalyzer
{
    private readonly string _contextedFilename;
    private readonly IReadOnlyList<Token> _tokens;
    private readonly Report _report;
    private int _currentIndex = 0;
    private readonly List<Scope> _scopes = new();
    private readonly List<(string, int)> _methodNames = new();

    public MethodLengthAnalyzer(FileTokenData fileData, Report report)
    {
        _contextedFilename = fileData.ContextedFilename;
        _tokens = fileData.Tokens;
        _report = report;
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
                (string methodName, int tokenIndex) = _methodNames[_methodNames.Count - 1];
                if (methodName != "none")
                {
                    // is method scope!
                    //Console.WriteLine($"###detected method exit {methodName} at {_currentIndex}");
                    //Console.WriteLine($"&&&counted {}");
                    int lineCount = CountLines(tokenIndex, _currentIndex);
                    if (lineCount > 15)
                        _report.Warnings.Add($"Too long method: {methodName} (in {_contextedFilename}) is {lineCount} lines long.");
                }
                _methodNames.RemoveAt(_methodNames.Count - 1);
                _scopes.RemoveAt(_scopes.Count - 1);
                _currentIndex++;
                return;
            }
            else // opening braces
            {
                AddScope(currentStatement);
                int? methodIndex = CanBeMethod(currentStatement);
                if (methodIndex != null)
                {
                    string methodName = ((ComplexToken)currentStatement[(int)methodIndex]).Info;
                    // Console.WriteLine($"***detected method entry {methodName} at {_currentIndex}");
                    _methodNames.Add((methodName, _currentIndex));
                }
                else
                {
                    _methodNames.Add(("none", _currentIndex));
                }
                _currentIndex++;
                currentStatement.Clear();
                ScanMethods();
                postBraces = true;
            }
        }
    }

    private int CountLines(int startIndex, int endIndex)
    {
        int newlineCount = 0;
        bool newlineMode = false;
        for (int i = startIndex; i < endIndex; i++)
        {
            if (_tokens[i].TokenType == TokenType.NewLine)
            {
                if (newlineMode)
                {
                    //  do nothing
                }
                else
                {
                    newlineCount++;
                    newlineMode = true;
                }
            }
            else
            {
                newlineMode = false;
            }
        }
        return newlineCount + 1;// closing brace is also a line
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

        if (firstType == If || firstType == Else || firstType == ForEach || firstType == Return)
        {
            currentStatement.Clear();
            return;
        }
        //Show(currentStatement);
        if (CanBeMethod(currentStatement) != null)
        {
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
                }
                if (tokenType == Assign) break;
            }
        }
        currentStatement.Clear();
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
                    possibleTypeStack.Count(t => t == Identifier) == 1 && SharedUtils.RepresentsClassName(currentStatement[i], _scopes))
                    &&
                IsDirectCall(currentStatement, i) && tokenType == Identifier && prevTokenType != Period)
                {
                    string methodName = ((ComplexToken)currentStatement[i]).Info;
                    //Console.WriteLine($"Candidate method: {methodName}");
                    if (!char.IsUpper(methodName[0])) _report.Warnings.Add(
                        $"Invalid method name: {methodName} (in {_contextedFilename}).");
                    return i;
                }
                if (tokenType == Assign) break;
            }
        }
        return null;
    }

    private bool IsDirectCall(List<Token> currentStatement, int i)
    {
        if (i == currentStatement.Count - 1) return false;
        TokenType nextType = currentStatement[i + 1].TokenType;
        return nextType == ParenthesesOpen;
    }
}