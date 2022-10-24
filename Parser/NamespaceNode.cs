using Parser.TopLevelNodes;
using Tokenizing;
using static Tokenizing.TokenType;

namespace Parser;

public class NamespaceNode : Node
{
    private readonly IReadOnlyList<Token> _nameSpaceName; // empty list = top level statements
    private readonly bool _isFileScoped;
    private readonly List<TopLevelNode> _topLevelNodes;

    private NamespaceNode(IReadOnlyList<Token> namespaceName, bool isFileScoped,
           List<TopLevelNode> topLevelNodes)
    {
        _nameSpaceName = namespaceName;
        _isFileScoped = isFileScoped;
        _topLevelNodes = topLevelNodes;
    }

    private static readonly HashSet<TokenType> _namespaceEnders = new() { SemiColon, BracesOpen };

    public static NamespaceNode Get(ParsePosition position)
    {
        position.SkipWhitespace();
        if (position.CurrentTokenType() == Namespace)
        {
            List<Token> identifierTokens = GetNextUntil(position, _namespaceEnders);
            bool isFileScoped = position.CurrentTokenType() == SemiColon;
            position.Proceed(); // skip ; or {
            List<TopLevelNode> topLevelNodes = new();
            do
            {
                TopLevelNode newNode = TopLevelNode.Get(position);
                if (newNode == null) break;
                topLevelNodes.Add(newNode);
            } while (true);
            return new NamespaceNode(identifierTokens, isFileScoped, topLevelNodes);
        }
        else
        {
            throw new ArgumentException("Top-level statements not supported yet!");
        }
    }
}