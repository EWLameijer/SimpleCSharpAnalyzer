using Tokenizing;

namespace DTOsAndUtilities;

public class FileTokenData
{
    public string ContextedFilename { get; }
    public IReadOnlyList<Token> Tokens { get; }

    public FileTokenData(string contextedFilename, IReadOnlyList<Token> tokens)
    {
        ContextedFilename = contextedFilename;
        Tokens = tokens;
    }
}