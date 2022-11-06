using Tokenizing;

namespace Parser.TopLevelNodes;

internal class TypeNode
{
    public static TypeNode Get(ParsePosition position)
    {
        TokenType currentType = position.CurrentTokenType();
        if (currentType == TokenType.ParenthesesOpen)
        {
            return TupleTypeNode.Parse(position);
        }
        if (currentType == TokenType.Identifier)
        {
            Token mainToken = position.CurrentToken();
            position.Proceed();
            TokenType newType = position.CurrentTokenType();
            if (newType == TokenType.BracketsOpen)
            {
                position.Proceed();
                TokenType thirdTokenType = position.CurrentTokenType();
                if (thirdTokenType == TokenType.BracketsClose)
                {
                }
                else
                {
                    // throw some kind of exception
                }
            }
        }
    }
}