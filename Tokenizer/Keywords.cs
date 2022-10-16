namespace Tokenizing;

internal static class Keywords
{
    internal static Dictionary<string, TokenType> dict = new()
    {
        ["break"] = TokenType.Break,
        ["case"] = TokenType.Case,
        ["catch"] = TokenType.Catch,
        ["class"] = TokenType.Class,
        ["const"] = TokenType.Const,
        ["do"] = TokenType.Do,
        ["else"] = TokenType.Else,
        ["enum"] = TokenType.Enum,
        ["false"] = TokenType.False,
        ["for"] = TokenType.For,
        ["foreach"] = TokenType.ForEach,
        ["get"] = TokenType.Get,
        ["global"] = TokenType.Global,
        ["if"] = TokenType.If,
        ["init"] = TokenType.Init,
        ["interface"] = TokenType.Interface,
        ["internal"] = TokenType.Internal,
        ["namespace"] = TokenType.Namespace,
        ["new"] = TokenType.New,
        ["null"] = TokenType.Null,
        ["out"] = TokenType.Out,
        ["override"] = TokenType.Override,
        ["private"] = TokenType.Private,
        ["protected"] = TokenType.Protected,
        ["public"] = TokenType.Public,
        ["readonly"] = TokenType.Readonly,
        ["record"] = TokenType.Record,
        ["return"] = TokenType.Return,
        ["set"] = TokenType.Set,
        ["static"] = TokenType.Static,
        ["struct"] = TokenType.Struct,
        ["switch"] = TokenType.Switch,
        ["throw"] = TokenType.Throw,
        ["true"] = TokenType.True,
        ["try"] = TokenType.Try,
        ["using"] = TokenType.Using,
        ["virtual"] = TokenType.Virtual,
        ["where"] = TokenType.Where,
        ["while"] = TokenType.While
    };
}