using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using SFML.Graphics;
using KeyboardKey = SFML.Window.Keyboard.Key;

namespace VM.Devices
{
    class Keyboard : Device
    {
        private short devPort;
        private object keyStrokesLock = new object();
        private Queue<short> keyStrokes;

        public Keyboard(RenderWindow window, VirtualMachine virtualMachine, XElement config)
        {
            keyStrokes = new Queue<short>();

            var errorMsg = "";

            try
            {
                errorMsg = "Bad Port";
                devPort = short.Parse(Util.ElementValue(config, "Port", null));
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Keyboard: {0}", errorMsg), e);
            }

            window.TextEntered += (sender, args) => QueueKey(args.Unicode);
        }

        public override void Attach(VirtualMachine machine)
        {
            machine.RegisterPortInHandler(devPort, () =>
            {
                if (keyStrokes.Count == 0)
                    return 0;

                return keyStrokes.Dequeue();
            });
        }

        public override void Reset()
        {
            keyStrokes.Clear();
        }

        private void QueueKey(string key)
        {
            lock (keyStrokesLock)
            {
                while (keyStrokes.Count > 16)
                {
                    keyStrokes.Dequeue();
                }

                if (key == "\r")
                    key = "\n";

                var bytes = Encoding.GetEncoding(437).GetBytes(key);
                keyStrokes.Enqueue(bytes[0]);
            }
        }
    }
}
