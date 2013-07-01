using System;

namespace VM
{
    abstract class Device
    {
        public abstract void Reset();
        public abstract void DataReceived(short port, short data);
        public abstract short? DataRequested(short port);
    }
}
