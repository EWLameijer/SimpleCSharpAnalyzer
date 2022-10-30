using DTOsAndUtilities;
using FileHandling;
using Tokenizing;

namespace SimpleCSharpAnalyzer.Tests;

internal static class Utilities
{
    internal static (FileAsTokens, Report) Setup(string text)
    {
        Tokenizer tokenizer = new(text.Split('\n'));
        IReadOnlyList<Token> tokens = tokenizer.Results();
        IReadOnlyList<Token> tokensWithoutAttributes = new TokenFilterer().Filter(tokens);
        LineCounter counter = new(tokens);
        return (new("", tokensWithoutAttributes, ""), counter.CreateReport());
    }
}