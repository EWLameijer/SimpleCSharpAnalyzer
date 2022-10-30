using System.Text;
using static Tokenizing.TokenType;

namespace Tokenizing;

public class Tokenizer
{
    private readonly List<string> _lines;
    private int _nextCharIndex = 0;
    private int _currentLineIndex = 0;
    private readonly int _lastLineIndex;
    private readonly List<Token> _parsedTokens = new();

    private char CurrentChar() => _lines[_currentLineIndex][_nextCharIndex];

    private char NextChar() => _lines[_currentLineIndex][_nextCharIndex + 1];

    public IReadOnlyList<Token> Results()
    {
        if (_parsedTokens.Count == 0)
        {
            while (HasNextToken())
            {
                Get();
            }
        }
        return _parsedTokens;
    }

    public Tokenizer(IReadOnlyList<string> lines)
    {
        _lines = lines.Select(line => line + "\n").ToList();
        _lastLineIndex = _lines.Count;
        BuildComplexTokensDictionary();
    }

    private Token StoreTokenWithConsume(TokenType tokenType)
    {
        Token token = new(tokenType, _currentLineIndex, _nextCharIndex);
        _parsedTokens.Add(token);
        _nextCharIndex++;
        return token;
    }

    private Token StoreTokenWithoutConsume(TokenType tokenType, int charIndex, string? contents = null)
    {
        Token token = contents == null ? new Token(tokenType, _currentLineIndex, charIndex) :
            new ComplexToken(tokenType, _currentLineIndex, charIndex, contents);
        _parsedTokens.Add(token);
        return token;
    }

    private Token StoreTokenWithoutConsume(Token token)
    {
        _parsedTokens.Add(token);
        return token;
    }

    public Token? Get()
    {
        if (!HasNextToken()) throw new EndOfStreamException();
        GoToFirstNonWhiteSpace();
        char currentChar = _lines[_currentLineIndex][_nextCharIndex];

        if (currentChar == '/') return HandlePossibleComment();
        else if (currentChar == '_' || char.IsLetter(currentChar))
            return GetIdentifierOrKeyword(currentChar);
        else if (char.IsDigit(currentChar)) return StoreTokenWithoutConsume(GetNumberToken());
        else if (_simpleTokens.ContainsKey(currentChar)) return StoreTokenWithConsume(_simpleTokens[currentChar]);
        else if (_complexTokens.ContainsKey(currentChar))
            return StoreTokenWithoutConsume(_complexTokens[currentChar]());

        Console.WriteLine($"Parse stopped at line {_lines[_currentLineIndex]}");
        throw new ArgumentException("Tokenizer error: weird token!");
    }

    private Token HandlePossibleComment()
    {
        int startCharIndex = _nextCharIndex;
        _nextCharIndex++;
        char nextChar = CurrentChar();
        if (nextChar == '/') return HandleLineOrDocComment();
        else if (nextChar == '*') return StoreTokenWithoutConsume(GetBlockComment());
        else return StoreTokenWithoutConsume(Division, startCharIndex);
    }

    private Token HandleLineOrDocComment()
    {
        int startingIndex = _nextCharIndex - 1;
        TokenType commentType = LineComment;
        _nextCharIndex++;
        if (_nextCharIndex < _lines[_currentLineIndex].Length && CurrentChar() == '/')
        {
            _nextCharIndex++;
            commentType = DocComment;
        }
        string contents = _lines[_currentLineIndex][_nextCharIndex..].Trim();
        _nextCharIndex = _lines[_currentLineIndex].Length - 1; // don't skip newline!
        return StoreTokenWithoutConsume(commentType, startingIndex, contents);
    }

    private Token GetIdentifierOrKeyword(char currentChar)
    {
        (string identifier, int startingPos) = GetIdentifier(currentChar);
        if (Keywords._dict.ContainsKey(identifier))
        {
            Token target = new(Keywords._dict[identifier], _currentLineIndex, startingPos);
            _parsedTokens.Add(target);
            return target;
        }
        else
        {
            Token token = new ComplexToken(Identifier, _currentLineIndex, startingPos, identifier);
            _parsedTokens.Add(token);
            return token;
        }
    }

