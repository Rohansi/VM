using System;

namespace Assembler
{
    public enum Registers
    {
        R0, R1, R2, R3, R4, R5, R6, R7, R8, R9, RA, RB, RC, RD, RE, RF, SP, IP
    }

    enum OperandType
    {
        Register, Number, Label
    }

    class Operand
    {
        private Registers register;

        public OperandType OperandType;
        public int Type
        {
            get
            {
                if (OperandType == OperandType.Register)
                    return (int)register;
                if (OperandType == OperandType.Number || OperandType == OperandType.Label)
                    return 18;

                throw new NotImplementedException();
            }
        }

        public bool Pointer;
        public bool Byte;
        public short? Payload;
        public byte[] PayloadBytes
        {
            get
            {
                if (Payload == null)
                    return new byte[0];
                if (Type == 18)
                    return BitConverter.GetBytes(Payload.Value);

                throw new NotImplementedException();
            }
        }

        // only for labels
        public string Label;
        public int Line;

        public static Operand FromNumber(short number, bool ptr, bool b)
        {
            return new Operand
            {
                OperandType = OperandType.Number,
                Pointer = ptr,
                Byte = b,
                Payload = number
            };
        }

        public static Operand FromRegister(Registers register, bool ptr, bool b)
        {
            return new Operand
            {
                OperandType = OperandType.Register,
                register = register,
                Pointer = ptr,
                Byte = b,
                Payload = null
            };
        }

        public static Operand FromLabel(string label, int line, bool ptr, bool b)
        {
            return new Operand
            {
                OperandType = OperandType.Label,
                Pointer = ptr,
                Byte = b,
                Label = label,
                Line = line,
                Payload = 0
            };
        }
    }
}
