namespace Tokenizing;

public enum TokenType
{
    TokenTypeNotSet, Abstract, As, Assign, Async, Await, Base, BlockCommentEnd, BlockCommentMiddle,
    BlockCommentStart,
    BlockCommentWhole, BracesClose, BracesOpen, BracketsOpen, BracketsClose, Break, Caret, Case,
    Catch, Checked,
    Class,
    Colon,
    Comma,
    Comparator, Const, Continue, DecimalLiteral, Decrement, Delegate, Division, Do, Else, Enum,
    Event,
    ExclamationMark, Explicit, Extern, False,
    FatArrow, Finally, Fixed,
    For, ForEach, Get, Global, Goto, Greater,
    Identifier, If, Implicit, In,
    Increment, Init, Interface, Internal, InterpolatedStringEnd, InterpolatedStringMiddle,
    InterpolatedStringStart, InterpolatedStringWhole, Is, Less,
    LineComment, Lock, LogicAnd, LogicOr, Minus, Modulus,
    Namespace, New, NewLine, Null,
    Number, Operator, Out, Override, Params, ParenthesesClose, ParenthesesOpen, Partial, Plus,
    Period, Pragma, Private, Protected, Public, QuestionMark, Readonly, Record, Ref, Return, Sealed,
    SemiColon, Set,
    SingleQuotedString, Stackalloc, Static,
    String, Struct, Switch, This,
    Throw, Times, True, Try, Unchecked, Unsafe, Using, VerbatimStringEnd, VerbatimStringMiddle,
    VerbatimStringStart,
    VerbatimStringWhole, Virtual, Volatile, Where, While, With, Yield
}