    private (string identifier, int startingPos) GetIdentifier(char currentChar)
    {
        StringBuilder result = new();
        int startingPos = _nextCharIndex;
        result.Append(currentChar);
        do
        {
            _nextCharIndex++;
            char ch = _lines[_currentLineIndex][_nextCharIndex];
            if (char.IsLetterOrDigit(ch) || ch == '_') result.Append(ch); else break;
        } while (true);

        return (result.ToString(), startingPos);
    }

    private void GoToFirstNonWhiteSpace()
    {
        while (_currentLineIndex != _lastLineIndex)
        {
            if (_nextCharIndex == _lines[_currentLineIndex].Length) GoToNextLine();
            else
            {
                char ch = _lines[_currentLineIndex][_nextCharIndex];
                if (!char.IsWhiteSpace(ch)) return;
                if (ch == '\n')
                    _parsedTokens.Add(new Token(Newline, _currentLineIndex, _nextCharIndex));
                _nextCharIndex++;
            }
        }
    }

    private void GoToNextLine()
    {
        _currentLineIndex++;
        _nextCharIndex = 0;
    }

    private void BuildComplexTokensDictionary()
    {
        _complexTokens['"'] = GetStringToken;
        _complexTokens['#'] = GetPragmaToken;
        _complexTokens['\''] = GetSingleQuotedStringToken;
        _complexTokens['<'] = GetLessTypeToken;
        _complexTokens['>'] = GetGreaterTypeToken;
        _complexTokens['+'] = GetPlusToken;
        _complexTokens['-'] = GetMinusToken;
        _complexTokens['&'] = GetLogicAndToken;
        _complexTokens['|'] = GetLogicOrToken;
        _complexTokens['$'] = GetDollarToken;
        _complexTokens['@'] = GetAtToken;
        _complexTokens['='] = GetAssignToken;
    }

    private readonly Dictionary<char, Func<Token>> _complexTokens = new();

    private readonly Dictionary<char, TokenType> _simpleTokens = new()
    {
        [','] = Comma,
        ['.'] = Period,
        ['?'] = QuestionMark,
        ['!'] = ExclamationMark,
        [';'] = Semicolon,
        [':'] = Colon,
        ['^'] = Caret,
        ['*'] = Times,
        ['%'] = Modulus,
        ['('] = ParenthesesOpen,
        [')'] = ParenthesesClose,
        ['['] = BracketsOpen,
        [']'] = BracketsClose,
        ['{'] = BracesOpen,
        ['}'] = BracesClose,
    };

    private Token GetPragmaToken()
    {
        StringBuilder pragma = new();
        int startPosition = _nextCharIndex;
        _nextCharIndex++;
        char ch = CurrentChar();
        while (ch != '\n')
        {
            pragma.Append(ch);
            _nextCharIndex++;
            ch = CurrentChar();
        }
        _nextCharIndex--; // reset so newline will be read
        return new ComplexToken(Pragma, _currentLineIndex, startPosition, pragma.ToString());
    }

    private Token GetBlockComment()
    {
        // starts at *
        StringBuilder result = new();
        Token? oneLineBlockComment = GetBlockCommentLine(_nextCharIndex - 1, result, BlockCommentWhole,
            BlockCommentStart);
        if (oneLineBlockComment != null) return oneLineBlockComment;
        do
        {
            result.Clear();
            Token? furtherLineBlockComment = GetBlockCommentLine(0, result, BlockCommentEnd,
                BlockCommentMiddle);
            if (furtherLineBlockComment != null) return furtherLineBlockComment;
        } while (true);
    }

    private Token? GetBlockCommentLine(int startIndex, StringBuilder result, TokenType finishType,
        TokenType nonFinishType)
    {
        do
        {
            _nextCharIndex++;
            char ch = CurrentChar();
            (Token? finalToken, bool startNewLine) = ProcessBlockCommentChar(ch, startIndex, result,
                finishType, nonFinishType);
            if (finalToken != null) return finalToken;
            if (startNewLine) break;
        } while (true);
        return null;
    }

