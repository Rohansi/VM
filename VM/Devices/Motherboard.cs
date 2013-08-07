using System;
using System.Diagnostics;
using SFML.Graphics;
using Texter;

namespace VM.Devices
{
    class Motherboard : Device
    {
        private Debugger debugger;
        private Timer[] timers;
        private Random random;
        private TextDisplay display;
        private Color[] originalPalette;

        public Motherboard(VirtualMachine virtualMachine, TextDisplay textDisplay)
        {
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

        public override void Attach(VirtualMachine machine)
        {
            machine.RegisterPortInHandler(3, () => debugger.DataRequested());
            machine.RegisterPortInHandler(9, () => (short)random.Next(short.MinValue, short.MaxValue));
            machine.RegisterPortInHandler(10, () => timers[0].DataRequested());
            machine.RegisterPortInHandler(11, () => timers[1].DataRequested());
            machine.RegisterPortInHandler(12, () => timers[2].DataRequested());
            machine.RegisterPortInHandler(13, () => timers[3].DataRequested());

            machine.RegisterPortOutHandler(3, s => debugger.DataReceived(s));
            machine.RegisterPortOutHandler(9, s => random = new Random(s));
            machine.RegisterPortOutHandler(10, s => timers[0].DataReceived(s));
            machine.RegisterPortOutHandler(11, s => timers[1].DataReceived(s));
            machine.RegisterPortOutHandler(12, s => timers[2].DataReceived(s));
            machine.RegisterPortOutHandler(13, s => timers[3].DataReceived(s));

            machine.RegisterPortOutHandler(20, data =>
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
            });
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
