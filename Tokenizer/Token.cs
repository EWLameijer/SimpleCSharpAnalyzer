using static Tokenizing.TokenType;

namespace Tokenizing;

public class Token
{
    public TokenType TokenType { get; set; }

    public int LineNumber { get; init; }

    // for starting character
    public int CharacterIndex { get; set; }

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
        [LogicAnd] = "&&",
        [Newline] = "\n",
        [ParenthesesOpen] = "(",
        [ParenthesesClose] = ")",
        [Period] = ".",
        [QuestionMark] = "?",
        [Semicolon] = ";"
    };

    public Token(TokenType tokenType, int lineNumber, int characterIndex)
    {
        TokenType = tokenType;
        LineNumber = lineNumber;
        CharacterIndex = characterIndex;
    }

    public virtual string PrettyPrint() =>
        _tokenTypeRepresentations.GetValueOrDefault(TokenType, TokenType.ToString());

    public override string ToString()
    {
        return TokenType.ToString();
    }
}

public class ComplexToken : Token
{
    public ComplexToken(TokenType tokenType, int lineNumber, int characterIndex, string info) :
        base(tokenType, lineNumber, characterIndex)
    {
        Info = info;
    }

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