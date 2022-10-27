// See https://aka.ms/new-console-template for more information

using ResultComparer;

Console.WriteLine("Hello, World!");

string firstFilePath = "D:\\Development\\ITvitae\\C#\\SimpleCSharpAnalyzer\\comparing_tests\\t1_6_o.txt"; //args[0];
string secondFilePath = "D:\\Development\\ITvitae\\C#\\SimpleCSharpAnalyzer\\comparing_tests\\t1_6_n.txt"; //args[0];
//string secondFilePath = args[1];

WarningCollection allWarnings1 = new(firstFilePath);
WarningCollection allWarnings2 = new(secondFilePath);

Console.WriteLine("Now merging...");

Console.WriteLine("Merged!");
Console.WriteLine("FIRST!");
allWarnings1.Show();
Console.WriteLine("SECOND!");
allWarnings2.Show();
Console.WriteLine("Differences?");

allWarnings1.Compare(allWarnings2);

// then skip until 1. reached
// while line starts with number followed by period
// parse line
// Show results