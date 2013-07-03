using System;
using System.Collections.Generic;
using System.Text;

namespace Assembler
{
	class Assembler
	{
		private readonly TokenList<Token> tokens;
		private int pos;
		private readonly List<Instruction> instructions;
		private readonly Dictionary<string, Label> labels;

		public byte[] Binary;

		private class ExpressionOperation
		{
			public TokenType Operation { get; private set; }
			public short Value { get; private set; }

			public ExpressionOperation(TokenType operation)
			{
				Operation = operation;
				Value = 0;
			}

			public ExpressionOperation(short value)
			{
				Operation = TokenType.Number;
				Value = value;
			}
		}

		public Assembler(string source)
		{
			var tokenizer = new AssemblyTokenizer(source);
			tokenizer.Scan();

			tokens = tokenizer.Tokens;
			instructions = new List<Instruction>();
			labels = new Dictionary<string, Label>();

			Parse();
			Build();
		}

		private void Build()
		{
			var offset = 0;

			for (var i = 0; i < instructions.Count; i++)
			{
				var instruction = instructions[i];

				foreach (var label in labels.Values)
				{
					if (label.Index == i)
						label.Address = offset;
				}

				var bytes = instruction.Assemble();
				offset += bytes.Length;

				if (offset > 32000)
					throw new AssemblerException("Program exceeds 32000 bytes");
			}

			foreach (Label label in labels.Values)
			{
				if (label.Address == 0 && label.Index >= instructions.Count)
					label.Address = offset;
			}

			var assembled = new List<byte>();
			foreach (var instruction in instructions)
			{
				Label label;

				if (instruction.Left != null && instruction.Left.OperandType == OperandType.Label)
				{
					if (!labels.TryGetValue(instruction.Left.Label, out label))
						throw new AssemblerException(string.Format("Unresolved label '{0}' on line {1}.", instruction.Left.Label,
																	instruction.Left.Line));

					instruction.Left.Payload = (short)label.Address;
				}

				if (instruction.Right != null && instruction.Right.OperandType == OperandType.Label)
				{
					if (!labels.TryGetValue(instruction.Right.Label, out label))
						throw new AssemblerException(string.Format("Unresolved label '{0}' on line {1}.", instruction.Right.Label, instruction.Right.Line));

					instruction.Right.Payload = (short)label.Address;
				}

				assembled.AddRange(instruction.Assemble());
			}

			Binary = assembled.ToArray();
		}

		private void Parse()
		{
			var t = tokens[pos];

			while (t.Type != TokenType.EndOfFile)
			{
				if (t.Type == TokenType.Label)
				{
					if (labels.ContainsKey(t.Value))
						throw new AssemblerException(string.Format("Duplicate label '{0}' on line {1}.", t.Value, t.Line));

					labels.Add(t.Value, new Label(t.Value, instructions.Count));
					pos++;
				}
				else if (t.Type == TokenType.Keyword)
				{
					instructions.Add(ParseInstruction());
				}
				else if (t.Type == TokenType.Word && new List<string> { "db", "dw", "rb" }.Contains(t.Value.ToLower()))
				{
					string type = t.Value;
					var dataLine = t.Line;
					t = tokens[++pos];

					var data = new DataInstruction();
					while (t.Type == TokenType.String || t.Type == TokenType.Number)
					{
						if (t.Type == TokenType.String)
						{
							data.Add(t.Value);
						}
						else if (type == "db")
						{
							byte value;
							if (!byte.TryParse(t.Value, out value))
								throw new AssemblerException(string.Format("Value out of range (0-255) on line {0}.", t.Line));

							data.Add(value);
						}
						else if (type == "dw")
						{
							short value;
							if (!short.TryParse(t.Value, out value))
								throw new AssemblerException(string.Format("Value out of range (0-65535) on line {0}.", t.Line));

							data.Add(value);
						}
						else if (type == "rb")
						{
							int value;
							if (!int.TryParse(t.Value, out value))
								throw new AssemblerException(String.Format("Value out of range (0-4294967295) on line {0}.", t.Line));

							while (value-- > 0)
								data.Add(0);
						}

						pos++;

						if (tokens[pos].Type == TokenType.Comma)
						{
							t = tokens[++pos];
							continue;
						}

						break;
					}

					if (!data.HasData)
						throw new AssemblerException(string.Format("Empty data directive on line {0}.", dataLine));

					instructions.Add(data);
				}
				else
				{
					throw new AssemblerException(string.Format("Unexpected {0} on line {1}.", t.Type, t.Line));
				}

				t = tokens[pos];
			}
		}

