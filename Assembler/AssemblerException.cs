using System;

namespace Assembler
{
    public class AssemblerException : Exception
    {
        public AssemblerException(string message, Exception innerException = null)
            : base(message, innerException)
        {

        }
    }
}
