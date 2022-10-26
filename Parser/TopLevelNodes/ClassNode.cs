using Tokenizing;

namespace Parser.TopLevelNodes;

internal class ClassNode : TopLevelNode
{
    // first token is "class"
    public static ClassNode Get(ParsePosition position, List<Token> modifiers)
    {
        position.Proceed(); // skip "class" token
    }
}