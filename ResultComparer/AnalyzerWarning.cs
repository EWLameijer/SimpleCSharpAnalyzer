namespace ResultComparer;

internal abstract class AnalyzerWarning
{
    public abstract string WarningType { get; }
}

internal class MethodTooLongWarning : AnalyzerWarning
{
    private const int MethodNameIndex = 3;
    private const int FileNameIndex = 5;
    private const int LengthIndex = 7;

    private readonly string _methodName;
    private readonly string _filename;
    private readonly string _length;

    public override string WarningType => "TOO_LONG_METHOD";

    public MethodTooLongWarning(string methodName, string filename, string length)
    {
        _methodName = methodName;
        _filename = filename[..^1];
        _length = length;
    }

    public static MethodTooLongWarning Parse(string[] lineParts) =>
        new(lineParts[MethodNameIndex], lineParts[FileNameIndex],
            lineParts[LengthIndex]);

    public override string ToString() => $"{WarningType}: {_methodName} in {_filename}";
}

internal class LineTooLongWarning : AnalyzerWarning
{
    private const int FileNameIndex = 4;
    private const int LineIndexIndex = 7;

    private readonly string _filename;
    private readonly string _lineIndex;

    public override string WarningType => "TOO_LONG_LINE";

    public LineTooLongWarning(string filename, string lineLength)
    {
        _filename = filename;
        _lineIndex = lineLength;
    }

    public static LineTooLongWarning Parse(string[] lineParts) =>
      new(lineParts[FileNameIndex], lineParts[LineIndexIndex]);

    public override string ToString() => $"{WarningType}: line {_lineIndex} in {_filename}";
}

internal class InvalidVariableNameWarning : AnalyzerWarning
{
    private const int VariableNameIndex = 3;
    private const int FileNameIndex = 5;
    private const int ContextIndex = 7;

    private readonly string _variableName;
    private readonly string _filename;
    private readonly string _context;

    public override string WarningType => "INVALID_VARIABLE_NAME";

    private InvalidVariableNameWarning(string variableName, string filename, string length)
    {
        _variableName = variableName;
        _filename = filename;
        _context = length;
    }

    public static InvalidVariableNameWarning Parse(string[] lineParts) =>
        new(lineParts[VariableNameIndex],
            lineParts[FileNameIndex], lineParts[ContextIndex]);

    public override string ToString() => $"{WarningType}: {_variableName} in {_filename}";
}

internal class InvalidParameterNameWarning : AnalyzerWarning
{
    public override string WarningType => "INVALID_PARAMETER_NAME";

    private const int ParameterNameIndex = 3;
    private const int FileNameIndex = 5;

    private readonly string _parameterName;
    private readonly string _filename;

    private InvalidParameterNameWarning(string parameterName, string filename)
    {
        _parameterName = parameterName;
        _filename = filename[..^2];
    }

    public override string ToString() => $"{WarningType}: {_parameterName} in {_filename}";

    public static InvalidParameterNameWarning Parse(string[] lineParts) =>
        new(lineParts[ParameterNameIndex], lineParts[FileNameIndex]);
}