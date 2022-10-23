using Tokenizing;
using static Tokenizing.TokenType;

namespace Parser;

public class UsingsNode : Node
{
    private readonly List<UsingDirectiveNode> _usings;

    private UsingsNode(List<UsingDirectiveNode> usings)
    {
        _usings = usings;
    }

    public static UsingsNode Get(ParsePosition position)
    {
        List<UsingDirectiveNode> usingDirectives = new();
        do
        {
            position.SkipWhitespace();
            if (position.CurrentTokenType() == Using)
                usingDirectives.Add(UsingDirectiveNode.Get(position));
            else break;
        } while (true);
        return new UsingsNode(usingDirectives);
    }
}

public class UsingDirectiveNode : Node
{
    private readonly IReadOnlyList<Token> _contents;

    public UsingDirectiveNode(List<Token> contents)
    {
        _contents = contents;
    }

    public static UsingDirectiveNode Get(ParsePosition position) =>
         new(GetNextUntil(position, SemiColon));
}