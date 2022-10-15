namespace Tokenizing;

public enum TokenType
{
    TokenTypeNotSet, Assign, BlockCommentEnd, BlockCommentMiddle, BlockCommentStart,
    BlockCommentWhole, BracesClose, BracesOpen, BracketsOpen, BracketsClose, Break, Caret, Catch,
    Class,
    Colon,
    Comma,
    Comparator, Decrement, Division, Do, Else, Enum, ExclamationMark, False, FatArrow,
    For, Get, Global, Greater,
    Identifier, If,
    Increment, Init, Interface, Internal, InterpolatedStringEnd, InterpolatedStringMiddle,
    InterPolatedStringStart, InterpolatedStringWhole, Less,
    LineComment, LogicAnd, LogicOr, Minus,
    Namespace, New, NewLine, Null,
    Number, Out, Override, ParenthesesClose, ParenthesesOpen, Plus,
    Period, Private, Public, QuestionMark, Readonly, Return, SemiColon, Set, SingleQuotedString,
    String, Throw, Times, True, Try, Using, VerbatimStringEnd, VerbatimStringMiddle, VerbatimStringStart,
    VerbatimStringWhole, While
}