using static DTOsAndUtilities.AttentionCategory;

namespace DTOsAndUtilities;

public record CommentData(string Path, string Comment, string PrecedingContext,
    string FollowingContext);

public class Scoring
{
    public int Correct { get; set; }
    public int NotYetCorrect { get; set; }

    public void Merge(Scoring other)
    {
        Correct += other.Correct;
        NotYetCorrect += other.NotYetCorrect;
    }
}

public enum AttentionCategory
{ AttentionCategoryNotSet, DefaultIdentifierNaming }

public class Report
{
    private readonly Dictionary<AttentionCategory, string> _translation = new()
    {
        [DefaultIdentifierNaming] = "Default identifier naming"
    };

    public void ScoreCorrect(AttentionCategory category) =>
        _scoresFor[category].Correct++;

    public void ScoreNotYetCorrect(AttentionCategory category) =>
        _scoresFor[category].NotYetCorrect++;

    private readonly Dictionary<AttentionCategory, Scoring> _scoresFor = new()
    {
        [DefaultIdentifierNaming] = new()
    };

    public int SetupLines { get; set; }
    public int EmptyLines { get; set; }
    public int BraceLines { get; set; }
    public int CodeLines { get; set; }
    public int CommentLines { get; set; }

    public int TotalLines => SetupLines + EmptyLines + BraceLines + CodeLines + CommentLines;

    public int ExtraCodeLines { get; set; }

    public List<string> Warnings { get; set; } = new();

    public List<CommentData> Comments { get; set; } = new();

    public void Add(Report other)
    {
        SetupLines += other.SetupLines;
        EmptyLines += other.EmptyLines;
        BraceLines += other.BraceLines;
        CodeLines += other.CodeLines;
        CommentLines += other.CommentLines;
        ExtraCodeLines += other.ExtraCodeLines;
        Warnings.AddRange(other.Warnings);
        Comments.AddRange(other.Comments);
        MergeScores(other);
    }

    private void MergeScores(Report other)
    {
        foreach (string key in _scoresFor.Keys)
        {
            _scoresFor[key].Merge(other._scoresFor[key]);
        }
    }

    public void Show()
    {
        ShowPadded("For usings/namespace", SetupLines);
        ShowPadded("Empty lines", EmptyLines);
        ShowPadded("Commentlines", CommentLines);
        ShowPadded("Brace lines", BraceLines);
        ShowPadded("Codelines", CodeLines);
        ShowPadded("Total lines", TotalLines);
        Console.WriteLine();
        for (int i = 0; i < Warnings.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {Warnings[i]}\n");
        }
        Console.WriteLine(Warnings.Count + " warnings in total");
        Console.WriteLine(ExtraCodeLines + " extra code lines");
    }

    private static void ShowPadded(string text, int number)
    {
        Console.Write((text + ":").PadRight(22));
        Console.WriteLine(number.ToString().PadLeft(4));
    }
}