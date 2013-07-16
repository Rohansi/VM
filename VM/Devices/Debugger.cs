using System;
using System.Collections.Generic;

namespace VM.Devices
{
    class Debugger
    {
        private VirtualMachine vm;

        public Debugger(VirtualMachine virtualMachine)
        {
            vm = virtualMachine;
        }

        public void DataReceived(short data)
        {
            var enabling = data != 0;

            if (enabling)
                vm.Flags |= VirtualMachine.Flag.Trap;
            else
                vm.Flags &= ~VirtualMachine.Flag.Trap;
        }

        public short? DataRequested()
        {
            var enabled = vm.Flags.HasFlag(VirtualMachine.Flag.Trap);
            return (short)(enabled ? 1 : 0);
        }

        private List<DisassemblyLine> GenerateDisassembly(short address, short length)
        {
            var res = new List<DisassemblyLine>();
            var instruction = new Instruction(vm);
            var originalIP = vm.IP;
            var previousIP = vm.IP;
            var failed = false;

            vm.IP = address;

            for (var i = 0; i < length; i++)
            {
                if (!failed)
                {
                    try
                    {
                        instruction.Decode();
                        res.Add(new DisassemblyLine(previousIP, instruction.ToString()));
                    }
                    catch
                    {
                        vm.IP = previousIP;
                        failed = true;
                    }
                }

                if (failed && (vm.IP > 0 || vm.IP < 32000))
                {
                    res.Add(new DisassemblyLine(previousIP, vm.Memory[vm.IP++].ToString("X")));
                }

                previousIP = vm.IP;
            }

            vm.IP = originalIP;
            return res;
        }

        private class DisassemblyLine
        {
            public readonly short Address;
            public readonly string Disassembly;

            public DisassemblyLine(short address, string disassembly)
            {
                Address = address;
                Disassembly = disassembly;
            }
        }
    }
}
