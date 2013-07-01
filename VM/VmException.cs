using System;

namespace VM
{
    class VmException : Exception
    {
        public VmException(string message, Exception innerException = null)
            : base(message, innerException)
        {

        }
    }
}
