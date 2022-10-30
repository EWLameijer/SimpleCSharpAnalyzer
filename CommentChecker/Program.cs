using CommentChecker;
using DTOsAndUtilities;
using FileHandling;

FileProcessor fileProcessor = new(args,
    @"Comments-analyzer.
Please enter the name of the directory which contains the code you wish to analyze: ");

Report totalReport = fileProcessor.Process(AnalysisMode.CommentsOnly);
CommentMerger.Merge(totalReport, fileProcessor.PathName);