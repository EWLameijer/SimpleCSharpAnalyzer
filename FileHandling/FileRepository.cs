namespace FileHandling;

public class FileRepository
{
    public string PathName { get; private set; } = null!;

    public IReadOnlyList<string> Filenames { get; init; }

    public FileRepository(string[] args, string query)
    {
        GetPathName(args, query);
        List<string> csFiles = Directory.GetFiles(PathName, "*.cs", SearchOption.AllDirectories).ToList();
        Filenames = csFiles.Where(
            fn => !fn.Contains(@"\Debug\") && !fn.Contains(@"\Release\") &&
            !fn.Contains(@"\Migrations\") && !fn.Contains(@".Designer.cs")).ToList();
    }

    public void GetPathName(string[] args, string query)
    {
        if (args.Length == 0)
        {
            Console.Write(query);
            PathName = Console.ReadLine()!;
        }
        else
        {
            PathName = args[0];
        }
    }
}