using System;

namespace VM
{
    public class VmException : Exception
    {
        public VmException(string message, Exception innerException = null)
            : base(message, innerException)
        {

        }
    }
}
