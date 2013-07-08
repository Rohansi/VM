using System;
using System.Collections.Generic;

namespace VM
{
    class VirtualMachine
    {
        [Flags]
        public enum Flag
        {
            None = 0,
            CmpReset = 255, // cmp resets lower 8 bits

            Zero = 1 << 0,
            Equal = 1 << 1,
            Above = 1 << 2,
            Below = 1 << 3,

            Trap = 1 << 15
        }

        public readonly short[] Registers;
        public short IP;
        public short SP;
        public Flag Flags;

        public readonly Memory Memory;
        public List<Device> Devices;

        public VirtualMachine(Memory memory)
        {
            Registers = new short[16];
            Memory = memory;
            Devices = new List<Device>();
        }

        public void Reset()
        {
            for (var i = 0; i < Registers.Length; i++)
            {
                Registers[i] = 0;
            }

            IP = 0;
            SP = 31999;
            Flags = Flag.None;

            for (var i = 0; i < 32000; i++)
            {
                Memory[i] = 0;
            }

            foreach (var dev in Devices)
            {
                dev.Reset();
            }
        }

        public void Step(bool overrideTrap = false)
        {
            if (Flags.HasFlag(Flag.Trap) && !overrideTrap)
                return;

            var instruction = new Instruction(this);
            short result = 0;

            //Console.WriteLine("{0,-8} {1}", ip, instruction);

            switch (instruction.Type)
            {
                /* MATH */
                case Instructions.Set:
                    instruction.Left.Set(instruction.Right.Get());
                    break;
                case Instructions.Add:
                    result = (short)(instruction.Left.Get() + instruction.Right.Get());
                    instruction.Left.Set(result);
                    SetZero(result);
                    break;
                case Instructions.Sub:
                    result = (short)(instruction.Left.Get() - instruction.Right.Get());
                    instruction.Left.Set(result);
                    SetZero(result);
                    break;
                case Instructions.Mul:
                    result = (short)(instruction.Left.Get() * instruction.Right.Get());
                    instruction.Left.Set(result);
                    SetZero(result);
                    break;
                case Instructions.Div:
                    var divisor = instruction.Right.Get();
                    if (divisor == 0)
                        throw new VmException("Divide by zero");

                    result = (short)(instruction.Left.Get() / divisor);
                    instruction.Left.Set(result);
                    SetZero(result);
                    break;
                case Instructions.Mod:
                    var modDivisor = instruction.Right.Get();
                    if (modDivisor == 0)
                        throw new VmException("Divide by zero");

                    result = (short)(instruction.Left.Get() % modDivisor);
                    instruction.Left.Set(result);
                    SetZero(result);
                    break;
                case Instructions.Inc:
                    result = (short)(instruction.Left.Get() + 1);
                    instruction.Left.Set(result);
                    SetZero(result);
                    break;
                case Instructions.Dec:
                    result = (short)(instruction.Left.Get() - 1);
                    instruction.Left.Set(result);
                    SetZero(result);
                    break;

                /* BITWISE MATH */
                case Instructions.Not:
                    result = (short)(~instruction.Left.Get());
                    instruction.Left.Set(result);
                    SetZero(result);
                    break;
                case Instructions.And:
                    result = (short)(instruction.Left.Get() & instruction.Right.Get());
                    instruction.Left.Set(result);
                    SetZero(result);
                    break;
                case Instructions.Or:
                    result = (short)(instruction.Left.Get() | instruction.Right.Get());
                    instruction.Left.Set(result);
                    SetZero(result);
                    break;
                case Instructions.Xor:
                    result = (short)(instruction.Left.Get() ^ instruction.Right.Get());
                    instruction.Left.Set(result);
                    SetZero(result);
                    break;
                case Instructions.Shl:
                    result = (short)(instruction.Left.Get() << instruction.Right.Get());
                    instruction.Left.Set(result);
                    SetZero(result);
                    break;
                case Instructions.Shr:
                    result = (short)(instruction.Left.Get() >> instruction.Right.Get());
                    instruction.Left.Set(result);
                    SetZero(result);
                    break;

                /* STACK */
                case Instructions.Push:
                    Push(instruction.Left.Get());
                    break;
                case Instructions.Pop:
                    instruction.Left.Set(Pop());
                    break;

                case Instructions.Jmp:
                    IP = instruction.Left.Get();
                    break;
                case Instructions.Call:
                    Push(IP);
                    IP = instruction.Left.Get();
                    break;
                case Instructions.Ret:
                    IP = Pop();
                    break;

                case Instructions.In:
                    var inPort = instruction.Right.Get();
                    result = 0;

                    foreach (var dev in Devices)
                    {
                        var res = dev.DataRequested(inPort);
                        if (res.HasValue)
                        {
                            result = res.Value;
                            break;
                        }
                    }

                    instruction.Left.Set(result);
                    SetZero(result);
                    break;
                case Instructions.Out:
                    var outPort = instruction.Left.Get();
                    var outData = instruction.Right.Get();

                    foreach (var dev in Devices)
                    {
                        dev.DataReceived(outPort, outData);
                    }
                    break;

                case Instructions.Cmp:
                    var cmpValL = instruction.Left.Get();
                    var cmpValR = instruction.Right.Get();

                    Flags &= ~Flag.CmpReset;

                    if (cmpValL == 0)
                        Flags |= Flag.Zero; // can be used as a shorter zero check, no payload needed
                    if (cmpValL == cmpValR)
                        Flags |= Flag.Equal;
                    if (cmpValL > cmpValR)
                        Flags |= Flag.Above;
                    if (cmpValL < cmpValR)
                        Flags |= Flag.Below;

                    break;

                case Instructions.Jz:
                    if (Flags.HasFlag(Flag.Zero))
                        IP = instruction.Left.Get();
                    break;
                case Instructions.Jnz:
                    if (!Flags.HasFlag(Flag.Zero))
                        IP = instruction.Left.Get();
                    break;
                case Instructions.Je:
                    if (Flags.HasFlag(Flag.Equal))
                        IP = instruction.Left.Get();
                    break;
                case Instructions.Ja:
                    if (Flags.HasFlag(Flag.Above))
                        IP = instruction.Left.Get();
                    break;
                case Instructions.Jb:
                    if (Flags.HasFlag(Flag.Below))
                        IP = instruction.Left.Get();
                    break;
                case Instructions.Jae:
                    if (Flags.HasFlag(Flag.Above) || Flags.HasFlag(Flag.Equal))
                        IP = instruction.Left.Get();
                    break;
                case Instructions.Jbe:
                    if (Flags.HasFlag(Flag.Below) || Flags.HasFlag(Flag.Equal))
                        IP = instruction.Left.Get();
                    break;
                case Instructions.Jne:
                    if (!Flags.HasFlag(Flag.Equal))
                        IP = instruction.Left.Get();
                    break;
            }
        }

        private void Push(short value)
        {
            Memory[SP--] = (byte)(value >> 8);
            Memory[SP--] = (byte)(value & 0xFF);
        }

        private short Pop()
        {
            var value = (int)Memory[++SP];
            value |= Memory[++SP] << 8;
            return (short)value;
        }

        private void SetZero(short value)
        {
            Flags &= ~Flag.Zero;
            if (value == 0)
                Flags |= Flag.Zero;
        }
    }
}
