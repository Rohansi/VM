using System;
using System.Collections.Generic;
using System.IO;

namespace Assembler
{
    public enum TokenType
    {
        EndOfFile, Number, String,
        Word, Keyword, Label, Comma, OpenBracket, CloseBracket, Period,
        OpenParentheses, CloseParentheses,
        Add, Subtract, Multiply, Divide, Modulo,
        BitwiseNot, BitwiseAnd, BitwiseOr, BitwiseXor
    }

    public struct Token
    {
        public readonly TokenType Type;
        public readonly string Filename;
        public readonly int Line;
        public readonly string Value;

        public Token(TokenType type, string value = null, int line = -1, string filename = "")
        {
            Type = type;
            Line = line;
            Filename = filename;
            Value = value;
        }
    }

    public class AssemblyTokenizer
    {
        private readonly string source;
        private readonly List<Token> tokens;
        private bool hasTokenized;
        private readonly Dictionary<string, List<Token>> defines;

        public AssemblyTokenizer(string input)
        {
            source = input;
            tokens = new List<Token>();
            defines = new Dictionary<string, List<Token>>();
        }

        public TokenList<Token> Tokens
        {
            get
            {
                if (!hasTokenized)
                    throw new InvalidOperationException("Scan() has not been called");

                if (tokens.Count > 0)
                {
                    var end = new Token(TokenType.EndOfFile, line: tokens[tokens.Count - 1].Line);
                    return new TokenList<Token>(tokens, end);
                }
                else
                {
                    var end = new Token(TokenType.EndOfFile, line: 1);
                    return new TokenList<Token>(tokens, end);
                }
            }
        }

        public void Scan()
        {
            if (hasTokenized)
                throw new InvalidOperationException("Scan() has already been called");

            var tokenizer = new Tokenizer(source);
            tokenizer.Scan();

            ScanSource(tokenizer.Tokens);

            hasTokenized = true;
        }

        private void ScanSource(TokenList<BasicToken> sourceTokens)
        {
            for (var i = 0; sourceTokens[i].Type != BasicTokenType.EndOfFile; i++)
            {
                var token = sourceTokens[i];

                switch (token.Type)
                {
                    case BasicTokenType.Word:
                        {
                            HashSet<string> opcodes = new HashSet<string>
						{
							"set", "add", "sub", "mul", "div", "mod", "inc", "dec", "not", "and", "or", "xor", "shl",
							"shr", "push", "pop", "jmp", "call", "ret", "in", "out", "cmp", "jz", "jnz", "je", "ja",
							"jb", "jae", "jbe", "jne"
						};

                            if (opcodes.Contains(token.Value.ToLower()))
                                tokens.Add(new Token(TokenType.Keyword, token.Value.ToLower(), token.Line));
                            else
                            {
                                if (defines.ContainsKey(token.Value))
                                {
                                    List<Token> define = defines[token.Value];
                                    foreach (Token defineToken in define)
                                        tokens.Add(new Token(defineToken.Type, defineToken.Value, defineToken.Line));
                                }
                                else
                                    tokens.Add(new Token(TokenType.Word, token.Value, token.Line));
                            }
                            break;
                        }

                    case BasicTokenType.Delimiter:
                        {
                            Dictionary<string, TokenType> delimiters = new Dictionary<string, TokenType>
						{
							{ ",", TokenType.Comma },
							{ "[", TokenType.OpenBracket },
							{ "]", TokenType.CloseBracket },
							{ "(", TokenType.OpenParentheses },
							{ ")", TokenType.CloseParentheses },
							{ ".", TokenType.Period },
							{ "+", TokenType.Add },
							{ "-", TokenType.Subtract },
							{ "*", TokenType.Multiply },
							{ "/", TokenType.Divide },
							{ "%", TokenType.Modulo },
							{ "~", TokenType.BitwiseNot },
							{ "&", TokenType.BitwiseAnd },
							{ "|", TokenType.BitwiseOr },
							{ "^", TokenType.BitwiseXor }
						};

                            if (delimiters.ContainsKey(token.Value))
                            {
                                tokens.Add(new Token(delimiters[token.Value], token.Value, token.Line));
                                break;
                            }

                            if (token.Value == ":" && tokens.Count > 0)
                            {
                                var last = tokens[tokens.Count - 1];
                                if (last.Type == TokenType.Word)
                                {
                                    tokens.RemoveAt(tokens.Count - 1);
                                    tokens.Add(new Token(TokenType.Label, last.Value, last.Line));
                                    break;
                                }
                            }

                            if (token.Value == "#")
                            {
                                token = sourceTokens[++i];
                                switch (token.Value)
                                {
                                    case "include":
                                        {
                                            BasicToken filenameToken = sourceTokens[++i];
                                            string includeSource;

                                            try
                                            {
                                                includeSource = File.ReadAllText(filenameToken.Value);
                                            }
                                            catch (Exception)
                                            {
                                                throw new AssemblerException(String.Format("Cannot open included file \"{0}\" at {2}:{1}.",
                                                    filenameToken.Value, filenameToken.Line, filenameToken.Filename));
                                            }

                                            var tokenizer = new Tokenizer(includeSource);
                                            tokenizer.Scan();

                                            ScanSource(tokenizer.Tokens);
                                            break;
                                        }

                                    case "define":
                                        {
                                            List<Token> defineTokens = new List<Token>();
                                            BasicToken name = sourceTokens[++i];

                                            while (sourceTokens[++i].Line == name.Line && i < sourceTokens.Count)
                                            {
                                                defineTokens.Add(new Token(TokenType.Number, sourceTokens[i].Value, sourceTokens[i].Line));
                                            }

                                            defines.Add(name.Value, defineTokens);
                                            --i;
                                            break;
                                        }

                                    default:
                                        throw new AssemblerException(String.Format("Unexpected preprocessor directive \"{0}\".", token.Value));
                                }
                                break;
                            }

                            throw new AssemblerException(String.Format("Unexpected delimiter '{0}'", token.Value));
                        }

                    case BasicTokenType.Number:
                        tokens.Add(new Token(TokenType.Number, token.Value, token.Line));
                        break;

                    case BasicTokenType.String:
                        tokens.Add(new Token(TokenType.String, token.Value, token.Line));
                        break;

                    default:
                        throw new AssemblerException(String.Format("Unhandled BasicToken {0}", token.Type));
                }
            }
        }

        public static bool IsExpressionOperation(TokenType type)
        {
            return new HashSet<TokenType>
		    {
			    TokenType.Add,
			    TokenType.Subtract
		    }.Contains(type);
        }

        public static bool IsTermOperation(TokenType type)
        {
            return new HashSet<TokenType>
		    {
			    TokenType.Multiply,
			    TokenType.Divide,
			    TokenType.Modulo
		    }.Contains(type);
        }

        public static bool IsBitwiseOperation(TokenType type)
        {
            return new HashSet<TokenType>
		    {
			    TokenType.BitwiseNot,
			    TokenType.BitwiseAnd,
			    TokenType.BitwiseOr,
			    TokenType.BitwiseXor
		    }.Contains(type);
        }
    }
}
