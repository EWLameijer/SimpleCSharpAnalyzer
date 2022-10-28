using DTOsAndUtilities;
using FileHandling;

FileProcessor fileProcessor = new(args,
    @"Code-analyzator (edit de settings.txt om de gevoeligheid aan te passen)
Geef de naam van de directory waarvan je de code wilt analyseren: ");

Report totalReport = fileProcessor.Process(AnalysisMode.Full);

Console.WriteLine($"\n***TOTAL ({fileProcessor.PathName})***");
totalReport.Show();