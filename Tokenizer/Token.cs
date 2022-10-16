namespace Tokenizing;

public class Token
{
    public TokenType TokenType { get; set; }

    public virtual string PrettyPrint()
    {
        return TokenType switch
        {
            TokenType.Assign => "=",
            TokenType.Period => ".",
            TokenType.ParenthesesOpen => "(",
            TokenType.ParenthesesClose => ")",
            TokenType.BracketsOpen => "[",
            TokenType.BracketsClose => "]",
            TokenType.ExclamationMark => "!",
            _ => TokenType.ToString()
        };
    }

    public override string ToString()
    {
        return TokenType.ToString();
    }
}

public class ComplexToken : Token
{
    public string Info { get; init; } = null!;

    public override string PrettyPrint()
    {
        return TokenType switch
        {
            TokenType.Identifier => $"{Info}(Id)",
            TokenType.String => $"{Info}(STR)",
            _ => TokenType.ToString()
        };
    }

    public override string ToString()
    {
        return $"{TokenType} ({Info})";
    }
}