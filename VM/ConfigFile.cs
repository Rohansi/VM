using System;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;
using SFML.Window;

namespace VM
{
    public class ConfigFile
    {
        [DefaultValue(1000)]
        public int Slow;

        [DefaultValue(5000)]
        public int Medium;

        [DefaultValue(10000)]
        public int Fast;

        [DefaultValue("out.bin")]
        public string DefaultFile;

        [DefaultValue(Keyboard.Key.F1)]
        public Keyboard.Key Pause;

        [DefaultValue(Keyboard.Key.F2)]
        public Keyboard.Key ChangeSpeed;

        [DefaultValue(Keyboard.Key.F3)]
        public Keyboard.Key Reload;

        [DefaultValue(true)]
        public bool Controller;

        [DefaultValue(Keyboard.Key.Up)]
        public Keyboard.Key ControllerUp;

        [DefaultValue(Keyboard.Key.Down)]
        public Keyboard.Key ControllerDown;

        [DefaultValue(Keyboard.Key.Left)]
        public Keyboard.Key ControllerLeft;

        [DefaultValue(Keyboard.Key.Right)]
        public Keyboard.Key ControllerRight;

        [DefaultValue(Keyboard.Key.A)]
        public Keyboard.Key ControllerA;

        [DefaultValue(Keyboard.Key.S)]
        public Keyboard.Key ControllerB;

        [DefaultValue(Keyboard.Key.D)]
        public Keyboard.Key ControllerC;

        public static ConfigFile Load(string fileName)
        {
            return JsonConvert.DeserializeObject<ConfigFile>(File.ReadAllText(fileName));
        }
    }
}
