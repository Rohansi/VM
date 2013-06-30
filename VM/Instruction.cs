using System;
using System.Collections.Generic;

namespace VM
{
    public enum Instructions
    {
        Set, Add, Sub, Mul, Div, Mod, Inc, Dec,
        Not, And, Or, Xor, Shl, Shr,
        Push, Pop,
        Jmp, Call, Ret,
        In, Out,
        Cmp, Jz, Jnz, Je, Ja, Jb, Jae, Jbe,

        Count
    }

    class Instruction
    {
        private static Dictionary<Instructions, int> operandCounts = new Dictionary<Instructions, int>()
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
        };

        public readonly Instructions Type;
        public readonly Operand Left;
        public readonly Operand Right;

        public Instruction(VirtualMachine machine)
        {
            var memory = machine.Memory;

            var byte1 = memory[machine.IP++];
            var id = byte1 >> 3;
            var extended = (byte1 & 4) != 0;

            if (id > (int)Instructions.Count)
                throw new VmException(string.Format("Bad opcode at {0}", machine.IP - 1));

            Type = (Instructions)id;
            var operandCount = operandCounts[Type];

            if (operandCount == 0)
                return;

            var byte2 = memory[machine.IP++];
            var leftType = 0;
            var leftPtr = false;
            short leftPayload = 0;
            var rightType = 0;
            var rightPtr = false;
            short rightPayload = 0;

            if (!extended)
            {
                leftType = ((byte1 & 3) << 3) | (byte2 >> 5);
                rightType = byte2 & 31;
            }
            else
            {
                leftType = byte2;
                rightType = memory[machine.IP++];
                leftPtr = (byte1 & 2) != 0;
                rightPtr = (byte1 & 1) != 0;
            }

            if (leftType == 18) // full payload
                leftPayload = (short)(machine.Memory[machine.IP++] | (machine.Memory[machine.IP++] << 8));

            Left = new Operand(machine, leftType, leftPtr, leftPayload);

            if (operandCount == 1)
                return;

            if (rightType == 18)
                rightPayload = (short)(machine.Memory[machine.IP++] | (machine.Memory[machine.IP++] << 8));

            Right = new Operand(machine, rightType, rightPtr, rightPayload);
        }

        public override string ToString()
        {
            var res = Type.ToString().ToUpper();
            if (Left != null)
                res += " " + Left;
            if (Right != null)
                res += ", " + Right;
            return res;
        }
    }
}
