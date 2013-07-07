using System;
using System.Collections.Generic;

namespace VM.Devices.Audio.Generators
{
	class Noise : ISoundGenerator
    {
        public double Amplitude
        {
            get { return amplitude; }
            set
            {
                lock (dataLock)
                    amplitude = value;
            }
        }

        public int Frequency
        {
            get { return frequency; }
            set
            {
                lock (dataLock)
                    frequency = value;
            }
        }

        private object dataLock = new object();
        private double amplitude;
        private int frequency;
		int position;
		Random random = new Random();
		short randomValue;

        public Noise()
        {
            Amplitude = 1;
            Frequency = 250;
        }

		public short[] GetData(uint size, uint rate)
		{
            lock (dataLock)
            {
                var data = new short[size];

                if (Frequency != 0)
                {
                    var samplesPerData = Math.Max(1, rate / Frequency);
                    for (var i = 0; i < size; i++, position++)
                    {
                        if (position % samplesPerData == 0)
                        {
                            randomValue = (short)((random.NextDouble() * 2 - 1) * (short.MaxValue * Amplitude));
                        }

                        data[i] = randomValue;
                    }
                }

                return data;
            }
		}
	}
}
