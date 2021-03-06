﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SFML.Graphics;
using KeyboardKey = SFML.Window.Keyboard.Key;

namespace VM.Devices
{
    [Flags]
    public enum ControllerKeys : short
    {
        Up = 1 << 0,
        Down = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        A = 1 << 4,
        B = 1 << 5,
        C = 1 << 6,

        Presence = 1 << 7 // presence of the device
    }

    class Controller : Device
    {
        private short devPort;
        private ControllerKeys state;

        public Dictionary<ControllerKeys, KeyboardKey> KeyBindings;

        public Controller(RenderWindow window, VirtualMachine virtualMachine, XElement config)
        {
            state = ControllerKeys.Presence;
            KeyBindings = new Dictionary<ControllerKeys, KeyboardKey>();

            var errorMsg = "";

            try
            {
                errorMsg = "Bad Port";
                devPort = short.Parse(Util.ElementValue(config, "Port", null));

                errorMsg = "Bad Key";
                KeyBindings[ControllerKeys.Up] = Util.EnumParse<KeyboardKey>(Util.ElementValue(config, "Up", "Up"));
                KeyBindings[ControllerKeys.Down] = Util.EnumParse<KeyboardKey>(Util.ElementValue(config, "Down", "Down"));
                KeyBindings[ControllerKeys.Left] = Util.EnumParse<KeyboardKey>(Util.ElementValue(config, "Left", "Left"));
                KeyBindings[ControllerKeys.Right] = Util.EnumParse<KeyboardKey>(Util.ElementValue(config, "Right", "Right"));
                KeyBindings[ControllerKeys.A] = Util.EnumParse<KeyboardKey>(Util.ElementValue(config, "A", "A"));
                KeyBindings[ControllerKeys.B] = Util.EnumParse<KeyboardKey>(Util.ElementValue(config, "B", "S"));
                KeyBindings[ControllerKeys.C] = Util.EnumParse<KeyboardKey>(Util.ElementValue(config, "C", "D"));
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Controller: {0}", errorMsg), e);
            }

            window.KeyPressed += (sender, e) =>
            {
                foreach (var binding in KeyBindings)
                {
                    if (e.Code == binding.Value)
                    {
                        if (!state.HasFlag(binding.Key))
                            state |= binding.Key;
                    }
                }
            };

            window.KeyReleased += (sender, e) =>
            {
                foreach (var binding in KeyBindings)
                {
                    if (e.Code == binding.Value)
                    {
                        if (state.HasFlag(binding.Key))
                            state &= ~binding.Key;
                    }
                }
            };
        }

        public override void Attach(VirtualMachine machine)
        {
            machine.RegisterPortInHandler(devPort, () => (short)state);
        }

        public override void Reset()
        {
            state = ControllerKeys.Presence;
        }
    }
}
