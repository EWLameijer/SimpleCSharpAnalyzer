using CommentChecker;
using DTOsAndUtilities;
using FileHandling;

FileRepository fileRepository = new(args,
    @"Comments-analyzer.
Please enter the name of the directory which contains the code you wish to analyze: ");

FileProcessor fileProcessor = new(fileRepository);
Report totalReport = fileProcessor.Process(AnalysisMode.CommentsOnly);
CommentMerger.Merge(totalReport, fileRepository.PathName);