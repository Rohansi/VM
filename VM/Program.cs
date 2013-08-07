using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using SFML.Graphics;
using SFML.Window;
using Texter;

namespace VM
{
    public enum VmSpeed
    {
        Slow, Medium, Fast, Unlimited
    }

    class Program
    {
        private const int CharWidth = 6;
        private const int CharHeight = 8;

        public static ConfigFile Config;

        private static RenderWindow window;
        private static TextDisplay display;
        private static VirtualMachine machine;
        private static Memory memory;

        private static TextDisplay statusDisplay;
        private static string error;

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => DumpError((Exception)e.ExceptionObject);

            var speed = VmSpeed.Medium;
            var running = true;

            Config = ConfigFile.Load("VmConfig.xml");

            TextDisplay.Initialize(CharWidth, CharHeight);

            window = new RenderWindow(new VideoMode(200 * CharWidth, 81 * CharHeight), "", Styles.Close);
            window.SetFramerateLimit(Config.Framerate);
            window.Closed += (sender, e) => window.Close();

            display = new TextDisplay(200, 80);
            memory = new Memory();
            machine = new VirtualMachine(memory);

            statusDisplay = new TextDisplay(200, 1);

            machine.AttachDevice(new Devices.Motherboard(machine, display));

            var devMap = new Dictionary<string, Type>()
            {
                { "Controller", typeof(Devices.Controller) },
                { "Keyboard", typeof(Devices.Keyboard) },
                { "HardDrive", typeof(Devices.HardDrive) },
                { "Speaker", typeof(Devices.Speaker) },
            };

            foreach (var devConfig in Config.Devices)
            {
                var devName = devConfig.Name.ToString();
                if (!devMap.ContainsKey(devName))
                    continue;

                var devEnabledAttr = devConfig.Attribute("Enabled");
                var devEnabled = devEnabledAttr == null || devEnabledAttr.Value.ToLower() == "true";

                if (!devEnabled)
                    continue;

                var device = (Device)Activator.CreateInstance(devMap[devName], window, machine, devConfig);
                machine.AttachDevice(device);
            }

            var file = Config.DefaultFile;
            if (args.Length > 0)
                file = args[0];

            if (error == null)
                Load(file);

            window.KeyPressed += (sender, eventArgs) =>
            {
                if (eventArgs.Code == Config.Pause)
                    running = !running;

                if (eventArgs.Code == Config.ChangeSpeed)
                    speed = (VmSpeed)(((int)speed + 1) % 4);

                if (eventArgs.Code == Config.Reload)
                    Load(file);
            };

            var previousSteps = new LinkedList<int>();

            var ipX = 0;
            var ipY = 0;
            var spX = 0;
            var spY = 0;

            while (window.IsOpen())
            {
                window.DispatchEvents();

                #region Stepping
                var requestedSteps = 0;
                var steps = 0;

                switch (speed)
                {
                    case VmSpeed.Slow:
                        requestedSteps = Config.Slow;
                        break;
                    case VmSpeed.Medium:
                        requestedSteps = Config.Medium;
                        break;
                    case VmSpeed.Fast:
                        requestedSteps = Config.Fast;
                        break;
                    case VmSpeed.Unlimited:
                        requestedSteps = int.MaxValue; // "unlimited"
                        break;
                }

                if (running && error == null)
                {
                    try
                    {
                        var stopwatch = Stopwatch.StartNew();
                        while (stopwatch.Elapsed.TotalSeconds < (0.95 / Config.Framerate))
                        {
                            if (steps >= requestedSteps)
                                break;

                            for (var i = 0; i < 100; i++, steps++)
                            {
                                machine.Step();
                            }
                        }
                    }
                    catch (VmException e)
                    {
                        error = e.Message;
                    }
                }

                previousSteps.AddLast(steps);
                if (previousSteps.Count > Config.Framerate)
                    previousSteps.RemoveFirst();
                #endregion

                display.DrawImage(0, 0, memory);

                #region IP and SP markers
                display.Set(ipX, ipY, Character.Create(background: 0));
                display.Set(spX, spY, Character.Create(background: 0));

                var ip = machine.IP / 2;
                ipX = ip % 200;
                ipY = ip / 200;
                var sp = machine.SP / 2;
                spX = sp % 200;
                spY = sp / 200;

                if (ipY < 80)
                    display.Set(ipX, ipY, Character.Create(background: 255));
                if (spY < 80)
                    display.Set(spX, spY, Character.Create(background: 255));
                #endregion

                var statusString = "Paused";

                if (error != null)
                    statusString = string.Format("ERROR: {0}", error);
                else if (machine.Flags.HasFlag(VirtualMachine.Flag.Trap))
                    statusString = "Trapped";
                else if (running)
                    statusString = string.Format("Running: {0} instructions per second ({1})", previousSteps.Sum(), speed);

                statusDisplay.Clear(Character.Create(' '));
                statusDisplay.DrawText(0, 0, statusString, Character.Create(foreground: 255));

                display.Draw(window, new Vector2f(0, 0));
                statusDisplay.Draw(window, new Vector2f(0, 80 * CharHeight));

                window.Display();
            }

            machine.Dispose();
        }

        private static void Load(string fileName)
        {
            try
            {
                var data = File.ReadAllBytes(fileName);

                machine.Reset();
                for (var i = 0; i < data.Length; i++)
                {
                    memory[i] = data[i];
                }

                error = null;
            }
            catch
            {
                error = string.Format("Failed to load {0}", fileName);
            }
        }

        private static void DumpError(Exception e)
        {
            // hope this works
            while (e is TypeInitializationException || e is TargetInvocationException)
            {
                e = e.InnerException;
            }

            File.WriteAllText("VmError.txt", e.ToString());
        }
    }
}
