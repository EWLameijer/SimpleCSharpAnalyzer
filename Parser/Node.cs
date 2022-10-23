using Tokenizing;

namespace Parser;

public class Node
{
    protected Node()
    {
    }

    protected static List<Token> GetNextUntil(ParsePosition position, HashSet<TokenType> endingTokens)
    {
        position.Proceed();
        List<Token> contents = new();
        while (!endingTokens.Contains(position.CurrentTokenType()))
        {
            contents.Add(position.CurrentToken());
            position.Proceed();
        }
        return contents;
    }

    protected static List<Token> GetNextUntil(ParsePosition position, TokenType endingToken)
    {
        return GetNextUntil(position, new HashSet<TokenType> { endingToken });
    }
}