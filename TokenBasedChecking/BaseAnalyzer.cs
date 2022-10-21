using DTOsAndUtilities;
using Tokenizing;
using static Tokenizing.TokenType;
using Scope = DTOsAndUtilities.Scope;

namespace TokenBasedChecking;

public class BaseAnalyzer
{
    protected readonly string ContextedFilename;
    protected readonly Report Report;
    protected const bool DoShow = true;
    protected readonly List<Scope> Scopes = new();

    public BaseAnalyzer(FileTokenData fileData, Report report)
    {
        ContextedFilename = fileData.ContextedFilename;
        Report = report;
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
            Scopes.Add(basicScope);
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
        if (CanBeMethod(currentStatement) != null) scopeType = ScopeType.Method;
        ScopeType possibleScopeType = ScopeType.ScopeTypeNotSet;
        possibleScopeType = GetBackupScopeType(currentStatement, possibleScopeType);
        if (possibleScopeType != ScopeType.ScopeTypeNotSet) scopeType = possibleScopeType;

        Scopes.Add(new Scope { Type = scopeType, Name = name });
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

    public int? CanBeMethod(List<Token> currentStatement)
    {
        List<TokenType> newBracesStack = new();
        List<TokenType> possibleTypeStack = new();
        for (int i = 0; i < currentStatement.Count; i++)
        {
            TokenType tokenType = currentStatement[i].TokenType;
            if (tokenType.IsModifier() || tokenType.IsDeclarer()) continue;
            possibleTypeStack.Add(tokenType);
            (int? foundIndex, bool done) =
                CheckMethodCloser(tokenType, newBracesStack, currentStatement, possibleTypeStack, i);
            if (foundIndex != null || done) return foundIndex;
        }
        return null;
    }

    private (int? foundIndex, bool done) CheckMethodCloser(TokenType tokenType, List<TokenType> newBracesStack,
        List<Token> currentStatement, List<TokenType> possibleTypeStack, int i)
    {
        if (tokenType.IsOpeningType()) newBracesStack.Add(tokenType);
        else if (tokenType.IsClosingType()) CheckForwardBraces(tokenType, newBracesStack);
        else
        {
            return CheckWhetherLineIsMethod(currentStatement, i, newBracesStack, possibleTypeStack);
        }
        return (null, false);
    }

    private (int? foundIndex, bool done) CheckWhetherLineIsMethod(List<Token> currentStatement, int i,
        List<TokenType> newBracesStack, List<TokenType> possibleTypeStack)
    {
        TokenType tokenType = currentStatement[i].TokenType;
        TokenType? prevTokenType = i > 0 ? currentStatement[i - 1].TokenType : null;
        if (TokenIsMethodName(currentStatement, i, newBracesStack,
            possibleTypeStack, tokenType, prevTokenType))
        {
            string methodName = ((ComplexToken)currentStatement[i]).Info;
            if (DoShow) Console.WriteLine($"Candidate method: {methodName}");
            if (!char.IsUpper(methodName[0])) Report.Warnings.Add(
                $"Invalid method name: {methodName} (in {ContextedFilename}).");
            return (i, true);
        }
        if (tokenType == Assign) return (null, true);
        return (null, false);
    }

    private bool TokenIsMethodName(List<Token> currentStatement, int i, List<TokenType> newBracesStack,
        List<TokenType> possibleTypeStack, TokenType tokenType, TokenType? prevTokenType)
    {
        return newBracesStack.Count == 0 && (possibleTypeStack.Count(t => t == Identifier) > 1 ||
                    possibleTypeStack.Count(t => t == Identifier) == 1 &&
                    RepresentsClassName(currentStatement[i], Scopes)) &&
                    IsDirectCall(currentStatement, i) && tokenType == Identifier && prevTokenType != Period;
    }

    private static bool IsDirectCall(List<Token> currentStatement, int i)
    {
        if (i == currentStatement.Count - 1) return false;
        TokenType nextType = currentStatement[i + 1].TokenType;
        return nextType == ParenthesesOpen;
    }
}