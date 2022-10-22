﻿using System.Text;
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
        return _parsedTokens;
    }

    public Tokenizer(IReadOnlyList<string> lines)
    {
        _lines = lines.Select(line => line + "\n").ToList();
        _lastLineIndex = _lines.Count;
    }

    private Token StoreTokenWithConsume(Token token)
    {
        _nextCharIndex++;
        _parsedTokens.Add(token);
        return token;
    }

    private Token StoreTokenWithoutConsume(TokenType tokenType, string? contents = null)
    {
        Token token = contents == null ? new Token { TokenType = tokenType } :
            new ComplexToken { TokenType = tokenType, Info = contents };
        _parsedTokens.Add(token);
        return token;
    }

    private Token StoreTokenWithoutConsume(Token token)
    {
        _parsedTokens.Add(token);
        return token;
    }

    /* Get
     * Check whether there is a next token (HasNextToken). If not, throw an exception
     * If there IS, go to the first non-whitespace character (adding newlines to the ParseLog)
     * if it is '/', check the next character.
     * If that is '/' too,
    */

    public Token? Get()
    {
        if (!HasNextToken()) throw new EndOfStreamException();
        while (_currentLineIndex != _lastLineIndex)
        {
            if (_nextCharIndex == _lines[_currentLineIndex].Length)
            {
                _currentLineIndex++;
                _nextCharIndex = 0;
            }
            else
            {
                char ch = _lines[_currentLineIndex][_nextCharIndex];
                if (!char.IsWhiteSpace(ch)) break;
                if (ch == '\n')
                    _parsedTokens.Add(new Token { TokenType = NewLine });
                _nextCharIndex++;
            }
        }
        char currentChar = _lines[_currentLineIndex][_nextCharIndex];

        if (currentChar == '/')
        {
            _nextCharIndex++;
            char nextChar = _lines[_currentLineIndex][_nextCharIndex];
            if (nextChar == '/')
            {
                _nextCharIndex++;
                string contents = _lines[_currentLineIndex][_nextCharIndex..].Trim();
                _nextCharIndex = _lines[_currentLineIndex].Length - 1; // don't skip newline!
                return StoreTokenWithoutConsume(LineComment, contents);
            }
            else if (nextChar == '*')
                return StoreTokenWithoutConsume(GetBlockComment());
            else return StoreTokenWithoutConsume(Division);
        }
        else if (currentChar == '_' || char.IsLetter(currentChar))
        {
            StringBuilder result = new();
            result.Append(currentChar);
            do
            {
                _nextCharIndex++;
                char ch = _lines[_currentLineIndex][_nextCharIndex];
                if (char.IsLetterOrDigit(ch) || ch == '_') result.Append(ch); else break;
            } while (true);

            string identifier = result.ToString();
            if (Keywords.dict.ContainsKey(identifier))
            {
                Token target = new() { TokenType = Keywords.dict[identifier] };
                _parsedTokens.Add(target);
                return target;
            }
            else
            {
                Token token = new ComplexToken { TokenType = Identifier, Info = result.ToString() };
                _parsedTokens.Add(token);
                return token;
            }
        }
        else if (char.IsDigit(currentChar)) return StoreTokenWithoutConsume(GetNumberToken());
        else if (currentChar == '.') return StoreTokenWithConsume(new Token { TokenType = Period });
        else if (currentChar == ';') return StoreTokenWithConsume(new Token { TokenType = SemiColon });
        else if (currentChar == ':') return StoreTokenWithConsume(new Token { TokenType = Colon });
        else if (currentChar == '(') return StoreTokenWithConsume(new Token { TokenType = ParenthesesOpen });
        else if (currentChar == ')') return StoreTokenWithConsume(new Token { TokenType = ParenthesesClose });
        else if (currentChar == '[') return StoreTokenWithConsume(new Token { TokenType = BracketsOpen });
        else if (currentChar == ']') return StoreTokenWithConsume(new Token { TokenType = BracketsClose });
        else if (currentChar == '!') return StoreTokenWithConsume(new Token { TokenType = ExclamationMark });
        else if (currentChar == '{') return StoreTokenWithConsume(new Token { TokenType = BracesOpen });
        else if (currentChar == '}') return StoreTokenWithConsume(new Token { TokenType = BracesClose });
        else if (currentChar == '?') return StoreTokenWithConsume(new Token { TokenType = QuestionMark });
        else if (currentChar == ',') return StoreTokenWithConsume(new Token { TokenType = Comma });
        else if (currentChar == '^') return StoreTokenWithConsume(new Token { TokenType = Caret });
        else if (currentChar == '*') return StoreTokenWithConsume(new Token { TokenType = Times });
        else if (currentChar == '"') return StoreTokenWithoutConsume(GetStringToken());
        else if (currentChar == '#') return StoreTokenWithoutConsume(GetPragmaToken());
        else if (currentChar == '\'') return StoreTokenWithoutConsume(GetSingleQuotedStringToken());
        else if (currentChar == '<') return StoreTokenWithoutConsume(GetLessTypeToken());
        else if (currentChar == '>') return StoreTokenWithoutConsume(GetGreaterTypeToken());
        else if (currentChar == '+') return StoreTokenWithoutConsume(GetPlusToken());
        else if (currentChar == '-') return StoreTokenWithoutConsume(GetMinusToken());
        else if (currentChar == '&') return StoreTokenWithoutConsume(GetLogicAndToken());
        else if (currentChar == '%') return StoreTokenWithConsume(new Token { TokenType = Modulus });
        else if (currentChar == '|') return StoreTokenWithoutConsume(GetLogicOrToken());
        else if (currentChar == '$') return StoreTokenWithoutConsume(GetDollarToken());
        else if (currentChar == '@') return StoreTokenWithoutConsume(GetAtToken());
        else if (currentChar == '=') return StoreTokenWithoutConsume(GetAssignToken());

        Console.WriteLine($"Parse stopped at line {_lines[_currentLineIndex]}");
        Environment.Exit(-1);
        throw new ArgumentException("Tokenizer error: weird token!");
    }

    private Token GetPragmaToken()
    {
        StringBuilder pragma = new();
        _nextCharIndex++;
        char ch = CurrentChar();
        while (ch != '\n')
        {
            pragma.Append(ch);
            _nextCharIndex++;
            ch = CurrentChar();
        }
        _nextCharIndex--; // reset so newline will be read
        return new ComplexToken { TokenType = Pragma, Info = pragma.ToString() };
    }

    private Token GetBlockComment()
    {
        // starts at *
        StringBuilder result = new();
        Token? oneLineBlockComment = GetBlockCommentLine(result, BlockCommentWhole,
            BlockCommentStart);
        if (oneLineBlockComment != null) return oneLineBlockComment;
        do
        {
            result.Clear();
            Token? furtherLineBlockComment = GetBlockCommentLine(result, BlockCommentEnd,
                BlockCommentMiddle);
            if (furtherLineBlockComment != null) return furtherLineBlockComment;
        } while (true);
    }

    private Token? GetBlockCommentLine(StringBuilder result, TokenType finishType,
        TokenType nonFinishType)
    {
        do
        {
            _nextCharIndex++;
            char ch = CurrentChar();
            (Token? finalToken, bool startNewLine) = ProcessBlockCommentChar(ch, result, finishType, nonFinishType);
            if (finalToken != null) return finalToken;
            if (startNewLine) break;
        } while (true);
        return null;
    }

    private (Token? finalToken, bool startNewLine) ProcessBlockCommentChar(char ch,
        StringBuilder result, TokenType finishType, TokenType nonFinishType)
    {
        if (ch == '*' && NextChar() == '/')
        {
            _nextCharIndex += 2;
            Token finalToken = new ComplexToken { TokenType = finishType, Info = result.ToString() };
            return (finalToken, false);
        }
        if (ch == '\n')
        {
            StoreNextBlockCommentLine(result, nonFinishType);
            return (null, true);
        }
        result.Append(ch);
        return (null, false);
    }

    private void StoreNextBlockCommentLine(StringBuilder result, TokenType nonFinishType)
    {
        _currentLineIndex++;
        _nextCharIndex = -1; // will soon need to start at 0
        _parsedTokens.Add(new ComplexToken { TokenType = nonFinishType, Info = result.ToString() });
        _parsedTokens.Add(new Token { TokenType = NewLine });
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
        _nextCharIndex++;
        char ch = CurrentChar();
        Token? possibleVerbatimStringToken = GetPossibleVerbatimStringToken(ch);
        if (possibleVerbatimStringToken != null) return possibleVerbatimStringToken;
        if (char.IsLetter(ch)) // @lock and such, for people who want a reserved word as identifier.
        {
            string atIdentifier = ExtractAtStartingIdentifier(ref ch);
            return new ComplexToken { TokenType = Identifier, Info = atIdentifier };
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
        Token? finishedToken = GetVerbatimStringLine(result, VerbatimStringWhole,
            VerbatimStringStart);
        if (finishedToken != null) return finishedToken;

        do
        {
            result.Clear();
            finishedToken = GetVerbatimStringLine(result, VerbatimStringEnd,
            VerbatimStringMiddle);
            if (finishedToken != null) return finishedToken;
        } while (true);
    }

    private Token? GetVerbatimStringLine(StringBuilder result, TokenType finalTokenType,
        TokenType nonFinalTokenType)
    {
        do
        {
            _nextCharIndex++;
            (Token finalToken, bool isBreak) = HandleVerbatimChar(result, finalTokenType,
                nonFinalTokenType);
            if (finalToken != null) return finalToken;
            if (isBreak) break;
        } while (true);
        return null;
    }

    private void HandleVerbatimLineEnd(StringBuilder result, TokenType nonFinalTokenType)
    {
        _currentLineIndex++;
        _nextCharIndex = -1; // will soon need to start at 0
        _parsedTokens.Add(new ComplexToken { TokenType = nonFinalTokenType, Info = result.ToString() });
        _parsedTokens.Add(new Token { TokenType = NewLine });
    }

    private (Token? finalToken, bool isBreak) HandleVerbatimChar(StringBuilder result,
        TokenType finalTokenType, TokenType nonFinalTokenType)
    {
        char ch = CurrentChar();
        if (ch == '"')
        {
            Token? finalToken = HandleDoubleQuotes(result, finalTokenType);
            if (finalToken != null) return (finalToken, false);
        }
        else if (ch == '\n')
        {
            HandleVerbatimLineEnd(result, nonFinalTokenType);
            return (null, true);
        }
        result.Append(ch);
        return (null, false);
    }

    private Token? HandleDoubleQuotes(StringBuilder result, TokenType finalTokenType)
    {
        if (NextChar() != '"')
        {
            _nextCharIndex++;
            return new ComplexToken { TokenType = finalTokenType, Info = result.ToString() };
        }
        else _nextCharIndex += 2; // skip next double quote
        return null;
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
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '+')
        {
            _nextCharIndex++;
            return new Token { TokenType = Increment };
        }
        return new Token { TokenType = Plus };
    }

    private Token GetMinusToken()
    {
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '-')
        {
            _nextCharIndex++;
            return new Token { TokenType = Decrement };
        }
        return new Token { TokenType = Minus };
    }

    private Token GetLogicAndToken()
    {
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '&')
        {
            _nextCharIndex++;
            return new Token { TokenType = LogicAnd };
        }
        throw new ArgumentException("& parse wrong!");
    }

    private Token GetLogicOrToken()
    {
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '|')
        {
            _nextCharIndex++;
            return new Token { TokenType = LogicOr };
        }
        throw new ArgumentException("| parse wrong!");
    }

    private Token GetLessTypeToken()
    {
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '=')
        {
            _nextCharIndex++;
            return new Token { TokenType = Comparator };
        }
        return new Token { TokenType = Less };
    }

    private Token GetAssignToken()
    {
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '>')
        {
            _nextCharIndex++;
            return new Token { TokenType = FatArrow };
        }
        else if (ch == '=')
        {
            _nextCharIndex++;
            return new Token { TokenType = Comparator };
        }
        return new Token { TokenType = Assign };
    }

    private Token GetGreaterTypeToken()
    {
        _nextCharIndex++;
        char ch = CurrentChar();
        if (ch == '=')
        {
            _nextCharIndex++;
            return new Token { TokenType = Comparator };
        }
        return new Token { TokenType = Greater };
    }

    private Token GetNumberToken()
    {
        StringBuilder result = new();
        result.Append(CurrentChar());
        do
        {
            _nextCharIndex++;
            char ch = CurrentChar();
            if (!char.IsDigit(ch) && ch != '_' && ch != '.') break;
            result.Append(ch);
        } while (true);
        return new ComplexToken { TokenType = Number, Info = result.ToString() };
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
        return new ComplexToken { TokenType = TokenType.String, Info = stringContents };
    }

    // TODO: {{ escapes {, so
    private Token GetInterpolatedStringToken()
    {
        bool isEscapeMode = false;
        StringBuilder result = new();
        int initCharIndex = _nextCharIndex + 1;
        TokenType tokenType = InterPolatedStringStart;
        do
        {
            _nextCharIndex++;
            char ch = _lines[_currentLineIndex][_nextCharIndex];
            if (ch == '\\') isEscapeMode = !isEscapeMode;
            else if (ch == '"' && !isEscapeMode) break;
            else if (ch == '{' && !isEscapeMode)
            {
                Token nextToken;
                if (_lines[_currentLineIndex][_nextCharIndex + 1] == '{')
                {
                    _nextCharIndex += 2;
                    result.Append(ch);
                    continue;
                }
                _parsedTokens.Add(new ComplexToken { TokenType = tokenType, Info = result.ToString() });
                result.Clear();
                tokenType = InterpolatedStringMiddle;
                result.Clear();
                _nextCharIndex++;
                do
                {
                    nextToken = Get()!;
                } while (nextToken.TokenType != BracesClose);
                _parsedTokens.RemoveAt(_parsedTokens.Count - 1);// get rid of }
                _nextCharIndex--; // so won't skip " or such
                // get all characters (also on next line) until
            }
            else
            {
                isEscapeMode = false;
                result.Append(ch);
            }
        } while (true);
        _nextCharIndex++;
        return new ComplexToken { TokenType = InterpolatedStringEnd, Info = result.ToString() };
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
        return new ComplexToken { TokenType = SingleQuotedString, Info = stringContents };
    }

    private class TextPosition
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