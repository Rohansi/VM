using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Assembler
{
    public enum BasicTokenType
    {
        EndOfFile, Delimiter, String, Word, Number
    }

    public class Tokenizer
    {
        private const char LineBreak = '\n';
        private const string Delimiters = ":,[]";

        private readonly string source;
        private readonly List<BasicToken> tokens;
        private bool hasTokenized;
        private int lineCount;
        private int currentLine;
        private int futureLine;
        private int pos;

        public Tokenizer(string input)
        {
            tokens = new List<BasicToken>();
            source = input;
            hasTokenized = false;
            currentLine = futureLine = 1;
        }

        public TokenList<BasicToken> Tokens
        {
            get
            {
                if (!hasTokenized)
                    throw new InvalidOperationException("Scan() has not been called");
                var end = new BasicToken(BasicTokenType.EndOfFile, line: lineCount);
                return new TokenList<BasicToken>(tokens, end);
            }
        }

        public void Scan()
        {
            if (hasTokenized)
                throw new InvalidOperationException("Scan() has already been called");

            try
            {
                while (pos < source.Length)
                {
                    currentLine = futureLine;

                    // Skip whitespace
                    while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                    {
                        if (source[pos++] == LineBreak)
                            futureLine++;


                    }

                    if (pos >= source.Length)
                        continue;

                    // Single line comment
                    if (pos < source.Length - 1 && source.Substring(pos, 2) == "//")
                    {
                        while (pos < source.Length && source[pos] != LineBreak)
                        {
                            pos++;
                        }
                        continue;
                    }

                    // Multi-line comment
                    if (pos < source.Length - 1 && source.Substring(pos, 2) == "/*")
                    {
                        pos += 2;
                        while (source.Substring(pos, 2) != "*/")
                        {
                            if (source[pos++] == LineBreak)
                                futureLine++;
                        }
                        pos += 2;
                        continue;
                    }

                    // Delimiters
                    if (Delimiters.Contains(source[pos]))
                    {
                        AddToken(BasicTokenType.Delimiter, "" + source[pos++]);
                        continue;
                    }

                    // Strings
                    if (source[pos] == '"')
                    {
                        var value = "";

                        while (source[++pos] != '"')
                        {
                            var chValue = "" + source[pos];

                            if (source[pos] == LineBreak)
                                futureLine++;

                            if (source[pos] == '\\')
                            {
                                pos++;
                                switch (source[pos])
                                {
                                    case 'a':
                                        chValue = "\a";
                                        break;
                                    case 'b':
                                        chValue = "\b";
                                        break;
                                    case 'f':
                                        chValue = "\f";
                                        break;
                                    case 'n':
                                        chValue = "\n";
                                        break;
                                    case 'r':
                                        chValue = "\r";
                                        break;
                                    case 't':
                                        chValue = "\t";
                                        break;
                                    case 'v':
                                        chValue = "\v";
                                        break;
                                    case '"':
                                        chValue = "\"";
                                        break;
                                    case '\\':
                                        chValue = "\\";
                                        break;
                                    case '0':
                                        chValue = "\0";
                                        break;
                                    case 'x':
                                        var hex = "" + source[++pos] + source[++pos];
                                        try
                                        {
                                            var hexVal = Convert.ToByte(hex, 16);
                                            chValue = Encoding.GetEncoding(437).GetString(new[] { hexVal });
                                        }
                                        catch
                                        {
                                            throw new AssemblerException(string.Format("Invalid hexadecimal escape sequence on line {0}", currentLine));
                                        }
                                        break;
                                    default:
                                        chValue = "" + source[pos];
                                        break;
                                }
                            }

                            value += chValue;
                        }

                        AddToken(BasicTokenType.String, value);
                        pos++;
                        continue;
                    }

                    // Word
                    if (char.IsLetter(source[pos]) || source[pos] == '_')
                    {
                        var value = "";

                        while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                        {
                            value += source[pos++];
                        }

                        AddToken(BasicTokenType.Word, value);
                        continue;
                    }

                    // Number
                    if (char.IsDigit(source[pos]) || source[pos] == '-')
                    {
                        var negative = source[pos] == '-';
                        var hex = false;

                        var value = "";

                        if (negative)
                        {
                            value = "-";
                            pos++;
                        }

                        while (pos < source.Length && char.IsDigit(source[pos]))
                        {
                            value += source[pos++];
                        }

                        if (pos < source.Length && value == "0" && source[pos] == 'x' && !negative)
                        {
                            pos++;
                            value = "";
                            hex = true;

                            while (pos < source.Length && char.IsLetterOrDigit(source[pos]))
                            {
                                value += source[pos++];
                            }

                            if (value.Length == 0)
                                throw new AssemblerException(string.Format("Invalid hexadecimal number on line {0}", currentLine));
                        }

                        short number;
                        try
                        {
                            if (!hex)
                                number = short.Parse(value, CultureInfo.InvariantCulture);
                            else
                                number = Convert.ToInt16(value, 16);
                        }
                        catch (Exception e)
                        {
                            throw new AssemblerException(string.Format("Invalid number on line {0}", currentLine), e);
                        }

                        AddToken(BasicTokenType.Number, number.ToString("D", CultureInfo.InvariantCulture));
                        continue;
                    }

                    throw new AssemblerException(string.Format("Unexpected character '{0}' on line {1}", source[pos], currentLine));
                }

                lineCount = futureLine;
                hasTokenized = true;
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException ||
                    e is ArgumentOutOfRangeException)
                {
                    throw new AssemblerException(string.Format("Unexpected end of file from token on line {0}.", currentLine), e);
                }
                throw;
            }
        }

        private bool IsNext(string str)
        {
            if (pos + str.Length > source.Length)
                return false;
            return source.Substring(pos, str.Length) == str;
        }

        private void AddToken(BasicTokenType type, string value = "")
        {
            tokens.Add(new BasicToken(type, value, futureLine));
        }
    }

    public struct BasicToken
    {
        public readonly BasicTokenType Type;
        public readonly int Line;
        public readonly string Value;

        public BasicToken(BasicTokenType type, string value = null, int line = -1)
        {
            Type = type;
            Line = line;
            Value = value;
        }

        public bool Matches(BasicToken template)
        {
            if (!string.IsNullOrEmpty(template.Value) && Value != template.Value)
                return false;
            return Type == template.Type;
        }
    }
}
