using static Tokenizing.TokenType;

namespace Tokenizing;

public class Token
{
    public TokenType TokenType { get; set; }

    private readonly Dictionary<TokenType, string> _tokenTypeRepresentations = new()
    {
        [Assign] = "=",
        [BracesClose] = "}",
        [BracesOpen] = "{",
        [BracketsOpen] = "[",
        [BracketsClose] = "]",
        [Comma] = ",",
        [ExclamationMark] = "!",
        [FatArrow] = "=>",
        [Greater] = ">",
        [Less] = "<",
        [ParenthesesOpen] = "(",
        [ParenthesesClose] = ")",
        [Period] = "."
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
            BlockCommentStart or BlockCommentMiddle or BlockCommentWhole or BlockCommentEnd => $"/* {Info} */",
            LineComment => $"// {Info}",
            Identifier => $"{Info}(Id)",
            TokenType.String => $"{Info}(STR)",
            InterpolatedStringStart => $"{Info}(ISS)",
            InterpolatedStringMiddle => $"{Info}(ISM)",
            InterpolatedStringEnd => $"{Info}(ISE)",
            _ => TokenType.ToString()
        };
    }

    public override string ToString()
    {
        return $"{TokenType} ({Info})";
    }
}