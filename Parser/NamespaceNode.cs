using Tokenizing;
using static Tokenizing.TokenType;

namespace Parser;

public class NamespaceNode : Node
{
    private readonly IReadOnlyList<Token> _nameSpaceName; // empty list = top level statements

    private NamespaceNode(IReadOnlyList<Token> namespaceName)
    {
        _nameSpaceName = namespaceName;
    }

    public static NamespaceNode Get(ParsePosition position)
    {
        position.SkipWhitespace();
        if (position.CurrentTokenType() == Namespace)
        {

        }
    }
}