		#region gross
		private static readonly Dictionary<Instructions, int> operandCounts = new Dictionary<Instructions, int>
        {
            { Instructions.Set,     2 },
            { Instructions.Add,     2 },
            { Instructions.Sub,     2 },
            { Instructions.Mul,     2 },
            { Instructions.Div,     2 },
            { Instructions.Mod,     2 },
            { Instructions.Inc,     1 },
            { Instructions.Dec,     1 },

            { Instructions.Not,     1 },
            { Instructions.And,     2 },
            { Instructions.Or,      2 },
            { Instructions.Xor,     2 },
            { Instructions.Shl,     2 },
            { Instructions.Shr,     2 },

            { Instructions.Push,    1 },
            { Instructions.Pop,     1 },

            { Instructions.Jmp,     1 },
            { Instructions.Call,    1 },
            { Instructions.Ret,     0 },

            { Instructions.In,      2 },
            { Instructions.Out,     2 },

            { Instructions.Cmp,     2 },
            { Instructions.Jz,      1 },
            { Instructions.Jnz,     1 },
            { Instructions.Je,      1 },
            { Instructions.Ja,      1 },
            { Instructions.Jb,      1 },
            { Instructions.Jae,     1 },
            { Instructions.Jbe,     1 },
            { Instructions.Jne,     1 },
        };
		#endregion

		private Instruction ParseInstruction()
		{
			var t = tokens[pos++];

			Instructions type;
			if (!Enum.TryParse(t.Value, true, out type))
				throw new AssemblerException(string.Format("Expected opcode on line {0}.", t.Line));

			var operandCount = operandCounts[type];

			if (operandCount == 0)
				return new Instruction(type, null, null);

			Operand left = ParseOperand();

			if (operandCount == 1)
				return new Instruction(type, left, null);

			Require(TokenType.Comma);
			Operand right = ParseOperand();
			return new Instruction(type, left, right);
		}

		private Operand ParseOperand()
		{
			var t = tokens[pos];
			var ptr = false;

			if (t.Type == TokenType.OpenBracket)
			{
				ptr = true;
				pos++;
			}

			try
			{
				t = tokens[pos++];

				switch (t.Type)
				{
					case TokenType.Word:
					{
						Registers register;
						if (Enum.TryParse(t.Value, true, out register))
						{
							return Operand.FromRegister(register, ptr);
						}
						else
						{
							return Operand.FromLabel(t.Value, t.Line, ptr);
						}
					}

					case TokenType.BitwiseNot:
					case TokenType.OpenParentheses:
					case TokenType.Number:
						--pos;
						return Operand.FromNumber(EvaluateExpression(), ptr);

					case TokenType.String:
					{
						var strBytes = Encoding.GetEncoding(437).GetBytes(t.Value);
						if (strBytes.Length < 1 || strBytes.Length > 2)
							throw new AssemblerException(string.Format("Bad string literal size on line {0}.", t.Line));
						return Operand.FromNumber(strBytes.Length == 2 ? BitConverter.ToInt16(strBytes, 0) : (sbyte)strBytes[0], ptr);
					}
				}

				throw new AssemblerException(string.Format("Expected operand on line {0}.", t.Line));
			}
			finally
			{
				if (ptr)
				{
					Require(TokenType.CloseBracket);
				}
			}
		}

