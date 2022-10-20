namespace Tokenizing;

public enum TokenType
{
    TokenTypeNotSet, Assign, BlockCommentEnd, BlockCommentMiddle, BlockCommentStart,
    BlockCommentWhole, BracesClose, BracesOpen, BracketsOpen, BracketsClose, Break, Caret, Case,
    Catch,
    Class,
    Colon,
    Comma,
    Comparator, Const, DecimalLiteral, Decrement, Division, Do, Else, Enum, ExclamationMark, False,
    FatArrow,
    For, ForEach, Get, Global, Greater,
    Identifier, If,
    Increment, Init, Interface, Internal, InterpolatedStringEnd, InterpolatedStringMiddle,
    InterPolatedStringStart, InterpolatedStringWhole, Less,
    LineComment, LogicAnd, LogicOr, Minus, Modulus,
    Namespace, New, NewLine, Null,
    Number, Out, Override, ParenthesesClose, ParenthesesOpen, Plus,
    Period, Private, Protected, Public, QuestionMark, Readonly, Record, Return, SemiColon, Set,
    SingleQuotedString, Static,
    String, Struct, Switch,
    Throw, Times, True, Try, Using, VerbatimStringEnd, VerbatimStringMiddle, VerbatimStringStart,
    VerbatimStringWhole, Virtual, Where, While
}