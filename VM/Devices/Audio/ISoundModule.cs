using System;

namespace VM.Devices.Audio
{
    interface ISoundModule
    {
        short[] GetData(uint size, uint rate);
    }
}
