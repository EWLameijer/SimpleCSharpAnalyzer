namespace DTOsAndUtilities;

public enum ScopeType
{
    ScopeTypeNotSet, File, Namespace, ClassRecordStruct, Method,
    If, While, Do, For, Foreach, New, Else
}

public record Scope(ScopeType Type, string Name);