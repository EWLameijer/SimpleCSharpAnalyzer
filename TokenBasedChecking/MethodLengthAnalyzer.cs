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
        while (CurrentIndex < Tokens.Count)
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
                    CurrentIndex++;
                    AddScope(currentStatement);
                    UpdateMethodNames(currentStatement);
                    ScanMethods();
                    Scopes.RemoveAt(Scopes.Count - 1);
                    currentStatement.Add(CurrentToken()); // should be }
                    if (!isBlockStatement) HandleStatement(currentStatement);
                }
                else if (currentTokenType == BracesClose)
                {
                    IfMethodScopeEndsCheckMethodLength();
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
            _methodNames.Add((methodName, CurrentIndex));
        }
        else
        {
            _methodNames.Add(("none", CurrentIndex));
        }
    }

    private void HandleStatementEndingInClosingBraces()
    {
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