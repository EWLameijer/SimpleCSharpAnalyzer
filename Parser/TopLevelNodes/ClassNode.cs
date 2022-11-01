using System.Diagnostics;
using Tokenizing;

namespace Parser.TopLevelNodes;

internal class ClassNode : TopLevelNode
{
    private readonly string _name;
    private readonly List<Token> _ancestors;
    private readonly List<Token> _modifiers; // public / internal / static

    public ClassNode(string name, List<Token> ancestors, List<Token> modifiers)
    {
        _name = name;
        _ancestors = ancestors;
        _modifiers = modifiers;
    }

    // first token is "class"
    public static ClassNode Get(ParsePosition position, List<Token> modifiers)
    {
        position.Proceed(); // skip "class" token
        string className = ((ComplexToken)position.CurrentToken()).Info;
        position.Proceed(); // expect ':' or '{'
        List<Token> ancestors = GetAncestors(position);
        position.SkipWhitespace();
        Debug.Assert(position.CurrentTokenType() == TokenType.BracesOpen);
        position.SkipWhitespace();

        position.Proceed(); // skip '{'
        List<ClassLevelNode> classLevelNodes = new();
        do
        {
            ClassLevelNode classLevelNode = ClassLevelNode.Get(position);
            if (classLevelNode == null) break;
            classLevelNodes.Add(classLevelNode);
        } while (true);
        return new ClassNode(className, ancestors, modifiers);
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