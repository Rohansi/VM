using System;
using System.Diagnostics;

namespace VM
{
    class Timer : IDevice
    {
        private short timerPort;
        private Stopwatch watch;
        private short target;

        public Timer(short port)
        {
            timerPort = port;
            target = 0;
            watch = new Stopwatch();
        }

        public void Reset()
        {
            target = 0;
        }

        public void DataReceived(short port, short data)
        {
            if (port != timerPort)
                return;

            watch.Restart();
            target = Math.Abs(data);
        }

        public short? DataRequested(short port)
        {
            if (port != timerPort)
                return null;
            
            return (short)Math.Max(target - watch.ElapsedMilliseconds, 0);
        }
    }
}
