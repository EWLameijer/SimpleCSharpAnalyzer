using Tokenizing;
using static Tokenizing.TokenType;

namespace Parser;

public class ParsePosition
{
    public int CurrentIndex { get; set; }
    public IReadOnlyList<Token> Tokens { get; set; }

    public Token CurrentToken() => Tokens[CurrentIndex];

    public TokenType CurrentTokenType() => Tokens[CurrentIndex].TokenType;

    public void Proceed() => CurrentIndex++;
}

public class CreateTree
{
    private readonly ParsePosition _position;

    private TokenType CurrentTokenType() => _position.Tokens[_position.CurrentIndex].TokenType;

    private void Proceed() => _position.CurrentIndex++;

    public CreateTree(IReadOnlyList<Token> tokens)
    {
        _position = new ParsePosition { CurrentIndex = 0, Tokens = tokens };
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
        // expect an using, or a namespace, or something else
        List<UsingDirectiveNode> usingDirectives = new();
        do
        {
            SkipWhitespace();
            if (CurrentTokenType() == Using) usingDirectives.Add(UsingDirectiveNode.Get(_position));
            else if (CurrentTokenType() == Namespace) { }
            else { }
        } while (true);
    }

    private void SkipWhitespace()
    {
        while (CurrentTokenType().IsSkippable()) Proceed();
    }

    public class Node
    { }

    public class UsingsNode : Node
    { }

    public class UsingDirectiveNode : Node
    {
        private readonly IReadOnlyList<Token> _contents;

        public UsingDirectiveNode(List<Token> contents)
        {
            _contents = contents;
        }

        public static UsingDirectiveNode Get(ParsePosition position)
        {
            List<Token> contents = new();
            while (position.CurrentTokenType() != SemiColon)
            {
                contents.Add(position.CurrentToken());
                position.Proceed();
            }
            return new UsingDirectiveNode(contents);
        }
    }

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
}