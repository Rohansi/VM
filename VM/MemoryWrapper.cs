using System;
using Texter;

namespace VM
{
    class MemoryWrapper : TextRenderer, IMemory
    {
        private byte[] memory;

        public byte this[int i]
        {
            get
            {
                if (i < 0 || i > 32000)
                    throw new VmException(string.Format("Memory read from {0}", i));

                return memory[i];
            }
            set
            {
                if (i < 0 || i > 32000)
                    throw new VmException(string.Format("Memory write to {0}", i));

                memory[i] = value;
            }
        }

        public MemoryWrapper()
        {
            memory = new byte[32000];

            Width = 200;
            Height = 80;
        }

        public override void Set(int x, int y, Character character, bool blend = true)
        {
            throw new NotImplementedException();
        }

        public override Character Get(int x, int y)
        {
            var cell = (y * Width + x) * 2;
            return Character.Create(memory[cell], memory[cell + 1], 0);
        }
    }
}
