using DTOsAndUtilities;
using Tokenizing;
using static Tokenizing.TokenType;

namespace TokenBasedChecking;

public class MethodLengthAnalyzer : BaseAnalyzer
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _currentIndex = 0;
    private readonly List<(string, int)> _methodNames = new();

    public MethodLengthAnalyzer(FileTokenData fileData, Report report) : base(fileData, report)
    {
        _tokens = fileData.Tokens;
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
                    {
                        Report.Warnings.Add($"Too long method: {methodName} " +
                            $"(in {ContextedFilename}) is {lineCount} lines long.");
                        Report.ExtraCodeLines += lineCount - 15;
                    }
                }
                _methodNames.RemoveAt(_methodNames.Count - 1);
                Scopes.RemoveAt(Scopes.Count - 1);
                _currentIndex++;
                // duplicate code!
                while (_currentIndex < _tokens.Count && (_tokens[_currentIndex].TokenType.IsSkippable() ||
                    _tokens[_currentIndex].TokenType == ParenthesesClose))
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
}