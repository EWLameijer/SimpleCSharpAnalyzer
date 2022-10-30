namespace DTOsAndUtilities;

public class FileAsLines
{
    public string ContextedFilename => FileUtils.GetContextedFilename(FilePath);

    public string FilePath { get; init; }

    public IReadOnlyList<string> Lines { get; init; }

    public FileAsLines(string filepath)
    {
        Lines = File.ReadAllLines(filepath);
        FilePath = filepath;
    }
}