using Tokenizing;

namespace FileHandling;

public class TokenFilterer
{
    public IReadOnlyList<Token> Filter(IReadOnlyList<Token> tokens) =>
        HandlePragmas(HandleDecimalLiterals(FilterOutAttributes(tokens)));

    private IReadOnlyList<Token> HandlePragmas(IReadOnlyList<Token> tokens) =>
        tokens.Where(t => t.TokenType != TokenType.Pragma).ToList();

    private IReadOnlyList<Token> HandleDecimalLiterals(IReadOnlyList<Token> tokens)
    {
        List<Token> output = new() { tokens[0] };
        for (int i = 1; i < tokens.Count; i++) // start at 1(!), as m must always be preceded by number
        {
            ProcessPossibleDecimalToken(tokens[i], output);
        }
        return output;
    }

    private void ProcessPossibleDecimalToken(Token current, List<Token> output)
    {
        int previousIndex = output.Count - 1;
        Token previousToken = output[previousIndex];
        if (IsMPrecededByNumber(current, previousToken))
        {
            output[previousIndex].TokenType = TokenType.DecimalLiteral;
        }
        else
        {
            output.Add(current);
        }
    }

    private IReadOnlyList<Token> FilterOutAttributes(IReadOnlyList<Token> tokens)
    {
        List<Token> output = new();
        for (int i = 0; i < tokens.Count; i++)
        {
            TokenType currentType = tokens[i].TokenType;
            if (currentType == TokenType.BracketsOpen && LastRealType(tokens, i) != TokenType.Identifier)
            {
                i = SkipAttribute(tokens, i);
            }
            else output.Add(tokens[i]);
        }
        return output;
    }

    private static int SkipAttribute(IReadOnlyList<Token> tokens, int startIndex)
    {
        int depth = 0;
        int newIndex = startIndex + 1;
        TokenType newToken = tokens[newIndex].TokenType;
        while (newToken != TokenType.BracketsClose || depth > 0)
        {
            if (newToken == TokenType.BracketsOpen) depth++;
            if (newToken == TokenType.BracketsClose) depth--;
            newIndex++;
            newToken = tokens[newIndex].TokenType;
        }
        return newIndex; // index of ']'
    }

    private static bool IsMPrecededByNumber(Token current, Token previous) =>
    current.TokenType == TokenType.Identifier &&
    ((ComplexToken)current).Info.ToLower() == "m" &&
    previous.TokenType == TokenType.Number;

    private TokenType LastRealType(IReadOnlyList<Token> tokens, int i)
    {
        for (int investigatedIndex = i - 1; investigatedIndex > 0; investigatedIndex--)
        {
            TokenType tokenType = tokens[investigatedIndex].TokenType;
            if (!tokenType.IsSkippable()) return tokenType;
        }
        return TokenType.Identifier;
    }
}