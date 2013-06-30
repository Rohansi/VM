using System;

namespace VM
{
    interface IMemory
    {
        byte this[int i] { get; set; }
    }
}
