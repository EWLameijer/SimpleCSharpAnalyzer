using Tokenizing;

namespace Parser;

public class CreateTree
{
    private readonly ParsePosition _position;

    public CreateTree(IReadOnlyList<Token> tokens)
    {
        _position = new ParsePosition { CurrentIndex = 0, Tokens = tokens };
    }

    public FileNode Parse()
    {
        return FileNode.Get(_position);
    }

    private NamespaceNode GetNameSpace()
    {
        throw new NotImplementedException();
    }
}