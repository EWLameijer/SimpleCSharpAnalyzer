namespace Tokenizing;

using static TokenType;

public static class TokenTypeExtensions
{
    public static bool IsCommentType(this TokenType tokenType)
    {
        List<TokenType> commentTypes = new() { BlockCommentEnd, BlockCommentMiddle,
            BlockCommentStart, BlockCommentWhole, DocComment, LineComment};
        return commentTypes.Contains(tokenType);
    }

    public static bool IsSkippable(this TokenType tokenType)
    {
        return tokenType == Newline || tokenType.IsCommentType();
    }

    public static bool IsBraceType(this TokenType tokenType)
    {
        List<TokenType> braceTypes = new() { BracesClose, BracesOpen };
        return braceTypes.Contains(tokenType);
    }

    public static bool IsClosingType(this TokenType tokenType)
    {
        List<TokenType> closeTypes = new() { BracesClose, BracketsClose, ParenthesesClose, Greater };
        return closeTypes.Contains(tokenType);
    }

    public static bool IsOpeningType(this TokenType tokenType)
    {
        List<TokenType> openTypes = new() { BracesOpen, BracketsOpen, ParenthesesOpen, Less };
        return openTypes.Contains(tokenType);
    }

    public static bool IsModifier(this TokenType tokenType)
    {
        List<TokenType> modifiers = new() { Async, Const, Internal, Override, Public, Private,
            Protected, Readonly, Static, Using, Virtual };
        return modifiers.Contains(tokenType);
    }

    public static bool IsDeclarer(this TokenType tokenType)
    {
        List<TokenType> declarers = new() { Namespace, Using };
        return declarers.Contains(tokenType);
    }

    public static bool IsTypeType(this TokenType tokenType)
    {
        List<TokenType> typeTypes = new() { Class, Enum, Record, Struct };
        return typeTypes.Contains(tokenType);
    }
}