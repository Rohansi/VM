using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace VM
{
    class Motherboard : Device
    {
        private VirtualMachine vm;
        private Debugger debugger;
        private Timer[] timers;
        private Random random;

        public Motherboard(VirtualMachine virtualMachine)
        {
            vm = virtualMachine;

            debugger = new Debugger(virtualMachine);
            random = new Random();

            timers = new Timer[4];
            for (var i = 0; i < timers.Length; i++)
            {
                timers[i] = new Timer();
            }
        }

        public override void Reset()
        {
            random = new Random();

            foreach (var t in timers)
            {
                t.Reset();
            }
        }

        public override void DataReceived(short port, short data)
        {
            switch (port)
            {
                case 3:
                    debugger.DataReceived(data);
                    break;
                case 9:
                    random = new Random(data);
                    break;
                case 10:
                case 11:
                case 12:
                case 13:
                    timers[port - 10].DataReceived(data);
                    break;
            }
        }

        public override short? DataRequested(short port)
        {
            switch (port)
            {
                case 3:
                    return debugger.DataRequested();
                case 9:
                    return (short)random.Next(short.MinValue, short.MaxValue);
                case 10:
                case 11:
                case 12:
                case 13:
                    return timers[port - 10].DataRequested();
            }

            return null;
        }

        private class Timer
        {
            private Stopwatch watch;
            private short target;

            public Timer()
            {
                target = 0;
                watch = new Stopwatch();
            }

            public void Reset()
            {
                target = 0;
            }

            public void DataReceived(short data)
            {
                watch.Restart();
                target = Math.Abs(data);
            }

            public short DataRequested()
            {
                return (short)Math.Max(target - watch.ElapsedMilliseconds, 0);
            }
        }
    }
}
