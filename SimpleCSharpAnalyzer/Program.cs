using CSharpParser;
using Tokenizing;

string pathname;
if (args.Length == 0)
{
    Console.Write("Geef de naam van de directory waarvan je de code-regels wilt tellen: ");
    pathname = Console.ReadLine()!;
}
else
{
    pathname = args[0];
}

List<string> csFiles = Directory.GetFiles(pathname, "*.cs", SearchOption.AllDirectories).ToList();
csFiles.ForEach(Console.WriteLine);
Console.WriteLine();
IEnumerable<string> relevantFileNames = csFiles.Where(
    fn => !fn.Contains(@"\Debug\") && !fn.Contains(@"\Release\") &&
    !fn.Contains(@"\Migrations\") && !fn.Contains(@".Designer.cs"));
Report totalReport = new();
foreach (string relevantFileName in relevantFileNames)
{
    string[] lines = File.ReadAllLines(relevantFileName);
    Tokenizer tokenizer = new(lines.ToList());
    string dirName = new DirectoryInfo(Path.GetDirectoryName(relevantFileName)!).Name;
    string filename = $"{dirName}\\{Path.GetFileName(relevantFileName)}";
    Console.WriteLine($"\n***{filename}***");

    while (tokenizer.HasNextToken())
    {
        Token? actualToken = tokenizer.Get();
    }
    LineCounter counter = new(tokenizer.Results());
    Report report = counter.CreateReport();
    report.Show();
    totalReport.Add(report);
}
Console.WriteLine("\n***TOTAL***");
totalReport.Show();