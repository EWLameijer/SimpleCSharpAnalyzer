using Tokenizing;

namespace TokenBasedChecking;

public static class InappropriateAtsHandler
{
    public static List<string> GetWarnings(string contextedFilename, IReadOnlyList<Token> tokens)
    {
        List<string> warnings = new();
        for (int i = 0; i < tokens.Count; i++)
        {
            Token current = tokens[i];
            if (current.TokenType == TokenType.Identifier)
            {
                WarnIfIdentifierInappropiatelyStartsWithAt(contextedFilename, warnings, current);
            }
        }
        return warnings;
    }

    private static void WarnIfIdentifierInappropiatelyStartsWithAt(string contextedFilename,
        List<string> warnings, Token current)
    {
        string identifierName = ((ComplexToken)current).Info;
        if (identifierName[0] == '@')
        {
            string restOfName = identifierName.Substring(1);
            if (!AllCSharpKeywords.KeyWords.Contains(restOfName))
            {
                warnings.Add($"Unnecessary '@' in {identifierName} (in {contextedFilename}).");
            }
        }
    }
}