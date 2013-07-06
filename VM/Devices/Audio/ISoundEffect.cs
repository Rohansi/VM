using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VM.Devices.Audio
{
	interface ISoundEffect : ISoundModule
	{
		void AddSource(ISoundModule source);
		void RemoveSource(ISoundModule source);
	}
}
