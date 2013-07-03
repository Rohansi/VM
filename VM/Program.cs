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
		const int Width = 200;
		const int Height = 81;
		const int CharWidth = 6;
		const int CharHeight = 8;

		public static ConfigFile Config;

		static RenderWindow window;
		static TextDisplay display;
		static VirtualMachine machine;
		static IMemory memory;
		static string error;

		static void Main(string[] args)
		{
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => DumpError((Exception)e.ExceptionObject);

			var speed = VmSpeed.Medium;
			var running = true;

			Config = ConfigFile.Load("VmConfig.xml");

			TextDisplay.Initialize(CharWidth, CharHeight);

			window = new RenderWindow(new VideoMode(Width * CharWidth, Height * CharHeight), "", Styles.Close);
			window.SetFramerateLimit(Config.Framerate);
			window.Closed += (sender, e) => window.Close();

			display = new TextDisplay(Width, Height);
			memory = new MemoryWrapper(display);
			machine = new VirtualMachine(memory);

            machine.Devices.Add(new Motherboard(machine, display));

            var devMap = new Dictionary<string, Type>()
            {
                { "Controller", typeof(Controller) },
                { "HardDrive", typeof(HardDrive) }
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
                machine.Devices.Add(device);
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
						while (stopwatch.Elapsed.TotalSeconds < (1.0 / Config.Framerate))
						{
							if (steps >= requestedSteps)
								break;

							machine.Step();
							steps++;
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

				display.DrawRectangle(0, Height - 1, Width, 1, Character.Create(' '));
				display.DrawText(0, Height - 1, statusString, Character.Create(foreground: 255));

				display.Draw(window, new Vector2f(0, 0));
				window.Display();
			}
		}

		static void Load(string fileName)
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

        static void DumpError(Exception e)
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
