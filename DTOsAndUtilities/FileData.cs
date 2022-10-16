namespace DTOsAndUtilities;

public class FileData
{
    public string ContextedFilename { get; init; }

    public IReadOnlyList<string> Lines { get; init; }

    public FileData(string filepath)
    {
        Lines = File.ReadAllLines(filepath);
        string dirName = new DirectoryInfo(Path.GetDirectoryName(filepath)!).Name;
        ContextedFilename = $"{dirName}\\{Path.GetFileName(filepath)}";
    }
}