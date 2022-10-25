namespace Tokenizing;

public class Token
{
    public TokenType TokenType { get; set; }

    private readonly Dictionary<TokenType, string> _tokenTypeRepresentations = new()
    {
        [TokenType.Assign] = "=",
        [TokenType.BracesClose] = "}",
        [TokenType.BracesOpen] = "{",
        [TokenType.BracketsOpen] = "[",
        [TokenType.BracketsClose] = "]",
        [TokenType.Comma] = ",",
        [TokenType.ExclamationMark] = "!",
        [TokenType.FatArrow] = "=>",
        [TokenType.Greater] = ">",
        [TokenType.Less] = "<",
        [TokenType.ParenthesesOpen] = "(",
        [TokenType.ParenthesesClose] = ")",
        [TokenType.Period] = "."
    };

    public virtual string PrettyPrint() =>
        _tokenTypeRepresentations.GetValueOrDefault(TokenType, TokenType.ToString());

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
            TokenType.InterpolatedStringStart => $"{Info}(ISS)",
            TokenType.InterpolatedStringMiddle => $"{Info}(ISM)",
            TokenType.InterpolatedStringEnd => $"{Info}(ISE)",
            _ => TokenType.ToString()
        };
    }

    public override string ToString()
    {
        return $"{TokenType} ({Info})";
    }
}