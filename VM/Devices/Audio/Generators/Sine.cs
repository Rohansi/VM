﻿using System;

namespace VM.Devices.Audio.Generators
{
    class Sine : ISoundGenerator
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
        private double position;
        private int oldFrequency;

        public Sine()
        {
            Amplitude = 1;
            Frequency = 250;
        }

        public short[] GetData(uint size, uint rate)
        {
            lock (dataLock)
            {
                var data = new short[size];

                if (Frequency != oldFrequency)
                {
                    position *= (double)oldFrequency / Frequency;
                    oldFrequency = Frequency;
                }

                for (var i = 0; i < size; i++)
                {
                    data[i] = (short)(Amplitude * Math.Sin(position * Frequency) * short.MaxValue);
                    position += Math.PI * 2 / rate;
                }

                return data;
            }
        }
    }
}
