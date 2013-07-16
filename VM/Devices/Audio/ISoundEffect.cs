using System;

namespace VM.Devices.Audio
{
    interface ISoundEffect : ISoundModule
    {
        void AddSource(ISoundModule source);
        void RemoveSource(ISoundModule source);
    }
}
