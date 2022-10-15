namespace Tokenizing;

internal static class Keywords
{
    internal static Dictionary<string, TokenType> dict = new()
    {
        ["break"] = TokenType.Break,
        ["catch"] = TokenType.Catch,
        ["class"] = TokenType.Class,
        ["do"] = TokenType.Do,
        ["else"] = TokenType.Else,
        ["enum"] = TokenType.Enum,
        ["false"] = TokenType.False,
        ["for"] = TokenType.For,
        ["get"] = TokenType.Get,
        ["global"] = TokenType.Global,
        ["if"] = TokenType.If,
        ["internal"] = TokenType.Internal,
        ["init"] = TokenType.Init,
        ["interface"] = TokenType.Interface,
        ["namespace"] = TokenType.Namespace,
        ["new"] = TokenType.New,
        ["null"] = TokenType.Null,
        ["out"] = TokenType.Out,
        ["override"] = TokenType.Override,
        ["private"] = TokenType.Private,
        ["public"] = TokenType.Public,
        ["readonly"] = TokenType.Readonly,
        ["return"] = TokenType.Return,
        ["set"] = TokenType.Set,
        ["throw"] = TokenType.Throw,
        ["true"] = TokenType.True,
        ["try"] = TokenType.Try,
        ["using"] = TokenType.Using,
        ["while"] = TokenType.While
    };
}