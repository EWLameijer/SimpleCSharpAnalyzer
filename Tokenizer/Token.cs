namespace Tokenizing;

public class Token
{
    public TokenType TokenType { get; set; }

    public virtual string PrettyPrint()
    {
        return TokenType switch
        {
            TokenType.Assign => "=",
            TokenType.BracesClose => "}",
            TokenType.BracesOpen => "{",
            TokenType.BracketsOpen => "[",
            TokenType.BracketsClose => "]",
            TokenType.Comma => ",",
            TokenType.ExclamationMark => "!",
            TokenType.FatArrow => "=>",
            TokenType.Greater => ">",
            TokenType.Less => "<",
            TokenType.ParenthesesOpen => "(",
            TokenType.ParenthesesClose => ")",
            TokenType.Period => ".",
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