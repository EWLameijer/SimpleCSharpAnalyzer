namespace DTOsAndUtilities;

internal static class FileUtils
{
    internal static string GetContextedFilename(string filePath)
    {
        string[] directories = filePath.Split(Path.DirectorySeparatorChar);
        return string.Join("\\", directories.TakeLast(3));
    }

    internal static string ExecutablePath()
    {
        string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        return Path.GetDirectoryName(strExeFilePath)!;
    }
}