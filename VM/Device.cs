using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VM
{
    public interface IDevice
    {
        void Reset();
        void DataReceived(short port, short data);
        short? DataRequested(short port);
    }
}
