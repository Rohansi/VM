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
        private Square square1;
        private Square square2;
        private Sine sine;
        private Noise noise;

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

                square1 = new Square();
                square2 = new Square();
                sine = new Sine();
                noise = new Noise();

                mixer = new SoundMixer();
                mixer.AddSource(square1);
                mixer.AddSource(square2);
                mixer.AddSource(sine);
                mixer.AddSource(noise);

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
            square1.Amplitude = 0;
            square1.Frequency = 250;

            square2.Amplitude = 0;
            square2.Frequency = 250;

            sine.Amplitude = 0;
            sine.Frequency = 250;

            noise.Amplitude = 0;
            noise.Frequency = 250;
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

            var noteByte = (byte)((ushort)data & 255);
            var amplitudeByte = (byte)((ushort)data >> 8);

            var frequency = NoteToFrequency(noteByte);
            var amplitude = (double)amplitudeByte / byte.MaxValue;
            
            if (port == devPort + 1)
            {
                square1.Frequency = frequency;
                square1.Amplitude = amplitude;
                return;
            }

            if (port == devPort + 2)
            {
                square2.Frequency = frequency;
                square2.Amplitude = amplitude;
                return;
            }

            if (port == devPort + 3)
            {
                sine.Frequency = frequency;
                sine.Amplitude = amplitude;
                return;
            }

            if (port == devPort + 4)
            {
                noise.Frequency = frequency;
                noise.Amplitude = amplitude;
                return;
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