    private (Token? finalToken, bool startNewLine) ProcessBlockCommentChar(char ch,
        int startIndex, StringBuilder result, TokenType finishType, TokenType nonFinishType)
    {
        if (ch == '*' && NextChar() == '/')
            return HandleBlockCommentEnd(startIndex, result, finishType);
        if (ch == '\n')
        {
            StoreNextBlockCommentLine(result, startIndex, nonFinishType);
            return (null, true);
        }
        result.Append(ch);
        return (null, false);
    }

    private (Token finalToken, bool startNewline) HandleBlockCommentEnd(
        int startIndex, StringBuilder result, TokenType finishType)
    {
        _nextCharIndex += 2;
        Token finalToken = new ComplexToken(finishType, _currentLineIndex, startIndex,
            result.ToString());
        return (finalToken, false);
    }

    private void StoreNextBlockCommentLine(StringBuilder result, int startCharIndex,
        TokenType nonFinishType)
    {
        _parsedTokens.Add(new ComplexToken(nonFinishType, _currentLineIndex, startCharIndex,
            result.ToString()));
        _parsedTokens.Add(new Token(Newline, _currentLineIndex,
            _lines[_currentLineIndex].Length - 1));
        _currentLineIndex++;
        _nextCharIndex = -1; // will soon need to start at 0
    }

