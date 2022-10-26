using ResultComparer;

namespace ResultsComparer;
internal class WarningData
{
    private readonly int _id;

    private readonly AnalyzerWarning _warning;

    // use Parse instead of typeof(AnalyzerWarning) - not using reflection is a lot simpler
    private static readonly Dictionary<string, Func<string, AnalyzerWarning>> _parsingType = new()
    {
        ["Invalid variable name"] = InvalidVariableNameWarning.Parse,
        ["Too long method"] = MethodTooLongWarning.Parse
    };

    public WarningData(string line)
    {
        string idAsString = string.Join("", line.TakeWhile(c => char.IsDigit(c)));
        string startOfError = string.Concat(line.Skip(idAsString.Length + 2));
        _id = int.Parse(idAsString);
        Console.WriteLine($"Id is {_id}, line is {startOfError}");
        _warning = Parse(startOfError);
    }

    private AnalyzerWarning Parse(string line)
    {
        string selectedKey = _parsingType.Keys.First(startString => line.StartsWith(startString));
        return _parsingType[selectedKey](line);
    }
}

public override string ToString()
{
    return $"
    }
}