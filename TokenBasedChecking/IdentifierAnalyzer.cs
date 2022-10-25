﻿using DTOsAndUtilities;
using Tokenizing;
using static Tokenizing.TokenType;

namespace TokenBasedChecking;

public class IdentifierAnalyzer : BaseAnalyzer
{
    private enum FileModus
    { FileModusNotSet, TopLevel, FileScoped, Traditional }

    public IdentifierAnalyzer(FileTokenData fileData, Report report) : base(fileData, report)
    {
        // is this a top level file?
        Console.WriteLine($"{ContextedFilename} is {GetFileModus()}.");
        ReportWrongIdentifierNames = true;
    }

    public void AddWarnings()
    {
        ScanVariables();
    }

    private FileModus GetFileModus()
    {
        if (!Tokens.Any(t => t.TokenType == Namespace)) return FileModus.TopLevel;
        int tokenIndex = Tokens.TakeWhile(t => t.TokenType != Namespace).Count();
        TokenType nextTokenType;
        int indexToScan = tokenIndex;
        do
        {
            indexToScan++;
            nextTokenType = Tokens[indexToScan].TokenType;
        } while (nextTokenType != BracesOpen && nextTokenType != SemiColon);
        return nextTokenType == BracesOpen ? FileModus.Traditional : FileModus.FileScoped;
    }

    // scan tokens until they end!
    // scan tokens until one of 3 possibilities:
    // 1: ; - there is a statement, check everything before it to see whether it is a variable or method declaration
    // 2: { it is a block. Assert the type, then scan everything INSIDE the block [recursively].
    //    When you return, add the closing } and check
    //    2a: is it a class/record/struct/ -method - /if/else/while/for/foreach/switch-block?
    //    Then end it here, check current statement, clear current statement, and go process next statement
    //    2b: is it a do or new block or a switch expression? Then continue to gobble up everything until the ;
    // 3: } return
    private void ScanVariables()
    {
        List<Token> currentStatement = new();
        while (CurrentIndex < Tokens.Count)
        {
            TokenType currentTokenType = CurrentTokenType();
            if (!currentTokenType.IsSkippable())
            {
                if (currentTokenType == SemiColon)
                {
                    currentStatement.Add(CurrentToken());
                    HandleStatement(currentStatement);
                }
                else if (currentTokenType == BracesOpen)
                {
                    bool isBlockStatement = IsBlockStatement(currentStatement);
                    currentStatement.Add(CurrentToken());
                    CurrentIndex++;
                    AddScope(currentStatement);
                    ScanVariables();
                    Scopes.RemoveAt(Scopes.Count - 1);
                    currentStatement.Add(CurrentToken()); // should be }
                    if (!isBlockStatement) HandleStatement(currentStatement);
                }
                else if (currentTokenType == BracesClose)
                {
                    return;
                }
                else
                {
                    currentStatement.Add(CurrentToken());
                }
            }
            Proceed();
        }
    }
}