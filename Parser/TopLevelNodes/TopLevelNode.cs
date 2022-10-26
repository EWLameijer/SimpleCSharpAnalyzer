using Tokenizing;

namespace Parser.TopLevelNodes;

internal class TopLevelNode : Node
{
    public static TopLevelNode Get(ParsePosition position)
    {
        position.SkipWhitespace();
        List<Token> modifiers = new();
        while (position.CurrentTokenType().IsModifier())
        {
            modifiers.Add(position.CurrentToken());
            position.Proceed();
        }
        return position.CurrentTokenType() switch
        {
            TokenType.Class => ClassNode.Get(position),
            _ => throw new ArgumentException("TopLevelNode.Get: illegal data structure to parse!")
        };
    }
}