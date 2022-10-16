using DTOsAndUtilities;
using Tokenizing;
using static Tokenizing.TokenType;

namespace TokenBasedChecking;

internal enum ScopeType
{
    ScopeTypeNotSet, File, Namespace, ClassRecordStruct, Method,
    If, While, Do, For, Foreach, New, Else
}

internal record Scope(ScopeType Type, string Name);

public class IdentifierAnalyzer
{
    private readonly string _contextedFilename;
    private readonly IReadOnlyList<Token> _tokens;
    private readonly Report _report;
    private int _currentIndex = 0;
    private readonly List<Scope> _scopes = new();

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
                return;
            }
            else // opening braces
            {
                AddScope(currentStatement);
                ProcessPossibleIdentifier(currentStatement);
                _currentIndex++;
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
        if (CanBeMethod(currentStatement)) scopeType = ScopeType.Method;
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

        if (firstType == If || firstType == Else || firstType == ForEach || firstType == Return)
        {
            currentStatement.Clear();
            return;
        }
        // "Show(currentStatement);"
        List<TokenType> newBracesStack = new();
        List<TokenType> possibleTypeStack = new();
        for (int i = 0; i < currentStatement.Count; i++)
        {
            TokenType tokenType = currentStatement[i].TokenType;
            if (tokenType.IsModifier() || tokenType.IsDeclarer()) continue;
            possibleTypeStack.Add(tokenType);
            if (tokenType.IsOpeningType()) newBracesStack.Add(tokenType);
            else if (tokenType.IsClosingType()) CheckForwardBraces(tokenType, newBracesStack);
            else
            {
                if (newBracesStack.Count == 0 && possibleTypeStack.Count(t => t == Identifier) > 1 &&
                !IsCall(currentStatement, i)
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
        else return char.IsLower(identifierName[0]);
    }

    private static bool SuggestsUpperCase(TokenType tokenType) =>
        tokenType == Public || tokenType == Protected || tokenType == Const;

    private static void CheckForwardBraces(TokenType tokenType, List<TokenType> bracesStack)
    {
        TokenType topBrace = bracesStack.Last();
        int lastIndex = bracesStack.Count - 1;
        if (tokenType == Greater && topBrace != Less) return; // something like "Assert.True(a>b)"
        while (topBrace == Less && tokenType != Greater)
        {
            bracesStack.RemoveAt(lastIndex);
            topBrace = bracesStack.Last();
            lastIndex = bracesStack.Count - 1;
        }
        if ((tokenType == ParenthesesClose && topBrace == ParenthesesOpen) ||
            (tokenType == BracketsClose && topBrace == BracketsOpen) ||
            (tokenType == BracesClose && topBrace == BracesOpen) ||
            (tokenType == Greater && topBrace == Less))
        {
            bracesStack.RemoveAt(lastIndex);
        }
        else
        {
            throw new ArgumentException("Parsing error! ");
        }
    }

    private bool IsCall(List<Token> currentStatement, int i)
    {
        if (i == currentStatement.Count - 1) return false;
        TokenType nextType = currentStatement[i + 1].TokenType;
        return nextType == ParenthesesOpen || nextType == Period;
    }

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

    private bool CanBeMethod(List<Token> currentStatement)
    {
        List<TokenType> newBracesStack = new();
        List<TokenType> possibleTypeStack = new();
        for (int i = 0; i < currentStatement.Count; i++)
        {
            TokenType tokenType = currentStatement[i].TokenType;
            if (tokenType.IsModifier() || tokenType.IsDeclarer()) continue;
            possibleTypeStack.Add(tokenType);
            if (tokenType.IsOpeningType()) newBracesStack.Add(tokenType);
            else if (tokenType.IsClosingType()) CheckForwardBraces(tokenType, newBracesStack);
            else
            {
                if (newBracesStack.Count == 0 && (possibleTypeStack.Count(t => t == Identifier) > 1 ||
                    possibleTypeStack.Count(t => t == Identifier) == 1 && RepresentsClassName(currentStatement[i]))
                    &&
                IsDirectCall(currentStatement, i) && tokenType == Identifier)
                {
                    string methodName = ((ComplexToken)currentStatement[i]).Info;
                    // Console.WriteLine($"Candidate method: {methodName}");
                    if (!char.IsUpper(methodName[0])) _report.Warnings.Add(
                        $"Invalid method name: {methodName} (in {_contextedFilename}).");
                    return true;
                }
                if (tokenType == Assign) break;
            }
        }
        return false;
    }

    private bool RepresentsClassName(Token token)
    {
        if (token.TokenType != Identifier) return false;
        string id = ((ComplexToken)token).Info;
        for (int i = _scopes.Count - 1; i >= 0; i--)
        {
            if (_scopes[i].Type == ScopeType.ClassRecordStruct) return _scopes[i].Name == id;
        }
        return false;
    }

    /*private static void ScanVariables()
    {
        _currentBraceLevel = 0;
        // first simply check each line
        List<Token> currentStatement = new();
        for (int i = 0; i < _tokens.Count; i++)
        {
            TokenType tokenType = _tokens[i].TokenType;
            if (tokenType == If || tokenType == While)
            {
                i = ParseIfBlock(i) - 1;
                continue;
            }
            if (tokenType == Else)
            {
                if (_tokens[i + 1].TokenType == If) continue;

                i = ParseBlock(i + 1) - 1;
                continue;
            }
            if (tokenType == BracesOpen) _currentBraceLevel++;
            else if (tokenType == BracesClose) _currentBraceLevel++;
            else if (tokenType == ParenthesesOpen && CanBeMethod(i, currentStatement))
            {
                i = ParseIfBlock(i - 1) - 1;
                continue;
            }
            else if (tokenType == Class || tokenType == Record || tokenType == Struct)
            { // guard against record struct...
                int candidateIdentifierIndex = i + 1;
                while (_tokens[candidateIdentifierIndex].TokenType != Identifier)
                    candidateIdentifierIndex++;
                _classNameStack[_currentBraceLevel + 1] =
                    ((ComplexToken)_tokens[candidateIdentifierIndex]).Info;
            }
            else ProcessPossibleIdentifier(i, currentStatement);
        }
    }

    private static int ParseBlock(int v)
    {
        // copy-paste from if. Does this work? Starts with newline or brace.
        int index = v;
        while (_tokens[index].TokenType == NewLine) index++;
        TokenType currentType = _tokens[index].TokenType;
        if (currentType == BracesOpen)
        {
            List<Token> currentStatement = new();
            do
            {
                do
                {
                    index++;
                    Token currentToken = _tokens[index];
                    currentType = currentToken.TokenType;
                    if (currentType == BracesClose) break;
                    if (currentType != NewLine) currentStatement.Add(currentToken);
                } while (currentType != SemiColon);
                ProcessPossibleIdentifier(index, currentStatement);
                currentStatement.Clear();
            } while (currentType != BracesClose);
            return index + 1;
        }
        else
        {
            // simple statement, assignment meaningless
            while (_tokens[index].TokenType != SemiColon) index++;
            index++; //mover past semicolon
            while (_tokens[index].TokenType == NewLine) index++;
            return index; // fresh start!
        }
    }

    private static int ParseIfBlock(int currentIndex)
    {
        int index = currentIndex + 1;
        while (_tokens[index].TokenType != ParenthesesOpen) index++; // skip newlines
        int braceLevel = 1;
        do
        {
            index++;
            TokenType tokenType = _tokens[index].TokenType;
            if (tokenType == ParenthesesOpen) braceLevel++;
            if (tokenType == ParenthesesClose) braceLevel--;
            if (braceLevel == 0) break;
        } while (true);
        index++;
        return ParseBlock(index);
    }

    private static void ProcessPossibleIdentifier(int tokenIndex, List<Token> currentStatement)
    {
        // using X;
        Token currentToken = _tokens[tokenIndex];
        if (currentToken.TokenType != SemiColon)
        {
            if (currentToken.TokenType != NewLine) currentStatement.Add(currentToken);
            return;
        }
        int depth = 0;
        Show(currentStatement);
        List<TokenType> bracesStack = new();
        int firstUsefulIndex = 0;
        for (int itemIndex = currentStatement.Count - 1; itemIndex >= 0; itemIndex--)
        {
            TokenType tokenType = currentStatement[itemIndex].TokenType;
            if (tokenType.IsClosingType()) bracesStack.Add(tokenType);
            if (tokenType.IsOpeningType())
            {
                if (bracesStack.Count == 0)
                {
                    firstUsefulIndex = itemIndex + 1;
                    break;
                }
                CheckBraces(tokenType, bracesStack);
            }
        }
        // declarations are typically [modifier*] [type] [identifier] [=...]?;

        List<TokenType> newBracesStack = new();
        List<TokenType> possibleTypeStack = new();
        for (int i = firstUsefulIndex; i < currentStatement.Count; i++)
        {
            TokenType tokenType = currentStatement[i].TokenType;
            if (tokenType.IsModifier() || tokenType.IsDeclarer()) continue;
            possibleTypeStack.Add(tokenType);
            if (tokenType.IsOpeningType()) newBracesStack.Add(tokenType);
            else if (tokenType.IsClosingType()) CheckForwardBraces(tokenType, newBracesStack);
            else
            {
                if (newBracesStack.Count == 0 && possibleTypeStack.Count(t => t == Identifier) > 1 &&
                !IsCall(currentStatement, i) && tokenType == Identifier)
                {
                    Console.WriteLine($"Candidate variable: {((ComplexToken)currentStatement[i]).Info}");
                }
                if (tokenType == Assign) break;
            }
        }
        currentStatement.Clear();
    }

    private static void CheckBraces(TokenType tokenType, List<TokenType> bracesStack)
    {
        TokenType topBrace = bracesStack.Last();
        int lastIndex = bracesStack.Count - 1;
        while (topBrace == Greater && tokenType != Less)
        {
            bracesStack.RemoveAt(lastIndex);
            topBrace = bracesStack.Last();
            lastIndex = bracesStack.Count - 1;
        }
        if ((tokenType == ParenthesesOpen && topBrace == ParenthesesClose) ||
            (tokenType == BracketsOpen && topBrace == BracketsClose) ||
            (tokenType == BracesOpen && topBrace == BracesClose) ||
            (tokenType == Less && topBrace == Greater))
        {
            bracesStack.RemoveAt(lastIndex);
        }
        else
        {
            throw new Exception("Parsing error! ");
        }
    }

*/
}

// need full recursion!
// as long as I don't encounter a "{"
// read till next ;
// check if there's an identifier in a line
// as soon as I encounter a {
// check type (struct, record, class, namespace, method, if, else, do, while, for, foreach)