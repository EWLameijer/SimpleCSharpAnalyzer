using Tokenizing;

namespace DTOsAndUtilities;

public class FileAsTokens
{
    public string ContextedFilename { get; }
    public IReadOnlyList<Token> Tokens { get; }

    public FileAsTokens(string contextedFilename, IReadOnlyList<Token> tokens)
    {
        ContextedFilename = contextedFilename;
        Tokens = tokens;
    }
}