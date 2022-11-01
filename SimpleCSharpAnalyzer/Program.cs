using DTOsAndUtilities;
using FileHandling;

FileRepository fileRepository = new(args,
    @"Code-analyzer (edit 'settings.txt' to adjust its sensitivity)
Please enter the name of the directory which contains the code you wish to analyze: ");

bool shouldContinue;
do
{
    FileProcessor fileProcessor = new(fileRepository);
    Report totalReport = fileProcessor.Process(AnalysisMode.Full);

    Console.WriteLine($"\n***TOTAL ({fileRepository.PathName})***");

    shouldContinue = totalReport.ShowTotal();
} while (shouldContinue);
Console.WriteLine("\nThank you for using the SimpleCSharpAnalyzer. Happy coding!");
Environment.Exit(0);