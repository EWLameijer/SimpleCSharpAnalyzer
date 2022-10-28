using DTOsAndUtilities;
using FileHandling;

string pathname;
if (args.Length == 0)
{
    Console.WriteLine("Code-analyzator (edit de settings.txt om de gevoeligheid aan te passen)");
    Console.Write("Geef de naam van de directory waarvan je de code wilt analyseren: ");
    pathname = Console.ReadLine()!;
}
else
{
    pathname = args[0];
}

FileProcessor fileProcessor = new FileProcessor(pathname);
Report totalReport = fileProcessor.Process();

Console.WriteLine($"\n***TOTAL ({pathname})***");
totalReport.Show();