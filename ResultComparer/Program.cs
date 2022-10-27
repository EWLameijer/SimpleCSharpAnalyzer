// See https://aka.ms/new-console-template for more information
using ResultsComparer;

Console.WriteLine("Hello, World!");

string firstFilePath = "D:\\Development\\ITvitae\\C#\\SimpleCSharpAnalyzer\\comparing_tests\\t1_9_n.txt"; //args[0];
//string secondFilePath = args[1];
Console.WriteLine($"Analyzing {firstFilePath}");
string[] lines = File.ReadAllLines(firstFilePath);

// skip lines until ***TOTAL reached
List<string> reportLines = lines.SkipWhile(line => !line.StartsWith("***TOTAL")).
    SkipWhile(line => !line.StartsWith("1.")).
    TakeWhile(line => line == "" || StartsWithNumberPoint(line)).Where(line => line != "").ToList();
IEnumerable<WarningData> errorData = reportLines.Select(line => new WarningData(line));
foreach (WarningData ed in errorData) Console.WriteLine(ed);
Console.WriteLine("Now merging...");
IOrderedEnumerable<IGrouping<string, WarningData>> groups = errorData.ToLookup(e => e.WarningType).OrderBy(w => w.Key);
foreach (IGrouping<string, WarningData>? g in groups)
{
    Console.WriteLine(g.Key);
    var ordered = g.OrderBy(v => v.ToString());
    foreach (WarningData? q in ordered) Console.WriteLine(q);
}

// then skip until 1. reached
// while line starts with number followed by period
// parse line
// Show results

bool StartsWithNumberPoint(string line)
{
    if (line.Length < 2) return false;
    List<char> number = line.TakeWhile(c => char.IsDigit(c)).ToList();
    if (number.Count == 0) return false;
    return line.Length > number.Count && line[number.Count] == '.';
}