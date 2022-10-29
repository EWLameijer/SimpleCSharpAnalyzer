// *** If the same comment
// occurs multiple times, put it like [file1, file2, file3: 3x] // arrange
//      FileProcessor with option of commentmode: ONLY instead of SIDE
//      Warnings

// 2. give options:
// 2a. approve (store) [all arrange]
// 2b. remove [if opened]
// 2c. skip (if require further action, typically todo-comments)

// NOTE: scanner should remark new comments that have not yet been resolved. Also all TODOs..
// done  goal:
// 1. List the comments found (ALL of them, including one-liners). DONE

using CommentChecker;
using DTOsAndUtilities;
using FileHandling;

FileProcessor fileProcessor = new(args,
    @"Comments-analyzer.
Please enter the name of the directory which contains the code you wish to analyze: ");

Report totalReport = fileProcessor.Process(AnalysisMode.CommentsOnly);
CommentMerger.Merge(totalReport, fileProcessor.PathName);
