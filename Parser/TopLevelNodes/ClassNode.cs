using Tokenizing;

namespace Parser.TopLevelNodes;

internal class ClassNode : TopLevelNode
{
    // first token is "class"
    public static ClassNode Get(ParsePosition position, List<Token> modifiers)
    {
        position.Proceed(); // skip "class" token
        string className = ((ComplexToken)position.CurrentToken()).Info;
        position.Proceed(); // expect : or {
        List<Token> ancestors = GetAncestors(position);
    }

    private static List<Token> GetAncestors(ParsePosition position)
    {
        List<Token> implemented = new();
        if (position.CurrentTokenType() == TokenType.Colon)
        {
            do
            {
                position.Proceed();
                TokenType currentTokenType = position.CurrentTokenType();
                if (currentTokenType == TokenType.Identifier) implemented.Add(position.CurrentToken());
                else if (currentTokenType == TokenType.BracesOpen) break;
                // skip whitespaces and commas
            } while (true);
        }
        return implemented;
    }
}