using System.Collections.Generic;

namespace Assembler
{
    class Label
    {
	    public Dictionary<string, Label> Labels; 
        public readonly string Name;
        public readonly int Index;
        public int Address;

        public Label(string name, int index)
        {
			Labels = new Dictionary<string, Label>();
            Name = name;
            Index = index;
        }
    }
}
