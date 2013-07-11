using System;
using System.Text;

namespace VM
{
    class Operand
    {
        private readonly VirtualMachine machine;
        private int type;
        private bool pointer;
        private bool byteValue;
        private short payload;

        public Operand(VirtualMachine machine)
        {
            this.machine = machine;
        }

        public void Change(int operandType, bool isPointer, bool isByte, short payloadValue)
        {
            type = operandType;
            pointer = isPointer;
            byteValue = isByte;
            payload = payloadValue;
        }

        public short Get(bool resolvePtr = true)
        {
            short val = 0;
            if (type <= 0x0F)
                val = machine.Registers[type];
            if (type == 0x10)
                val = machine.IP;
            if (type == 0x11)
                val = machine.SP;
            if (type == 0x12)
                val = payload;
            if (type == 0x13)
                val = payload;

            if (resolvePtr && pointer)
                val = (short)((machine.Memory[val + 1] << 8) | machine.Memory[val]);

            // don't trim memory locations
            if (resolvePtr && byteValue)
                val &= 0xFF;

            return val;
        }

        public void Set(short value)
        {
            if (!pointer)
            {
                if (type <= 0x0F)
                    machine.Registers[type] = PreserveUpper(value, machine.Registers[type], byteValue);
                if (type == 0x10)
                    machine.IP = PreserveUpper(value, machine.IP, byteValue);
                if (type == 0x11)
                    machine.SP = PreserveUpper(value, machine.SP, byteValue);

                // ignore immediate sets
                return;
            }

            var val = (ushort)PreserveUpper(value, Get(), byteValue);
            var addr = Get(false);
            machine.Memory[addr] = (byte)(val & 0xFF);
            machine.Memory[addr + 1] = (byte)(val >> 8);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (byteValue)
                sb.Append("byte ");
            if (pointer)
                sb.Append("[");

            if (type <= 0x0F)
                sb.Append("R" + type.ToString("X"));
            if (type == 0x10)
                sb.Append("IP");
            if (type == 0x11)
                sb.Append("SP");
            if (type == 0x12)
                sb.Append(payload.ToString("G"));
            if (type == 0x13)
                sb.Append(payload.ToString("G"));

            if (pointer)
                sb.Append("]");
            
            return sb.ToString();
        }

        private static short PreserveUpper(short newValue, short originalValue, bool isByte)
        {
            if (!isByte)
                return newValue;

            return (short)((originalValue & 0xFF00) | (newValue & 0xFF));
        }
    }
}
