using static DTOsAndUtilities.AttentionCategory;

namespace DTOsAndUtilities;

public record CommentData(string Path, string Comment, string PrecedingContext,
    string FollowingContext);

public class Scoring
{
    public int Correct { get; set; }
    public int NotYetCorrect { get; set; }

    public int Total => Correct + NotYetCorrect;

    public double Percentage => 100.0 * Correct / Total;

    public void Merge(Scoring other)
    {
        Correct += other.Correct;
        NotYetCorrect += other.NotYetCorrect;
    }
}

public enum AttentionCategory
{
    AttentionCategoryNotSet = 0,
    DefaultIdentifierNaming = 1, // identifier names + inappropriate @s
    VeryVeryLongLines = 2, // lines not too long (140)
    VeryVeryLongMethods = 3,
    MissingBlankLines = 4,
    BadlyFormattedComments = 5,
    WrongSynonyms = 6,
    UncheckedComments = 7,
    VeryLongLines = 8,
    VeryLongMethods = 9,
    ToDoComments = 10
}

public record Warning(string Text, AttentionCategory Category);

public class Report
{
    private const ConsoleColor Green = ConsoleColor.Green;
    private const ConsoleColor Yellow = ConsoleColor.Yellow;

    private readonly Dictionary<AttentionCategory, string> _description = new()
    {
        [AttentionCategoryNotSet] = "PLEASE REPORT THIS BUG",
        [DefaultIdentifierNaming] = "Identifier names according to standards",
        [VeryVeryLongLines] = $"Non-test-file lines under {WarningSettings.BasicMaxLineLength} " +
            "characters long",
        [VeryVeryLongMethods] = $"Methods under {WarningSettings.BasicMaxMethodLength + 1} " +
            "lines long",
        [MissingBlankLines] = "Blank lines before method definitions",
        [BadlyFormattedComments] = "Properly formatted comments",
        [WrongSynonyms] = "Proper type synonyms",
        [UncheckedComments] = "Comments checked and approved",
        [VeryLongLines] = $"Non-test-file lines under {WarningSettings.IdealMaxLineLength} " +
            "characters long",
        [VeryLongMethods] = $"Methods under {WarningSettings.IdealMaxMethodLength + 1} lines long",
        [ToDoComments] = "TODO comments left of total"
    };

    public void ScoreCorrect(AttentionCategory category) =>
        _scoresFor[category].Correct++;

    public void ScoreNotYetCorrect(AttentionCategory category) =>
        _scoresFor[category].NotYetCorrect++;

    private readonly Dictionary<AttentionCategory, Scoring> _scoresFor = new();

    // "Enum.GetValues<AttentionCategory>().Select(v => _scoresFor[v] = new());" does not work.
    // Likely optimized away...
    public Report()
    {
        Enum.GetValues<AttentionCategory>().ToList().ForEach(v => _scoresFor[v] = new());
    }

    public int SetupLines { get; set; }
    public int EmptyLines { get; set; }
    public int BraceLines { get; set; }
    public int CodeLines { get; set; }
    public int CommentLines { get; set; }

    public int TotalLines => SetupLines + EmptyLines + BraceLines + CodeLines + CommentLines;

    public int ExtraCodeLines { get; set; }

    private List<Warning> Warnings { get; set; } = new();

    public IReadOnlyList<Warning> GetWarnings() => Warnings;

    public void AddNonScoredWarning(string warning)
    {
        Warnings.Add(new Warning(warning, AttentionCategoryNotSet));
    }

    public void AddWarning(AttentionCategory category, string warning)
    {
        Warnings.Add(new Warning(warning, category));
        ScoreNotYetCorrect(category);
    }

