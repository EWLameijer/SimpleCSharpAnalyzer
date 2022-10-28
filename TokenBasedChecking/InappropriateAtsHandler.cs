using DTOsAndUtilities;
using Tokenizing;

namespace TokenBasedChecking;

public static class InappropriateAtsHandler
{
    public static List<string> GetWarnings(FileAsTokens fileTokenData)
    {
        List<string> warnings = new();
        IReadOnlyList<Token> tokens = fileTokenData.Tokens;
        for (int i = 0; i < tokens.Count; i++)
        {
            Token current = tokens[i];
            if (current.TokenType == TokenType.Identifier)
            {
                WarnIfIdentifierInappropiatelyStartsWithAt(fileTokenData.ContextedFilename,
                    warnings, current);
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