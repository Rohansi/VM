using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VM.Devices.Audio.Generators
{
    interface ISoundGenerator : ISoundModule
    {
        double Amplitude { get; set; }
        int Frequency { get; set; }
    }
}
