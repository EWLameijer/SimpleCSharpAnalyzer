namespace ResultComparer;

internal abstract class AnalyzerWarning
{
}

internal class MethodTooLongWarning : AnalyzerWarning
{
    private const int MethodNameIndex = 3;
    private const int FileNameIndex = 5;
    private const int LengthIndex = 7;

    private readonly string _methodName;
    private readonly string _filename;
    private readonly string _length;

    public MethodTooLongWarning(string methodName, string filename, string length)
    {
        _methodName = methodName;
        _filename = filename;
        _length = length;
    }

    public static MethodTooLongWarning Parse(string line)
    {
        string[] lineParts = line.Split(' ');
        return new MethodTooLongWarning(lineParts[MethodNameIndex], lineParts[FileNameIndex],
            lineParts[LengthIndex]);
    }
}

internal class InvalidVariableNameWarning : AnalyzerWarning
{
    private const int VariableNameIndex = 3;
    private const int FileNameIndex = 5;
    private const int ContextIndex = 7;

    private readonly string _variableName;
    private readonly string _filename;
    private readonly string _context;

    private InvalidVariableNameWarning(string methodName, string filename, string length)
    {
        _variableName = methodName;
        _filename = filename;
        _context = length;
    }

    public static InvalidVariableNameWarning Parse(string line)
    {
        string[] lineParts = line.Split(' ');
        return new InvalidVariableNameWarning(lineParts[VariableNameIndex],
            lineParts[FileNameIndex], lineParts[ContextIndex]);
    }
}