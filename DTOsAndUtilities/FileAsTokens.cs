using Tokenizing;

namespace DTOsAndUtilities;

public class FileAsTokens
{
    public string ContextedFilename => FileUtils.GetContextedFilename(FilePath);
    public IReadOnlyList<Token> Tokens { get; }

    public string FilePath { get; init; }
    public string BasePath { get; init; }

    public FileAsTokens(string filePath, IReadOnlyList<Token> tokens, string basePath)
    {
        FilePath = filePath;
        Tokens = tokens;
        BasePath = basePath;
    }
}