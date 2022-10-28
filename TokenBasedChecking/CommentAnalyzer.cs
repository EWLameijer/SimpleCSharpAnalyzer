using DTOsAndUtilities;
using Tokenizing;

namespace TokenBasedChecking;

public class CommentAnalyzer
{
    private readonly string _contextedFilename;
    private readonly Report _report;
    private readonly IReadOnlyList<Token> _tokens;

    public CommentAnalyzer(FileAsTokens fileData, Report report)
    {
        _contextedFilename = fileData.ContextedFilename;
        _report = report;
        _tokens = fileData.Tokens;
    }

    public void AddWarnings()
    {
        for (int i = 0; i < _tokens.Count; i++)
        {
            if (_tokens[i].TokenType.IsCommentType())
            {
                string contents = ((ComplexToken)_tokens[i]).Info;
                string[] words = contents.Trim().Split();
                if (words.Length > 1)
                    _report.Warnings.Add(
                        $"Commented-out code in {_contextedFilename}: {_tokens[i].PrettyPrint()}");
            }
        }
    }
}