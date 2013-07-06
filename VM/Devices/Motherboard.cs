using System;
using System.Collections.Generic;
using System.Diagnostics;
using SFML.Graphics;
using Texter;

namespace VM.Devices
{
    class Motherboard : Device
    {
        private VirtualMachine machine;
        private Debugger debugger;
        private Timer[] timers;
        private Random random;
        private TextDisplay display;
        private Color[] originalPalette;

        public Motherboard(VirtualMachine virtualMachine, TextDisplay textDisplay)
        {
            machine = virtualMachine;
            display = textDisplay;

            debugger = new Debugger(virtualMachine);
            random = new Random();

            timers = new Timer[4];
            for (var i = 0; i < timers.Length; i++)
            {
                timers[i] = new Timer();
            }

            originalPalette = new Color[256];
            for (var i = 0; i < originalPalette.Length; i++)
            {
                originalPalette[i] = display.PaletteGet((byte)i);
            }
        }

        public override void Reset()
        {
            random = new Random();

            foreach (var t in timers)
            {
                t.Reset();
            }

            PaletteReset();
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
                case 20:
                {
                    if (data == 0)
                    {
                        PaletteReset();
                        return;
                    }

                    for (var i = 0; i < 256; i++)
                    {
                        var colorOffset = data + (i * 3);
                        var r = machine.Memory[colorOffset + 0];
                        var g = machine.Memory[colorOffset + 1];
                        var b = machine.Memory[colorOffset + 2];
                        display.PaletteSet((byte)i, new Color(r, g, b));
                    }

                    break;
                }
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

        private void PaletteReset()
        {
            for (var i = 0; i < originalPalette.Length; i++)
            {
                display.PaletteSet((byte)i, originalPalette[i]);
            }
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
