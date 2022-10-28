﻿// goal:
// 1. List the comments found (ALL of them, including one-liners). If the same comment
// occurs multiple times, put it like [file1, file2, file3: 3x] // arrange
//      FileProcessor with option of commentmode: ONLY instead of SIDE
//      Warnings
// 2. give options:
// 2a. approve (store) [all arrange]
// 2b. remove [if opened]
// 2c. skip (if require further action, typically todo-comments)

// NOTE: scanner should remark new comments that have not yet been resolved. Also all TODOs..

using DTOsAndUtilities;
using FileHandling;

FileProcessor fileProcessor = new(args,
    @"Commentaar-analysator:
Geef de naam van de directory waarvan je de code wilt analyseren: ");

Report totalReport = fileProcessor.Process(AnalysisMode.CommentsOnly);

Console.WriteLine($"\n***TOTAL ({fileProcessor.PathName})***");
totalReport.Show();