using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SFML.Graphics;
using VM.Devices.Audio;
using VM.Devices.Audio.Generators;

namespace VM.Devices
{
    // very experimental device
    class Speaker : Device
    {
        private short devPort;
        private uint sampleRate;
        private SoundPlayer player;
        private SoundMixer mixer;
        private List<ISoundGenerator> generators;

        public Speaker(RenderWindow window, VirtualMachine virtualMachine, XElement config)
        {
            var errorMsg = "";

            try
            {
                errorMsg = "Bad Port";
                devPort = short.Parse(Util.ElementValue(config, "Port", null));

                errorMsg = "Bad SampleRate";
                sampleRate = uint.Parse(Util.ElementValue(config, "SampleRate", null));

                errorMsg = "Bad UpdateFrequency";
                var updateFreq = uint.Parse(Util.ElementValue(config, "UpdateFrequency", null));
                if (updateFreq == 0)
                    throw new Exception("UpdateFrequency must be above 0");

                errorMsg = "Init failed";
                player = new SoundPlayer(sampleRate, updateFreq);

                generators = new List<ISoundGenerator>();
                generators.Add(new Square());
                generators.Add(new Square());
                generators.Add(new Sine());
                generators.Add(new Noise());

                mixer = new SoundMixer();
                generators.ForEach(mixer.AddSource);

                Reset();

                player.Source = mixer;
                player.Play();
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Speaker: {0}", errorMsg), e);
            }
        }

        public override void Reset()
        {
            foreach (var generator in generators)
            {
                generator.Amplitude = 0;
                generator.Frequency = 250;
            }
        }

        public override void DataReceived(short port, short data)
        {
            if (port < devPort || port > devPort + 4)
                return;

            if (port == devPort)
            {
                // TODO: device status
                return;
            }
            
            if (port > devPort && port <= devPort + generators.Count)
            {
                var noteByte = (byte)((ushort)data & 255);
                var amplitudeByte = (byte)((ushort)data >> 8);

                var frequency = NoteToFrequency(noteByte);
                var amplitude = (double)amplitudeByte / byte.MaxValue;

                var generatorIndex = port - devPort - 1;
                generators[generatorIndex].Frequency = frequency;
                generators[generatorIndex].Amplitude = amplitude;
            }
        }

        public override short? DataRequested(short port)
        {
            if (port < devPort || port > devPort + 6)
                return null;
            return 0;
        }

        public override void Dispose()
        {
            // required for clean exit
            player.Dispose();
        }

        private static int NoteToFrequency(byte note)
        {
            return (int)(Math.Pow(2, ((double)note - 40) / 12) * 440);
        }
    }
}
