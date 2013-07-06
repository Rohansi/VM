using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VM.Devices.Audio
{
	interface ISoundModule
	{
		short[] GetData(uint size, uint rate);
	}
}
