using System;

namespace Assembler
{
    class Label
    {
        public readonly string Name;
        public readonly int Index;
        public int Address;

        public Label(string name, int index)
        {
            Name = name;
            Index = index;
        }
    }
}
