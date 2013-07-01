using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.Window;

namespace VM
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

    class Controller : IDevice
    {
        private ControllerKeys state;
        
        public Dictionary<ControllerKeys, Keyboard.Key> KeyBindings; 

        public Controller(RenderWindow window)
        {
            KeyBindings = new Dictionary<ControllerKeys, Keyboard.Key>();

            state = ControllerKeys.Presence;

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

        public void Reset()
        {
            
        }

        public void DataReceived(short port, short data)
        {
            
        }

        public short? DataRequested(short port)
        {
            if (port != 100)
                return null;

            return (short)state;
        }
    }
}
