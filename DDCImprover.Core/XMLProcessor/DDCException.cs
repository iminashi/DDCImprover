using System;
using System.Runtime.Serialization;

namespace DDCImprover.Core
{
    public class DDCException : Exception
    {
        public DDCException(string message) : base(message) { }

        public DDCException(string message, Exception innerException) : base(message, innerException) { }

        protected DDCException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public DDCException() { }
    }
}
