namespace ResultComparer;

internal abstract class AnalyzerWarning
{
    protected readonly string Filename;

    protected AnalyzerWarning(string filename)
    {
        string[] parts = filename.Split("\\");

        Filename = string.Join("\\", parts.TakeLast(2));
    }

    public abstract string WarningType { get; }
}

internal class MethodTooLongWarning : AnalyzerWarning
{
    private const int MethodNameIndex = 3;
    private const int FileNameIndex = 5;
    private const int LengthIndex = 7;

    private readonly string _methodName;
    private readonly string _length;

    public override string WarningType => "TOO_LONG_METHOD";

    public MethodTooLongWarning(string methodName, string filename, string length) : base(filename[..^1])
    {
        _methodName = methodName;
        _length = length;
    }

    public static MethodTooLongWarning Parse(string[] lineParts) =>
        new(lineParts[MethodNameIndex], lineParts[FileNameIndex],
            lineParts[LengthIndex]);

    public override string ToString() => $"{WarningType}: {_methodName} in {Filename}";

    public override bool Equals(object? obj)
    {
        if (obj is not MethodTooLongWarning) return false;
        MethodTooLongWarning other = (MethodTooLongWarning)obj;
        return _methodName == other._methodName && _length == other._length &&
            Filename == other.Filename;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

internal class LineTooLongWarning : AnalyzerWarning
{
    private const int FileNameIndex = 4;
    private const int LineIndexIndex = 7;

    private readonly string _lineIndex;

    public override string WarningType => "TOO_LONG_LINE";

    public LineTooLongWarning(string filename, string lineLength) : base(filename)
    {
        _lineIndex = lineLength;
    }

    public static LineTooLongWarning Parse(string[] lineParts) =>
      new(lineParts[FileNameIndex], lineParts[LineIndexIndex]);

    public override string ToString() => $"{WarningType}: line {_lineIndex} in {Filename}";

    public override bool Equals(object? obj)
    {
        if (obj is not LineTooLongWarning) return false;
        LineTooLongWarning other = (LineTooLongWarning)obj;
        return _lineIndex == other._lineIndex && Filename == other.Filename;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

internal class InvalidVariableNameWarning : AnalyzerWarning
{
    private const int VariableNameIndex = 3;
    private const int FileNameIndex = 5;
    private const int ContextIndex = 7;

    private readonly string _variableName;
    private readonly string _context;

    public override string WarningType => "INVALID_VARIABLE_NAME";

    private InvalidVariableNameWarning(string variableName, string filename, string context) : base(filename)
    {
        _variableName = variableName;
        _context = context;
    }

    public static InvalidVariableNameWarning Parse(string[] lineParts) =>
        new(lineParts[VariableNameIndex],
            lineParts[FileNameIndex], lineParts[ContextIndex]);

    public override string ToString() => $"{WarningType}: {_variableName} in {Filename}";

    public override bool Equals(object? obj)
    {
        if (obj is not InvalidVariableNameWarning) return false;
        InvalidVariableNameWarning other = (InvalidVariableNameWarning)obj;
        return _variableName == other._variableName && _context == other._context &&
            Filename == other.Filename;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

internal class InvalidParameterNameWarning : AnalyzerWarning
{
    public override string WarningType => "INVALID_PARAMETER_NAME";

    private const int ParameterNameIndex = 3;
    private const int FileNameIndex = 5;

    private readonly string _parameterName;

    private InvalidParameterNameWarning(string parameterName, string filename) : base(filename[..^2])
    {
        _parameterName = parameterName;
    }

    public override string ToString() => $"{WarningType}: {_parameterName} in {Filename}";

    public static InvalidParameterNameWarning Parse(string[] lineParts) =>
        new(lineParts[ParameterNameIndex], lineParts[FileNameIndex]);

    public override bool Equals(object? obj)
    {
        if (obj is not InvalidParameterNameWarning) return false;
        InvalidParameterNameWarning other = (InvalidParameterNameWarning)obj;
        return _parameterName == other._parameterName && Filename == other.Filename;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

internal class InvalidMethodNameWarning : AnalyzerWarning
{
    public override string WarningType => "INVALID_METHOD_NAME";

    private const int MethodNameIndex = 3;
    private const int FileNameIndex = 5;

    private readonly string _methodName;

    private InvalidMethodNameWarning(string parameterName, string filename) : base(filename[..^2])
    {
        _methodName = parameterName;
    }

    public override string ToString() => $"{WarningType}: {_methodName} in {Filename}";

    public static InvalidMethodNameWarning Parse(string[] lineParts) =>
        new(lineParts[MethodNameIndex], lineParts[FileNameIndex]);

    public override bool Equals(object? obj)
    {
        if (obj is not InvalidMethodNameWarning) return false;
        InvalidMethodNameWarning other = (InvalidMethodNameWarning)obj;
        return _methodName == other._methodName && Filename == other.Filename;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}