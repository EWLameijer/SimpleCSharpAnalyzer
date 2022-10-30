namespace DTOsAndUtilities;

public class CommentArchiver
{
    public static bool ContainsComment(string basePath, string comment)
    {
        List<string> comments = GetFromStorage(basePath);
        return comments.Contains(ToStorageFormat(comment));
    }

    public static string ToStorageFormat(string input) => input.Trim().Replace("\n", "-\\n");

    public static List<string> GetFromStorage(string pathName)
    {
        string storageFilename = FilenameFromPath(pathName);
        return File.Exists(storageFilename) ?
            File.ReadAllLines(storageFilename).ToList() : new();
    }

    private static string FilenameFromPath(string pathName) =>
        pathName.Replace("\\", "_") + ".txt";

    public static void Store(string pathName, List<string> comments)
    {
        string storageFilename = FilenameFromPath(pathName);
        File.WriteAllLines(storageFilename, comments);
    }
}