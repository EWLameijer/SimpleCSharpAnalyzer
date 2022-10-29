using System.Diagnostics;
using Tokenizing;

namespace Parser.TopLevelNodes;

internal class ClassNode : TopLevelNode
{
    private readonly string _name;
    private readonly List<Token> _ancestors;

    public ClassNode(string name, List<Token> ancestors)
    {
        _name = name;
        _ancestors = ancestors;
    }

    // first token is "class"
    public static ClassNode Get(ParsePosition position, List<Token> modifiers)
    {
        position.Proceed(); // skip "class" token
        string className = ((ComplexToken)position.CurrentToken()).Info;
        position.Proceed(); // expect : or {
        List<Token> ancestors = GetAncestors(position);
        position.SkipWhitespace();
        Debug.Assert(position.CurrentTokenType() == TokenType.BracesOpen);
        position.SkipWhitespace();
        return new ClassNode(className, ancestors);
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