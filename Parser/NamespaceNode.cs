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

    private static readonly HashSet<TokenType> _namespaceEnders = new() { SemiColon, BracesOpen };

    public static NamespaceNode Get(ParsePosition position)
    {
        position.SkipWhitespace();
        if (position.CurrentTokenType() == Namespace)
        {
            List<Token> contents = GetNextUntil(position, _namespaceEnders);
            return new NamespaceNode(contents);
        }
        else
        {
            throw new ArgumentException("Top-level statements not supported yet!");
        }
    }
}