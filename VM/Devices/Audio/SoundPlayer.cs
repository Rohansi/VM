using System;
using SFML.Audio;

namespace VM.Devices.Audio
{
    class SoundPlayer : SoundStream
    {
        public ISoundModule Source;

        private uint updateFreq;

        public SoundPlayer(uint rate, uint update)
        {
            updateFreq = update;
            Source = null;
            Initialize(1, rate);
        }

        protected override bool OnGetData(out short[] samples)
        {
            if (Source == null)
            {
                samples = null;
                return false;
            }

            samples = Source.GetData(SampleRate / updateFreq, SampleRate);
            return true;
        }

        protected override void OnSeek(TimeSpan timeOffset)
        {

        }
    }
}
