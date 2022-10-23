using Tokenizing;

namespace Parser;

public class ParsePosition
{
    public int CurrentIndex { get; set; }

    public IReadOnlyList<Token> Tokens { get; set; } = null!;

    public Token CurrentToken() => Tokens[CurrentIndex];

    public TokenType CurrentTokenType() => Tokens[CurrentIndex].TokenType;

    public void Proceed() => CurrentIndex++;

    public void SkipWhitespace()
    {
        while (CurrentTokenType().IsSkippable()) Proceed();
    }
}