using DTOsAndUtilities;

namespace CommentChecker;

internal static class CommentMerger
{
    public static void Merge(Report report, string pathName)
    {
        Dictionary<string, List<string>> mergedComments = MergeComments(report);
        List<string> toStorage = new();
        string storageFilename = pathName.Replace("\\", "_") + ".txt";
        string[] ignoredLines = File.ReadAllLines(storageFilename);
        foreach (KeyValuePair<string, List<string>> entry in mergedComments)
        {
            if (ignoredLines.Contains(entry.Key)) continue;
            DisplayMergedComment(entry);

            AskForAction(toStorage, entry);
        }

        File.WriteAllLines(storageFilename, toStorage);
    }

    private static void AskForAction(List<string> toStorage, KeyValuePair<string, List<string>> entry)
    {
        Console.WriteLine("What do you want to do with this comment?");
        Console.WriteLine("a. Approve: it is stored and you won't be asked about it again");
        Console.WriteLine("b. Handle: remove it from the file(s)");
        Console.WriteLine("c. Skip (for now), decide how to handle it later.");
        char answer = char.ToLower(Console.ReadKey().KeyChar!);
        if (answer == 'a') toStorage.Add(entry.Key);
    }

    private static void DisplayMergedComment(KeyValuePair<string, List<string>> entry)
    {
        IEnumerable<IGrouping<string, string>> occurrences = entry.Value.GroupBy(p => p);
        string result = string.Join(", ", occurrences.Select(o => $"{o.Count()}x {string.Join(", ", o)}"));
        Console.WriteLine($"\n\n{entry.Key}\n[{result}]\n");
    }

    private static Dictionary<string, List<string>> MergeComments(Report report)
    {
        Dictionary<string, List<string>> mergedComments = new();
        foreach (string warning in report.Warnings)
        {
            MergeCommentIntoResult(mergedComments, warning);
        }

        return mergedComments;
    }

    private static void MergeCommentIntoResult(Dictionary<string, List<string>> mergedComments, string warning)
    {
        // example input line:
        // "Commented-out code in SimpleCSharpAnalyzer\CommentChecker\CommentMerger.cs: // raw strings are NOT numbered"
        const int FilePathIndex = 3;
        string[] parts = warning.Split(' ');
        string path = parts[FilePathIndex][..^1];
        string comment = string.Join(" ", parts.Skip(FilePathIndex + 1)).Trim();
        if (!mergedComments.ContainsKey(comment))
        {
            mergedComments[comment] = new List<string>();
        }
        mergedComments[comment].Add(path);
    }
}