    public void AddNonScoredWarnings(IEnumerable<string> warnings)
    {
        Warnings.AddRange(warnings.Select(w => new Warning(w, AttentionCategoryNotSet)));
    }

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
        foreach (AttentionCategory key in _scoresFor.Keys)
        {
            _scoresFor[key].Merge(other._scoresFor[key]);
        }
    }

    public void Show()
    {
        ShowLineCounts();
        for (int i = 0; i < Warnings.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {Warnings[i].Text}\n");
        }
        Console.WriteLine(Warnings.Count + " warnings in total");
        Console.WriteLine(ExtraCodeLines + " extra code lines");
    }

    private void ShowLineCounts()
    {
        ShowPadded("For usings/namespace", SetupLines);
        ShowPadded("Empty lines", EmptyLines);
        ShowPadded("Commentlines", CommentLines);
        ShowPadded("Brace lines", BraceLines);
        ShowPadded("Codelines", CodeLines);
        ShowPadded("Total lines", TotalLines);
        Console.WriteLine();
    }

    public bool ShowTotal()
    {
        FinalizeMethodAndLineScores();
        ShowLineCounts();
        for (int i = 1; i <= Enum.GetValues<AttentionCategory>().Select(a => (int)a).Max(); i++)
        {
            Scoring scores = _scoresFor[(AttentionCategory)i];

            OutputWithPercentage(i, scores);
            if (scores.NotYetCorrect != 0)
            {
                return LetUserChooseBetweenFixingAndQuitting(i, scores);
            }
        }
        return false;
    }

    private void FinalizeMethodAndLineScores()
    {
        _scoresFor[VeryVeryLongMethods].Correct = _scoresFor[VeryLongMethods].NotYetCorrect +
            _scoresFor[VeryLongMethods].Correct;
        _scoresFor[VeryVeryLongLines].Correct = _scoresFor[VeryLongLines].NotYetCorrect +
            _scoresFor[VeryLongLines].Correct;
    }

    private bool LetUserChooseBetweenFixingAndQuitting(int i, Scoring scores)
    {
        int numWarnings = ShowCodeStatus(i, scores);
        string reply = Console.ReadKey(true).Key.ToString();
        if (reply.ToUpper() == "Q") return false;
        List<Warning> warningsToDisplay = Warnings.Where(
            w => w.Category == (AttentionCategory)i).Take(numWarnings).ToList();
        for (int j = 0; j < numWarnings; j++)
            Console.WriteLine($"{j + 1}. {warningsToDisplay[j].Text}\n");
        Console.WriteLine("Press any key to retry...");
        Console.ReadKey(true);
        return true;
    }

    private static int ShowCodeStatus(int i, Scoring scores)
    {
        int numWarnings = Math.Min(scores.NotYetCorrect, 7);
        double extraLevel = scores.Percentage / 100;
        if (extraLevel < 1.0 && extraLevel > 0.99) extraLevel = 0.99;
        double levelReached = Math.Round(i + extraLevel, 2);
        Console.WriteLine($"You have reached level {levelReached}. Do you want to (S)ee " +
            $"the next {numWarnings} warnings or (Q)uit?");
        return numWarnings;
    }

    private static void WriteColored(ConsoleColor color, string text)
    {
        DisplayColoredText(color, text, Console.Write);
    }

    private static void WriteColoredLine(ConsoleColor color, string text)
    {
        DisplayColoredText(color, text, Console.WriteLine);
    }

    private static void DisplayColoredText(ConsoleColor color, string text, Action<string> write)
    {
        ConsoleColor previousConsoleColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        write(text);
        Console.ForegroundColor = previousConsoleColor;
    }

    private void OutputWithPercentage(int i, Scoring scores)
    {
        double percentage = scores.Percentage;
        (ConsoleColor color, string extraText) = percentage < 100.0 ?
            (Yellow, "not yet there...") : (Green, "perfect!");
        WriteColoredLine(color, $"Level {i} - {_description[(AttentionCategory)i]}: " +
            $"{extraText} {scores.Correct}/{scores.Total}.");
        for (i = 1; i <= 100; i++)
        {
            ConsoleColor progressColor = i <= percentage ? Green : Yellow;
            WriteColored(progressColor, "=");
        }
        Console.WriteLine("\n");
    }

    private static void ShowPadded(string text, int number)
    {
        Console.Write((text + ":").PadRight(22));
        Console.WriteLine(number.ToString().PadLeft(4));
    }
}