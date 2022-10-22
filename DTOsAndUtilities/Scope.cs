namespace DTOsAndUtilities;

public enum ScopeType
{
    ScopeTypeNotSet, File, Namespace, ClassRecordStruct, Method,
    If, While, Do, For, Foreach, New, Else
}

public static class ScopeTypeExtensions
{
    public static bool IsFoundational(this ScopeType scopeType) =>
        scopeType == ScopeType.ClassRecordStruct || scopeType == ScopeType.Method;
}

public class Scope
{
    public ScopeType Type { get; set; }
    public string Name { get; set; } = null!;
}