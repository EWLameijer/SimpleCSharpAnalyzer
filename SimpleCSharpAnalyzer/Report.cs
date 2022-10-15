namespace CSharpParser;

internal class Report
{
    public int SetupLines { get; set; }
    public int EmptyLines { get; set; }
    public int BraceLines { get; set; }
    public int CodeLines { get; set; }
    public int CommentLines { get; set; }

    public int TotalLines => SetupLines + EmptyLines + BraceLines + CodeLines + CommentLines;

    public void Add(Report other)
    {
        SetupLines += other.SetupLines;
        EmptyLines += other.EmptyLines;
        BraceLines += other.BraceLines;
        CodeLines += other.CodeLines;
        CommentLines += other.CommentLines;
    }

    public void Show()
    {
        ShowPadded("For usings/namespace", SetupLines);
        ShowPadded("Empty lines", EmptyLines);
        ShowPadded("Commentlines", CommentLines);
        ShowPadded("Brace lines", BraceLines);
        ShowPadded("Codelines", CodeLines);
        ShowPadded("Total lines", TotalLines);
    }

    private void ShowPadded(string text, int number)
    {
        Console.Write((text + ":").PadRight(22));
        Console.WriteLine(number.ToString().PadLeft(4));
    }
}