using Tokenizing;

namespace DTOsAndUtilities;

public class FileAsTokens
{
    public string ContextedFilename => FileUtils.GetContextedFilename(FilePath);
    public IReadOnlyList<Token> Tokens { get; }

    public string FilePath { get; init; }

    public FileAsTokens(string filePath, IReadOnlyList<Token> tokens)
    {
        FilePath = filePath;
        Tokens = tokens;
    }
}