    private Token GetDollarToken()
    {
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '"')
        {
            Token token = GetInterpolatedStringToken();
            return token;
        }
        if (ch == '@' && NextChar() == '"')
        {
            _nextCharIndex++;
            return GetInterpolatedVerbatimStringToken();
        }
        throw new ArgumentException("$ parsing error");
    }

    private Token GetAtToken()
    {
        int startCharIndex = _nextCharIndex;
        _nextCharIndex++;
        char ch = CurrentChar();
        Token? possibleVerbatimStringToken = GetPossibleVerbatimStringToken(ch);
        if (possibleVerbatimStringToken != null) return possibleVerbatimStringToken;
        if (char.IsLetter(ch)) // @lock and such, for people who want a reserved word as identifier.
        {
            string atIdentifier = ExtractAtStartingIdentifier(ref ch);
            return new ComplexToken(Identifier, _currentLineIndex, startCharIndex, atIdentifier);
        }
        throw new ArgumentException("@ parsing error");
    }

    private Token? GetPossibleVerbatimStringToken(char ch)
    {
        if (ch == '"')
        {
            return GetVerbatimStringToken();
        }
        if (ch == '$' && NextChar() == '"')
        {
            _nextCharIndex++;
            return GetInterpolatedVerbatimStringToken();
        }
        return null;
    }

    private string ExtractAtStartingIdentifier(ref char ch)
    {
        StringBuilder atIdentifier = new();
        atIdentifier.Append('@');
        do
        {
            atIdentifier.Append(ch);
            _nextCharIndex++;
            ch = CurrentChar();
        } while (char.IsLower(ch));
        return atIdentifier.ToString();
    }

    private Token GetVerbatimStringToken()
    {
        StringBuilder result = new();
        Token? finishedToken = GetVerbatimStringLine(_nextCharIndex, result, VerbatimStringWhole,
            VerbatimStringStart);
        if (finishedToken != null) return finishedToken;

        do
        {
            result.Clear();
            finishedToken = GetVerbatimStringLine(0, result, VerbatimStringEnd,
            VerbatimStringMiddle);
            if (finishedToken != null) return finishedToken;
        } while (true);
    }

    private Token? GetVerbatimStringLine(int startCharIndex, StringBuilder result, TokenType finalTokenType,
        TokenType nonFinalTokenType)
    {
        do
        {
            _nextCharIndex++;
            (Token? finalToken, bool isBreak) = HandleVerbatimChar(startCharIndex, result, finalTokenType,
                nonFinalTokenType);
            if (finalToken != null) return finalToken;
            if (isBreak) break;
        } while (true);
        return null;
    }

    private void HandleVerbatimLineEnd(int startCharIndex, StringBuilder result, TokenType nonFinalTokenType)
    {
        _parsedTokens.Add(new ComplexToken(nonFinalTokenType, _currentLineIndex, startCharIndex,
            result.ToString()));
        _parsedTokens.Add(new Token(Newline, _currentLineIndex, _lines[_currentLineIndex].Length - 1));
        _currentLineIndex++;
        _nextCharIndex = -1; // will soon need to start at 0
    }

    private (Token? finalToken, bool isBreak) HandleVerbatimChar(int startCharIndex,
        StringBuilder result,
        TokenType finalTokenType, TokenType nonFinalTokenType)
    {
        char ch = CurrentChar();
        if (ch == '"')
        {
            Token? finalToken = HandleDoubleQuotes(startCharIndex, result, finalTokenType);
            if (finalToken != null) return (finalToken, false);
        }
        else if (ch == '\n')
        {
            HandleVerbatimLineEnd(startCharIndex, result, nonFinalTokenType);
            return (null, true);
        }
        result.Append(ch);
        return (null, false);
    }

    private Token? HandleDoubleQuotes(int startCharIndex, StringBuilder result, TokenType finalTokenType)
    {
        _nextCharIndex++;
        return CurrentChar() != '"' ? new ComplexToken(finalTokenType, _currentLineIndex,
            startCharIndex, result.ToString()) : null;
    }

    // note: is a copy of verbatim processing.
    // if necessary, improve it so it will properly process interpolated verbatim strings
    // for now, may work well enough, though...
    private Token GetInterpolatedVerbatimStringToken()
    {
        return GetVerbatimStringToken();
    }

    private Token GetPlusToken()
    {
        int startCharIndex = _nextCharIndex;
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '+') return TokenAt(Increment, startCharIndex);
        return new Token(Plus, _currentLineIndex, startCharIndex);
    }

    private Token GetMinusToken()
    {
        int startCharIndex = _nextCharIndex;
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '-') return TokenAt(Decrement, startCharIndex);
        return new Token(Minus, _currentLineIndex, startCharIndex);
    }

    private Token GetLogicAndToken()
    {
        int startCharIndex = _nextCharIndex;
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '&') return TokenAt(LogicAnd, startCharIndex);
        throw new ArgumentException("& parse wrong!");
    }

    private Token GetLogicOrToken()
    {
        int startCharIndex = _nextCharIndex;
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '|') return TokenAt(LogicOr, startCharIndex);
        throw new ArgumentException("| parse wrong!");
    }

    private Token GetLessTypeToken()
    {
        int startCharIndex = _nextCharIndex;
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '=') return TokenAt(Comparator, startCharIndex);
        return new Token(Less, _currentLineIndex, startCharIndex);
    }

    private Token GetAssignToken()
    {
        int startCharIndex = _nextCharIndex;
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '>') return TokenAt(FatArrow, startCharIndex);
        else if (ch == '=') return TokenAt(Comparator, startCharIndex);
        return new Token(Assign, _currentLineIndex, startCharIndex);
    }

    private Token TokenAt(TokenType tokenType, int startCharIndex)
    {
        _nextCharIndex++;
        return new Token(tokenType, _currentLineIndex, startCharIndex);
    }

    private Token GetGreaterTypeToken()
    {
        int startCharIndex = _nextCharIndex;
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '=') return TokenAt(Comparator, startCharIndex);
        return new Token(Greater, _currentLineIndex, startCharIndex);
    }

    private Token GetNumberToken()
    {
        StringBuilder result = new();
        int startCharIndex = _nextCharIndex;
        result.Append(CurrentChar());
        do
        {
            _nextCharIndex++;
            char ch = CurrentChar();
            if (!char.IsDigit(ch) && ch != '_' && ch != '.') break;
            result.Append(ch);
        } while (true);
        return new ComplexToken(Number, _currentLineIndex, startCharIndex, result.ToString());
    }

    private Token GetStringToken()
    {
        bool isEscapeMode = false;
        int initCharIndex = _nextCharIndex + 1;
        do
        {
            _nextCharIndex++;
            char ch = _lines[_currentLineIndex][_nextCharIndex];
            if (ch == '\\') isEscapeMode = !isEscapeMode;
            else if (ch == '"' && !isEscapeMode) break;
            else isEscapeMode = false;
        } while (true);
        _nextCharIndex++;
        string stringContents = _lines[_currentLineIndex].Substring(initCharIndex, _nextCharIndex - initCharIndex - 1);
        return new ComplexToken(TokenType.String, _currentLineIndex, initCharIndex - 1, stringContents);
    }

    private Token GetInterpolatedStringToken()
    {
        StringBuilder result = new();
        bool isEscapeMode = false;
        bool shouldContinue;
        TokenType tokenType = InterpolatedStringStart;
        int startCharIndex = _nextCharIndex;
        do
        {
            (shouldContinue, isEscapeMode, tokenType, startCharIndex) =
                ParseInterpolatedStringChar(startCharIndex, result, isEscapeMode, tokenType);
        } while (shouldContinue);
        _nextCharIndex++;
        return new ComplexToken(InterpolatedStringEnd, _currentLineIndex, startCharIndex, result.ToString());
    }

    private (bool shouldContinue, bool escapeMode, TokenType tokenType, int startCharIndex)
        ParseInterpolatedStringChar(int startCharIndex, StringBuilder result, bool isEscapeMode, TokenType tokenType)
    {
        _nextCharIndex++;
        char ch = _lines[_currentLineIndex][_nextCharIndex];
        if (ch == '\\') isEscapeMode = !isEscapeMode;
        else if (ch == '"' && !isEscapeMode) return (false, isEscapeMode, tokenType, startCharIndex);
        else if (ch == '{' && !isEscapeMode) (tokenType, startCharIndex) =
                HandleInterpolatedBraceOpen(startCharIndex, result, tokenType);
        else
        {
            isEscapeMode = false;
            result.Append(ch);
        }
        return (true, isEscapeMode, tokenType, startCharIndex);
    }

    private (TokenType tokenType, int startCharIndex)
        HandleInterpolatedBraceOpen(int startCharIndex, StringBuilder result, TokenType tokenType)
    {
        char ch = CurrentChar();
        if (_lines[_currentLineIndex][_nextCharIndex + 1] == '{')
        {
            _nextCharIndex += 2;
            result.Append(ch);
            return (tokenType, startCharIndex);
        }
        return ParseEmbeddedCode(startCharIndex, result, tokenType);
    }

    private (TokenType tokenType, int startCharIndex) ParseEmbeddedCode(int startCharIndex,
        StringBuilder result, TokenType tokenType)
    {
        _parsedTokens.Add(new ComplexToken(tokenType, _currentLineIndex, startCharIndex,
            result.ToString()));
        result.Clear();
        tokenType = InterpolatedStringMiddle;
        _nextCharIndex++;
        Token nextToken;
        do
        {
            nextToken = Get()!;
        } while (nextToken.TokenType != BracesClose);
        _parsedTokens.RemoveAt(_parsedTokens.Count - 1); // get rid of }
        _nextCharIndex--; // so won't skip " or such
        return (tokenType, _nextCharIndex);
    }

    private Token GetSingleQuotedStringToken()
    {
        bool isEscapeMode = false;
        int initCharIndex = _nextCharIndex + 1;
        do
        {
            _nextCharIndex++;
            char ch = _lines[_currentLineIndex][_nextCharIndex];
            if (ch == '\\') isEscapeMode = !isEscapeMode;
            else if (ch == '\'' && !isEscapeMode) break;
            else isEscapeMode = false;
        } while (true);
        _nextCharIndex++;
        string stringContents = _lines[_currentLineIndex].Substring(initCharIndex, _nextCharIndex - initCharIndex - 1);
        return new ComplexToken(SingleQuotedString, _currentLineIndex, initCharIndex - 1, stringContents);
    }

    private sealed class TextPosition
    {
        public int LineIndex { get; set; }
        public int CharIndex { get; set; }
    }

    public bool HasNextToken()
    {
        TextPosition pos = new() { LineIndex = _currentLineIndex, CharIndex = _nextCharIndex };
        while (pos.LineIndex != _lastLineIndex)
        {
            bool tokenFound = NextTokenFound(pos);
            if (tokenFound) return true;
        }
        return false;
    }

    private bool NextTokenFound(TextPosition pos)
    {
        if (pos.CharIndex == _lines[pos.LineIndex].Length)
        {
            pos.LineIndex++;
            pos.CharIndex = 0;
        }
        else
        {
            if (!char.IsWhiteSpace(_lines[pos.LineIndex][pos.CharIndex])) return true;
            pos.CharIndex++;
        }
        return false;
    }
}