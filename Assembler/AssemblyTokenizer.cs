using System;
using System.Collections.Generic;

namespace Assembler
{
    public enum TokenType
    {
        EndOfFile, Number, String,
        Word, Keyword, Label, Comma, OpenBracket, CloseBracket, Period,
		OpenParentheses, CloseParentheses,
		Add, Subtract, Multiply, Divide, Modulo
    }

    public struct Token
    {
        public readonly TokenType Type;
        public readonly int Line;
        public readonly string Value;

        public Token(TokenType type, string value = null, int line = -1)
        {
            Type = type;
            Line = line;
            Value = value;
        }
    }

    public class AssemblyTokenizer
    {
        private readonly string source;
        private readonly List<Token> tokens;
        private bool hasTokenized;

        public AssemblyTokenizer(string input)
        {
            source = input;
            tokens = new List<Token>();
        }

        public TokenList<Token> Tokens
        {
            get
            {
                if (!hasTokenized)
                    throw new InvalidOperationException("Scan() has not been called");
                var end = new Token(TokenType.EndOfFile, line: tokens[tokens.Count - 1].Line);
                return new TokenList<Token>(tokens, end);
            }
        }

        public void Scan()
        {
            if (hasTokenized)
                throw new InvalidOperationException("Scan() has already been called");

            var tokenizer = new Tokenizer(source);
            tokenizer.Scan();

            var t = tokenizer.Tokens;
            for (var i = 0; t[i].Type != BasicTokenType.EndOfFile; i++)
            {
                var tok = t[i];

                switch (tok.Type)
                {
                    case BasicTokenType.Word:
                    {
                        switch (tok.Value.ToLower())
                        {
                            case "set":
                            case "add":
                            case "sub":
                            case "mul":
                            case "div":
                            case "mod":
                            case "inc":
                            case "dec":
                            case "not":
                            case "and":
                            case "or":
                            case "xor":
                            case "shl":
                            case "shr":
                            case "push":
                            case "pop":
                            case "jmp":
                            case "call":
                            case "ret":
                            case "in":
                            case "out":
                            case "cmp":
                            case "jz":
                            case "jnz":
                            case "je":
                            case "ja":
                            case "jb":
                            case "jae":
                            case "jbe":
                            case "jne":
                                tokens.Add(new Token(TokenType.Keyword, tok.Value.ToLower(), tok.Line));
                                break;
                            default:
                                tokens.Add(new Token(TokenType.Word, tok.Value, tok.Line));
                                break;
                        }
                        break;
                    }

                    case BasicTokenType.Delimiter:
                    {
                        if (tok.Value == ",")
                        {
                            tokens.Add(new Token(TokenType.Comma, tok.Value, tok.Line));
                            break;
                        }

                        if (tok.Value == "[")
                        {
                            tokens.Add(new Token(TokenType.OpenBracket, tok.Value, tok.Line));
                            break;
                        }

                        if (tok.Value == "]")
                        {
                            tokens.Add(new Token(TokenType.CloseBracket, tok.Value, tok.Line));
                            break;
                        }

                        if (tok.Value == ":" && tokens.Count > 0)
                        {
                            var last = tokens[tokens.Count - 1];
                            if (last.Type == TokenType.Word)
                            {
                                tokens.RemoveAt(tokens.Count - 1);
                                tokens.Add(new Token(TokenType.Label, last.Value, last.Line));
                                break;
                            }
                        }

						if (tok.Value == "(")
						{
							tokens.Add(new Token(TokenType.OpenParentheses, tok.Value, tok.Line));
							break;
						}

						if (tok.Value == ")")
						{
							tokens.Add(new Token(TokenType.CloseParentheses, tok.Value, tok.Line));
							break;
						}

						if (tok.Value == ".")
						{
							tokens.Add(new Token(TokenType.Period, tok.Value, tok.Line));
							break;
						}

						if (tok.Value == "+")
						{
							tokens.Add(new Token(TokenType.Add, tok.Value, tok.Line));
							break;
						}

						if (tok.Value == "-")
						{
							tokens.Add(new Token(TokenType.Subtract, tok.Value, tok.Line));
							break;
						}

						if (tok.Value == "*")
						{
							tokens.Add(new Token(TokenType.Multiply, tok.Value, tok.Line));
							break;
						}

						if (tok.Value == "/")
						{
							tokens.Add(new Token(TokenType.Divide, tok.Value, tok.Line));
							break;
						}

						if (tok.Value == "%")
						{
							tokens.Add(new Token(TokenType.Modulo, tok.Value, tok.Line));
							break;
						}

                        throw new AssemblerException(string.Format("Unexpected delimiter '{0}'", tok.Value));
                    }

                    case BasicTokenType.Number:
                    {
                        tokens.Add(new Token(TokenType.Number, tok.Value, tok.Line));
                        break;
                    }

                    case BasicTokenType.String:
                    {
                        tokens.Add(new Token(TokenType.String, tok.Value, tok.Line));
                        break;
                    }

                    default:
                    throw new AssemblerException(string.Format("Unhandled BasicToken {0}", tok.Type));
                }
            }

            hasTokenized = true;
        }
    }
}
