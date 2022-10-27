using ResultComparer;

namespace ResultsComparer;

internal class WarningData
{
    private readonly int _id;

    private readonly AnalyzerWarning _warning;

    public string WarningType => _warning.WarningType;

    // use Parse instead of typeof(AnalyzerWarning) - not using reflection is a lot simpler
    private static readonly Dictionary<string, Func<string[], AnalyzerWarning>> _parsingType = new()
    {
        ["Invalid variable name"] = InvalidVariableNameWarning.Parse,
        ["Too long method"] = MethodTooLongWarning.Parse,
        ["Too long line in"] = LineTooLongWarning.Parse,
        ["Invalid parameter name"] = InvalidParameterNameWarning.Parse
    };

    public WarningData(string line)
    {
        string idAsString = string.Join("", line.TakeWhile(c => char.IsDigit(c)));
        string startOfError = string.Concat(line.Skip(idAsString.Length + 2));
        _id = int.Parse(idAsString);
        // Console.WriteLine($"Id is {_id}, line is {startOfError}");
        _warning = Parse(startOfError);
    }

    private static AnalyzerWarning Parse(string line)
    {
        string selectedKey = _parsingType.Keys.First(startString => line.StartsWith(startString));
        return _parsingType[selectedKey](line.Split());
    }

    public override string ToString()
    {
        return $"{_warning} [{_id}]";
    }
}