		private short EvaluateExpression()
		{
			Stack<ExpressionOperation> operations = new Stack<ExpressionOperation>();
			Stack<int> stack = new Stack<int>();

			BitwiseExpression(operations);

			operations = new Stack<ExpressionOperation>(operations);

			while (operations.Count != 0)
			{
				ExpressionOperation operation = operations.Pop();
				switch (operation.Operation)
				{
					case TokenType.Number:
						stack.Push(operation.Value);
						break;

					case TokenType.BitwiseNot:
						stack.Push(~stack.Pop());
						break;

					case TokenType.BitwiseAnd:
					{
						int a = stack.Pop();
						int b = stack.Pop();

						stack.Push(b & a);
						break;
					}

					case TokenType.BitwiseOr:
					{
						int a = stack.Pop();
						int b = stack.Pop();

						stack.Push(b | a);
						break;
					}

					case TokenType.BitwiseXor:
					{
						int a = stack.Pop();
						int b = stack.Pop();

						stack.Push(b ^ a);
						break;
					}

					case TokenType.Add:
					{
						int a = stack.Pop();
						int b = stack.Pop();

						stack.Push(b + a);
						break;
					}

					case TokenType.Subtract:
					{
						int a = stack.Pop();
						int b = stack.Pop();

						stack.Push(b - a);
						break;
					}

					case TokenType.Multiply:
					{
						int a = stack.Pop();
						int b = stack.Pop();

						stack.Push(b * a);
						break;
					}

					case TokenType.Divide:
					{
						int a = stack.Pop();
						int b = stack.Pop();

						stack.Push(b / a);
						break;
					}

					case TokenType.Modulo:
					{
						int a = stack.Pop();
						int b = stack.Pop();

						stack.Push(b % a);
						break;
					}
				}
			}

			return (short)stack.Pop();
		}

		private void BitwiseExpression(Stack<ExpressionOperation> operations)
		{
			if (Accept(TokenType.BitwiseNot))
			{
				Expression(operations);
				operations.Push(new ExpressionOperation(TokenType.BitwiseNot));
			}
			else
				Expression(operations);

			while (AssemblyTokenizer.IsBitwiseOperation(tokens[pos].Type))
			{
				if (Accept(TokenType.BitwiseAnd))
				{
					Expression(operations);
					operations.Push(new ExpressionOperation(TokenType.BitwiseAnd));
				}
				else if (Accept(TokenType.BitwiseOr))
				{
					Expression(operations);
					operations.Push(new ExpressionOperation(TokenType.BitwiseOr));
				}
				else if (Accept(TokenType.BitwiseXor))
				{
					Expression(operations);
					operations.Push(new ExpressionOperation(TokenType.BitwiseXor));
				}
				else
					throw new AssemblerException("Expected bitwise operation.");
			}
		}

		private void Expression(Stack<ExpressionOperation> operations)
		{
			Term(operations);

			while (AssemblyTokenizer.IsExpressionOperation(tokens[pos].Type))
			{
				if (Accept(TokenType.Add))
				{
					Term(operations);
					operations.Push(new ExpressionOperation(TokenType.Add));
				}
				else if (Accept(TokenType.Subtract))
				{
					Term(operations);
					operations.Push(new ExpressionOperation(TokenType.Subtract));
				}
				else
					throw new AssemblerException("Expected expression operation.");
			}
		}

		private void Term(Stack<ExpressionOperation> operations)
		{
			Factor(operations);

			while (AssemblyTokenizer.IsTermOperation(tokens[pos].Type))
			{
				if (Accept(TokenType.Multiply))
				{
					Factor(operations);
					operations.Push(new ExpressionOperation(TokenType.Multiply));
				}
				else if (Accept(TokenType.Divide))
				{
					Factor(operations);
					operations.Push(new ExpressionOperation(TokenType.Divide));
				}
				else if (Accept(TokenType.Modulo))
				{
					Factor(operations);
					operations.Push(new ExpressionOperation(TokenType.Modulo));
				}
				else
					throw new AssemblerException("Unexpected term operation.");
			}
		}

		private void Factor(Stack<ExpressionOperation> operations)
		{
			if (Accept(TokenType.OpenParentheses))
			{
				Expression(operations);
				Require(TokenType.CloseParentheses);
			}
			else if (tokens[pos].Type == TokenType.Number)
			{
				short value;
				if (!short.TryParse(tokens[pos].Value, out value))
					throw new AssemblerException(String.Format("Expected number on line {0}.", tokens[pos].Line));

				operations.Push(new ExpressionOperation(value));
				++pos;
			}
			else if (tokens[pos].Type == TokenType.Label)
			{
				throw new NotImplementedException();
			}
			else
				throw new AssemblerException(
					String.Format("Expected open parentheses, number or label on line {0}, got \"{1}\" instead.",
								  tokens[pos].Line, tokens[pos].Value));
		}

		private void Require(TokenType tokenType)
		{
			if (tokens[pos++].Type != tokenType)
				throw new AssemblerException("Expected " + tokenType);
		}

		private bool Accept(TokenType tokenType)
		{
			if (tokens[pos].Type == tokenType)
			{
				++pos;
				return true;
			}

			return false;
		}
	}
}
