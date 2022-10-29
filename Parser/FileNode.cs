namespace Parser;

public class FileNode : Node
{
    private readonly UsingsNode? _usingsNode;
    private readonly NamespaceNode? _namespaceNode;

    public FileNode(UsingsNode usingsNode, NamespaceNode namespaceNode)
    {
        _usingsNode = usingsNode;
        _namespaceNode = namespaceNode;
    }

    public static FileNode Get(ParsePosition position)
    {
        UsingsNode usingsNode = UsingsNode.Get(position); // works!
        NamespaceNode namespaceNode = NamespaceNode.Get(position);
        return new FileNode(usingsNode, namespaceNode);
    }
}