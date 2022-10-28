namespace DTOsAndUtilities;

public class FileAsLines
{
    public string ContextedFilename { get; init; }

    public IReadOnlyList<string> Lines { get; init; }

    public FileAsLines(string filepath)
    {
        Lines = File.ReadAllLines(filepath);
        string[] directories = filepath.Split(Path.DirectorySeparatorChar);
        ContextedFilename = string.Join("\\", directories.TakeLast(3));
    }
}