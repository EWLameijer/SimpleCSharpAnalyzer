using DTOsAndUtilities;

namespace CommentChecker;

internal record CommentContext(string Filepath, string PrecedingContext, string FollowingContext);

internal static class CommentMerger
{
    public static void Merge(Report report, string pathName)
    {
        Dictionary<string, List<CommentContext>> mergedComments = MergeComments(report);
        List<string> toStorage = new();
        List<string> ignoredLines = CommentArchiver.GetFromStorage(pathName);
        HandleEachMergedComment(mergedComments, toStorage, ignoredLines);
        List<string> newEntries = toStorage.Select(k => CommentArchiver.ToStorageFormat(k)).ToList();
        ignoredLines.AddRange(newEntries);
        CommentArchiver.Store(pathName, ignoredLines);
    }

    private static void HandleEachMergedComment(Dictionary<string,
        List<CommentContext>> mergedComments, List<string> toStorage, List<string> ignoredLines)
    {
        foreach (KeyValuePair<string, List<CommentContext>> entry in mergedComments)
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

    private static void DisplayMergedComment(KeyValuePair<string, List<CommentContext>> entry)
    {
        Console.WriteLine("\n************************************************************\n");
        IEnumerable<IGrouping<string, CommentContext>> occurrences = entry.Value.GroupBy(p => p.Filepath);
        string result = string.Join(", ", occurrences.Select(o => $"{o.Count()}x {o.Key}"));
        CommentContext commentWithContext = entry.Value[0];
        Console.Write(commentWithContext.PrecedingContext);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(entry.Key);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(commentWithContext.FollowingContext);
        Console.WriteLine($"\n[{result}]\n");
    }

    private static Dictionary<string, List<CommentContext>> MergeComments(Report report)
    {
        Dictionary<string, List<CommentContext>> mergedComments = new();
        foreach (CommentData commentData in report.Comments)
        {
            MergeCommentIntoResult(mergedComments, commentData);
        }

        return mergedComments;
    }

    private static void MergeCommentIntoResult(Dictionary<string,
        List<CommentContext>> mergedComments, CommentData commentData)
    {
        string path = commentData.Path;
        string comment = commentData.Comment;
        if (!mergedComments.ContainsKey(comment))
        {
            mergedComments[comment] = new List<CommentContext>();
        }
        mergedComments[comment].Add(new CommentContext(path,
            commentData.PrecedingContext, commentData.FollowingContext));
    }
}