using System;

namespace VM
{
    abstract class Device : IDisposable
    {
        public abstract void Attach(VirtualMachine machine);
        public abstract void Reset();

        public virtual void Dispose()
        {

        }
    }
}
