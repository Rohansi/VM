using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using SFML.Window;

namespace VM
{
    class ConfigFile
    {
        public uint Framerate;
        public int Slow;
        public int Medium;
        public int Fast;
        public string DefaultFile;
        public Keyboard.Key Pause;
        public Keyboard.Key ChangeSpeed;
        public Keyboard.Key Reload;

        public List<XElement> Devices;

        public static ConfigFile Load(string fileName)
        {
            // has to be a better way to do this but for now its fine
            var errorMsg = "XML";

            try
            {
                var doc = XDocument.Load(fileName).Root;
                var res = new ConfigFile();

                if (doc == null)
                    throw new Exception("why is root null?");

                errorMsg = "Framerate";
                res.Framerate = uint.Parse(Util.ElementValue(doc, "Framerate", "60"));

                if (res.Framerate == 0)
                    throw new Exception("Framerate cannot be 0");

                errorMsg = "Slow";
                res.Slow = int.Parse(Util.ElementValue(doc, "Slow", "60"));

                errorMsg = "Medium";
                res.Medium = int.Parse(Util.ElementValue(doc, "Medium", "60"));

                errorMsg = "Fast";
                res.Fast = int.Parse(Util.ElementValue(doc, "Fast", "60"));

                errorMsg = "DefaultFile";
                res.DefaultFile = Util.ElementValue(doc, "DefaultFile", "out.bin");

                errorMsg = "Pause";
                res.Pause = Util.EnumParse<Keyboard.Key>(Util.ElementValue(doc, "Pause", "F1"));

                errorMsg = "ChangeSpeed";
                res.ChangeSpeed = Util.EnumParse<Keyboard.Key>(Util.ElementValue(doc, "ChangeSpeed", "F2"));

                errorMsg = "Reload";
                res.Reload = Util.EnumParse<Keyboard.Key>(Util.ElementValue(doc, "Reload", "F3"));

                // convert speeds to instructions/frame
                var fps = (int)res.Framerate;
                res.Slow /= fps;
                res.Medium /= fps;
                res.Fast /= fps;

                errorMsg = "Devices";
                res.Devices = new List<XElement>();
                var devices = doc.Element("Devices");
                if (devices != null)
                    res.Devices.AddRange(devices.Elements());

                return res;
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Could not read config: {0}", errorMsg), e);
            }
        }
    }
}
