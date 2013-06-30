using System;
using Texter;

namespace VM
{
    class MemoryWrapper : IMemory
    {
        private TextRenderer _display;

        public byte this[int i]
        {
            get
            {
                if (i < 0 || i > 32000)
                    throw new VmException(string.Format("Memory read from {0}", i));

                var chI = i / 2;
                var x = chI % 200;
                var y = chI / 200;
                var ch = _display.Get(x, y);

                if ((i & 1) == 0)
                    return (byte)ch.Glyph;
                return (byte)ch.Foreground;
            }
            set
            {
                if (i < 0 || i > 32000)
                    throw new VmException(string.Format("Memory write to {0}", i));

                var chI = i / 2;
                var x = chI % 200;
                var y = chI / 200;

                if ((i & 1) == 0)
                    _display.Set(x, y, Character.Create(value));
                else
                    _display.Set(x, y, Character.Create(-1, value));
            }
        }

        public MemoryWrapper(TextRenderer display)
        {
            if (display == null)
                throw new ArgumentNullException("display");

            if (display.Width > 200 || display.Height < 80)
                throw new ArgumentException("display must be 200 wide and at least 80 tall");

            _display = display;
        }
    }
}
