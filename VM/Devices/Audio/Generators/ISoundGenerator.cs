using System;

namespace VM.Devices.Audio.Generators
{
    interface ISoundGenerator : ISoundModule
    {
        double Amplitude { get; set; }
        int Frequency { get; set; }
    }
}
