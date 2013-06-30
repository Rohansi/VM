using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assembler
{
    public enum Instructions
    {
        Set, Add, Sub, Mul, Div, Mod, Inc, Dec,
        Not, And, Or, Xor, Shl, Shr,
        Push, Pop,
        Jmp, Call, Ret,
        In, Out,
        Cmp, Jz, Jnz, Je, Ja, Jb, Jae, Jbe,

        Db, None
    }

    class Instruction
    {
        public readonly Instructions Type;
        public Operand Left;
        public Operand Right;

        protected Instruction()
        {
            Type = Instructions.None;
            Left = null;
            Right = null;
        }

        public Instruction(Instructions type, Operand left, Operand right)
        {
            Type = type;
            Left = left;
            Right = right;
        }

        public virtual byte[] Assemble()
        {
            int operandCount;
            if (Left == null && Right == null)
                operandCount = 0;
            else if (Left != null && Right != null)
                operandCount = 2;
            else
                operandCount = 1;

            var extended = (Left != null && Left.Pointer) || (Right != null && Right.Pointer);
            var len = operandCount == 0 ? 1 : (extended ? 3 : 2);

            var bytes = new byte[len];
            bytes[0] = (byte)((int)Type << 3);
            if (!extended)
            {
                if (Left != null)
                {
                    bytes[0] |= (byte)((Left.Type & 24) >> 3);
                    bytes[1] |= (byte)(Left.Type << 5);
                }

                if (Right != null)
                    bytes[1] |= (byte)(Right.Type & 31);
            }
            else
            {
                bytes[0] |= 4;
                if (Left != null && Left.Pointer)
                    bytes[0] |= 2;
                if (Right != null && Right.Pointer)
                    bytes[0] |= 1;
                if (Left != null)
                    bytes[1] = (byte)Left.Type;
                if (Right != null)
                    bytes[2] = (byte)Right.Type;
            }

            if (Left != null)
                bytes = bytes.Concat(Left.PayloadBytes).ToArray();
            if (Right != null)
                bytes = bytes.Concat(Right.PayloadBytes).ToArray();

            return bytes;
        }
    }

    class DataInstruction : Instruction
    {
        private List<byte> _bytes = new List<byte>();

        public bool HasData
        {
            get { return _bytes.Count > 0; }
        }
         
        public void Add(byte value)
        {
            _bytes.Add(value);
        }

        public void Add(string value)
        {
            _bytes.AddRange(Encoding.GetEncoding(437).GetBytes(value));
        }

        public override byte[] Assemble()
        {
            return _bytes.ToArray();
        }
    }
}
