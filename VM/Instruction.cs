using System;

namespace VM
{
    public enum Instructions
    {
        Set, Add, Sub, Mul, Div, Mod, Inc, Dec,
        Not, And, Or, Xor, Shl, Shr,
        Push, Pop,
        Jmp, Call, Ret,
        In, Out,
        Cmp, Jz, Jnz, Je, Ja, Jb, Jae, Jbe, Jne, 

        Count
    }

    class Instruction
    {
        private static int[] operandCounts;

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
            var operandCount = operandCounts[id];

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

        static Instruction()
        {
            operandCounts = new int[(int)Instructions.Count];

            operandCounts[(int)Instructions.Set] = 2;
            operandCounts[(int)Instructions.Add] = 2;
            operandCounts[(int)Instructions.Sub] = 2;
            operandCounts[(int)Instructions.Mul] = 2;
            operandCounts[(int)Instructions.Div] = 2;
            operandCounts[(int)Instructions.Mod] = 2;
            operandCounts[(int)Instructions.Inc] = 1;
            operandCounts[(int)Instructions.Dec] = 1;

            operandCounts[(int)Instructions.Not] = 1;
            operandCounts[(int)Instructions.And] = 2;
            operandCounts[(int)Instructions.Or] = 2;
            operandCounts[(int)Instructions.Xor] = 2;
            operandCounts[(int)Instructions.Shl] = 2;
            operandCounts[(int)Instructions.Shr] = 2;

            operandCounts[(int)Instructions.Push] = 1;
            operandCounts[(int)Instructions.Pop] = 1;

            operandCounts[(int)Instructions.Jmp] = 1;
            operandCounts[(int)Instructions.Call] = 1;
            operandCounts[(int)Instructions.Ret] = 0;

            operandCounts[(int)Instructions.In] = 2;
            operandCounts[(int)Instructions.Out] = 2;

            operandCounts[(int)Instructions.Cmp] = 2;
            operandCounts[(int)Instructions.Jz] = 1;
            operandCounts[(int)Instructions.Jnz] = 1;
            operandCounts[(int)Instructions.Je] = 1;
            operandCounts[(int)Instructions.Ja] = 1;
            operandCounts[(int)Instructions.Jb] = 1;
            operandCounts[(int)Instructions.Jae] = 1;
            operandCounts[(int)Instructions.Jbe] = 1;
            operandCounts[(int)Instructions.Jne] = 1;
        }
    }
}
