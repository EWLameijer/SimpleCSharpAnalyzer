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
            currentStatement.Add(currentToken);
            TokenType currentTokenType = currentToken.TokenType;
            if (currentTokenType == SemiColon)
            {
                HandleStatementEndingWithSemicolon(currentStatement, postBraces);
            }
            else if (currentTokenType == BracesClose)
            {
                (string methodName, int tokenIndex) = _methodNames[_methodNames.Count - 1];
                if (methodName != "none")
                {
                    // is method scope!
                    //Console.WriteLine($"###detected method exit {methodName} at {CurrentIndex}");
                    int lineCount = CountLines(tokenIndex, CurrentIndex);
                    if (lineCount > 15)
                    {
                        Report.Warnings.Add($"Too long method: {methodName} " +
                            $"(in {ContextedFilename}) is {lineCount} lines long.");
                        Report.ExtraMethodLines += lineCount - 15;
                    }
                }
                _methodNames.RemoveAt(_methodNames.Count - 1);
                Scopes.RemoveAt(Scopes.Count - 1);
                CurrentIndex++;
                // duplicate code!
                while (CurrentIndex < Tokens.Count && (Tokens[CurrentIndex].TokenType.IsSkippable() ||
                    Tokens[CurrentIndex].TokenType == ParenthesesClose))
                    CurrentIndex++;
                return;
            }
            else // opening braces
            {
                AddScope(currentStatement);
                int? methodIndex = CanBeMethod(currentStatement);
                if (methodIndex != null)
                {
                    string methodName = ((ComplexToken)currentStatement[(int)methodIndex]).Info;
                    // Console.WriteLine($"***detected method entry {methodName} at {CurrentIndex}");
                    _methodNames.Add((methodName, CurrentIndex));
                }
                else
                {
                    _methodNames.Add(("none", CurrentIndex));
                }
                CurrentIndex++;
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
            if (Tokens[i].TokenType == TokenType.NewLine)
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
}