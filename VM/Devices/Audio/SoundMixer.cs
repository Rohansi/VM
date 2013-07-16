using System;
using System.Collections.Generic;

namespace VM.Devices.Audio
{
    class SoundMixer : ISoundEffect
    {
        List<ISoundModule> inputs = new List<ISoundModule>();

        // TODO: may want to make this more thread safe
        // TODO: i am 100% sure this is wrong.
        public short[] GetData(uint size, uint rate)
        {
            var data = new List<short[]>();
            foreach (var module in inputs)
            {
                data.Add(module.GetData(size, rate));
            }

            var result = new short[size];
            for (var i = 0; i < size; i++)
            {
                var sum = 0;
                for (var j = 0; j < data.Count; j++)
                {
                    sum += data[j][i];
                }

                result[i] = (short)(sum / data.Count);
            }

            return result;
        }

        public void AddSource(ISoundModule source)
        {
            inputs.Add(source);
        }

        public void RemoveSource(ISoundModule source)
        {
            inputs.Remove(source);
        }
    }
}
