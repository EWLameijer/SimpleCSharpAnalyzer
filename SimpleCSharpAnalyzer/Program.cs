using DTOsAndUtilities;
using FileHandling;

FileProcessor fileProcessor = new(args,
    @"Code-analyzer (edit 'settings.txt' to adjust its sensitivity)
Please enter the name of the directory which contains the code you wish to analyze: ");

Report totalReport = fileProcessor.Process(AnalysisMode.Full);

Console.WriteLine($"\n***TOTAL ({fileProcessor.PathName})***");
totalReport.Show();