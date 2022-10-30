﻿namespace DTOsAndUtilities;

public record CommentData(string Path, string Comment, string PrecedingContext,
    string FollowingContext);

public class Report
{
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