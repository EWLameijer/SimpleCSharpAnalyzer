namespace DTOsAndUtilities;

internal static class FileUtils
{
    internal static string GetContextedFilename(string filePath)
    {
        string[] directories = filePath.Split(Path.DirectorySeparatorChar);
        return string.Join("\\", directories.TakeLast(3));
    }
}