using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VM
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
            var originalIP = vm.IP;

            vm.IP = address;
            var previousIP = vm.IP;
            var failed = false;

            for (var i = 0; i < length; i++)
            {
                if (!failed)
                {
                    try
                    {
                        var instruction = new Instruction(vm);
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
