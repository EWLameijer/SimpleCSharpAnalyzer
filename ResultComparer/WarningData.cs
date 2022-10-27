using ResultComparer;

namespace ResultsComparer;

internal class WarningData
{
    private readonly List<int> _ids = new();

    public int IdsCount => _ids.Count;

    private readonly AnalyzerWarning _warning;

    public string WarningType => _warning.WarningType;

    public string WarningText => _warning.ToString()!;

    // use Parse instead of typeof(AnalyzerWarning) - not using reflection is a lot simpler
    private static readonly Dictionary<string, Func<string[], AnalyzerWarning>> _parsingType = new()
    {
        ["Invalid variable name"] = InvalidVariableNameWarning.Parse,
        ["Too long method"] = MethodTooLongWarning.Parse,
        ["Too long line in"] = LineTooLongWarning.Parse,
        ["Invalid parameter name"] = InvalidParameterNameWarning.Parse,
        ["Invalid method name"] = InvalidMethodNameWarning.Parse
    };

    public WarningData(string line)
    {
        string idAsString = string.Join("", line.TakeWhile(c => char.IsDigit(c)));
        string startOfError = string.Concat(line.Skip(idAsString.Length + 2));
        _ids.Add(int.Parse(idAsString));
        // Console.WriteLine($"Id is {_id}, line is {startOfError}");
        _warning = Parse(startOfError);
    }

    private static AnalyzerWarning Parse(string line)
    {
        string selectedKey = _parsingType.Keys.First(startString => line.StartsWith(startString));
        return _parsingType[selectedKey](line.Split());
    }

    public void Merge(WarningData other)
    {
        _ids.AddRange(other._ids);
    }

    public override string ToString()
    {
        string[] idsAsStrings = _ids.Select(i => $"{i}").ToArray();
        return $"{_warning} [{string.Join(", ", idsAsStrings)}]";
    }
}