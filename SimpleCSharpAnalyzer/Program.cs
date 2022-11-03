using DTOsAndUtilities;
using FileHandling;
using TokenBasedChecking;

FileRepository fileRepository = new(args,
    @"Code-analyzer (edit 'settings.txt' to adjust its sensitivity)
Please enter the name of the directory which contains the code you wish to analyze: ");

bool shouldContinue;
do
{
    FileProcessor fileProcessor = new(fileRepository);
    Report totalReport = fileProcessor.Process(AnalysisMode.Full);

    Console.WriteLine($"\n***TOTAL ({fileRepository.PathName})***");

    if (WarningSettings.UseLevels)
    {
        shouldContinue = totalReport.ShowTotal();
    }
    else
    {
        totalReport.Show();
        break;
    }
} while (shouldContinue);
Console.WriteLine("\nThank you for using the SimpleCSharpAnalyzer. Happy coding!");
Environment.Exit(0);