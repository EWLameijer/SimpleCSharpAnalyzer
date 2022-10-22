using Tokenizing;

namespace Parser;

public class CreateTree
{
    private readonly IReadOnlyList<Token> _tokens;
    private readonly int _currentIndex = 0;

    public CreateTree(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens;
    }

    public FileNode Parse()
    {
        UsingsNode usingsNode = GetUsingDirectives();
        NamespaceNode namespaceNode = GetNameSpace();
        return new FileNode(usingsNode, namespaceNode);
    }

    private NamespaceNode GetNameSpace()
    {
        throw new NotImplementedException();
    }

    private UsingsNode GetUsingDirectives()
    {
        throw new NotImplementedException();
    }
}

public class Node
{ }

public class UsingsNode : Node
{ }

public class NamespaceNode : Node
{ }

public class FileNode : Node
{
    private readonly UsingsNode? _usingsNode;
    private readonly NamespaceNode? _namespaceNode;

    public FileNode(UsingsNode usingsNode, NamespaceNode namespaceNode)
    {
        _usingsNode = usingsNode;
        _namespaceNode = namespaceNode;
    }
}