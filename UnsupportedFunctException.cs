using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SingleCycleMIPS
{
    public class UnsupportedFunctException : Exception
    {
        public UnsupportedFunctException()
        {
        }

        public UnsupportedFunctException(string? message) : base(message)
        {
        }

        public UnsupportedFunctException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected UnsupportedFunctException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
