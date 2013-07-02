using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace VM
{
    class Motherboard : Device
    {
        private const int TimerCount = 4;

        private VirtualMachine vm;
        private Timer[] timers;
        private Random random;

        public Motherboard(VirtualMachine virtualMachine)
        {
            vm = virtualMachine;

            random = new Random();

            timers = new Timer[TimerCount];
            for (var i = 0; i < TimerCount; i++)
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
            if (port == 9)
            {
                random = new Random(data);
            }

            if (port >= 10 && port < 10 + TimerCount)
            {
                timers[port - 10].DataReceived(data);
            }
        }

        public override short? DataRequested(short port)
        {
            if (port == 9)
            {
                return (short)random.Next(short.MinValue, short.MaxValue);
            }

            if (port >= 10 && port < 10 + TimerCount)
            {
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
