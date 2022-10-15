namespace Tokenizing;

public class Token
{
    public TokenType TokenType { get; set; }

    public override string ToString()
    {
        return TokenType.ToString();
    }
}

public class ComplexToken : Token
{
    public string Info { get; init; } = null!;

    public override string ToString()
    {
        return $"{TokenType} ({Info})";
    }
}