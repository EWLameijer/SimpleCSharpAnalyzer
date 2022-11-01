using Tokenizing;

namespace Parser.TopLevelNodes;

internal class TopLevelNode : Node
{
    // pubic static class ...
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
            TokenType.Class => ClassNode.Get(position, modifiers),
            _ => throw new ArgumentException("TopLevelNode.Get: illegal data structure to parse!")
        };
    }
}