using DTOsAndUtilities;

namespace CommentChecker;

internal static class CommentMerger
{
    public static void Merge(Report report, string pathName)
    {
        Dictionary<string, List<CommentData>> mergedComments = MergeComments(report);
        List<string> toStorage = new();
        List<string> ignoredLines = CommentArchiver.GetFromStorage(pathName);
        HandleEachMergedComment(mergedComments, toStorage, ignoredLines);
        List<string> newEntries = toStorage.Select(k => CommentArchiver.ToStorageFormat(k)).ToList();
        ignoredLines.AddRange(newEntries);
        CommentArchiver.Store(pathName, ignoredLines);
    }

    private static void HandleEachMergedComment(Dictionary<string,
        List<CommentData>> mergedComments, List<string> toStorage, List<string> ignoredLines)
    {
        foreach (KeyValuePair<string, List<CommentData>> entry in mergedComments)
        {
            string storageFormat = CommentArchiver.ToStorageFormat(entry.Key);
            if (ignoredLines.Contains(storageFormat)) continue;
            DisplayMergedComment(entry);
            AskForAction(toStorage, entry.Key);
        }
    }

    private static void AskForAction(List<string> toStorage, string key)
    {
        Console.WriteLine("What do you want to do with this comment?");
        Console.WriteLine("a. Approve: it is stored and you won't be asked about it again");
        // Console.WriteLine("b. Handle: remove it from the file(s)");
        Console.WriteLine("b. Skip (for now), decide how to handle it later.");
        char answer = char.ToLower(Console.ReadKey().KeyChar!);
        if (answer == 'a') toStorage.Add(key);
    }

    private static void DisplayMergedComment(KeyValuePair<string, List<CommentData>> entry)
    {
        Console.WriteLine("\n************************************************************\n");
        IEnumerable<IGrouping<string, CommentData>> occurrences = entry.Value.GroupBy(p => p.Path);
        string result = string.Join(", ", occurrences.Select(o => $"{o.Count()}x {o.Key}"));
        CommentData commentWithContext = entry.Value[0];
        Console.Write(commentWithContext.PrecedingContext);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(commentWithContext.Comment);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(commentWithContext.FollowingContext);
        Console.WriteLine($"\n[{result}]\n");
    }

    private static Dictionary<string, List<CommentData>> MergeComments(Report report)
    {
        Dictionary<string, List<CommentData>> mergedComments = new();
        foreach (CommentData commentData in report.Comments)
        {
            MergeCommentIntoResult(mergedComments, commentData);
        }

        return mergedComments;
    }

    private static void MergeCommentIntoResult(Dictionary<string,
        List<CommentData>> mergedComments, CommentData commentData)
    {
        string path = commentData.Path;
        string trimmedComment = commentData.Comment.Trim();
        if (!mergedComments.ContainsKey(trimmedComment))
        {
            mergedComments[trimmedComment] = new List<CommentData>();
        }
        mergedComments[trimmedComment].Add(commentData);
    }
}