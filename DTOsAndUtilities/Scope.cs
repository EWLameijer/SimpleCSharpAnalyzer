namespace DTOsAndUtilities;

public enum ScopeType
{
    ScopeTypeNotSet, File, Namespace, ClassRecordStruct, Method,
    If, While, Do, For, Foreach, New, Else
}

public class Scope
{
    public ScopeType Type { get; set; }
    public string Name { get; set; } = null!;
}