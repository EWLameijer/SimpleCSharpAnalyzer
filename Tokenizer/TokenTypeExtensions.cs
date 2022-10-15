namespace Tokenizing;

using static TokenType;

/*
 * Testing commentlines
 * */

public static class TokenTypeExtensions
{
    public static bool IsCommentType(this TokenType tokenType)
    {
        List<TokenType> commentTypes = new() { BlockCommentEnd, BlockCommentMiddle,
            BlockCommentStart, BlockCommentWhole, LineComment};
        return commentTypes.Contains(tokenType);
    }

    public static bool IsBraceType(this TokenType tokenType)
    {
        List<TokenType> braceTypes = new() { BracesClose, BracesOpen };
        return braceTypes.Contains(tokenType);
    }
}