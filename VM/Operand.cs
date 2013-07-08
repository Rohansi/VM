using System;

namespace VM
{
    class Operand
    {
        private readonly VirtualMachine machine;
        private int type;
        private bool ptr;
        private short payload;

        public Operand(VirtualMachine machine)
        {
            this.machine = machine;
        }

        public void Change(int type, bool ptr, short payload)
        {
            this.type = type;
            this.ptr = ptr;
            this.payload = payload;
        }

        public short Get(bool resolvePtr = true)
        {
            if (type == int.MaxValue)
                return 0;

            short val = 0;
            if (type < 16)
                val = machine.Registers[type];
            if (type == 16)
                val = machine.IP;
            if (type == 17)
                val = machine.SP;
            if (type == 18)
                val = payload;

            if (resolvePtr && ptr)
                val = (short)((machine.Memory[val + 1] << 8) | machine.Memory[val]);

            return val;
        }

        public void Set(short value)
        {
            if (type == int.MaxValue)
                return;

            if (!ptr)
            {
                if (type < 16)
                    machine.Registers[type] = value;
                if (type == 16)
                    machine.IP = value;
                if (type == 17)
                    machine.SP = value;

                return;
            }

            var val = (ushort)value;
            var addr = Get(false);
            machine.Memory[addr] = (byte)(val & 0xFF);
            machine.Memory[addr + 1] = (byte)(val >> 8);
        }

        public override string ToString()
        {
            var res = "";
            if (type < 16)
                res = "R" + type.ToString("X");
            if (type == 16)
                res = "IP";
            if (type == 17)
                res = "SP";
            if (type == 18)
                res = payload.ToString();
            if (ptr)
                res = "[" + res + "]";
            return res;
        }
    }
}
