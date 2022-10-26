namespace DTOsAndUtilities;

public class FileData
{
    public string ContextedFilename { get; init; }

    public IReadOnlyList<string> Lines { get; init; }

    public FileData(string filepath)
    {
        Lines = File.ReadAllLines(filepath);
        string[] directories = filepath.Split(Path.DirectorySeparatorChar);
        ContextedFilename = string.Join("\\", directories.TakeLast(3));
    }
}