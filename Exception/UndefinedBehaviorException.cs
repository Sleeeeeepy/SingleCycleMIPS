using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SingleCycleMIPS.Exceptions
{
    public class UndefinedBehaviorException : Exception
    {
        public UndefinedBehaviorException()
        {
        }

        public UndefinedBehaviorException(string? message) : base(message)
        {
        }

        public UndefinedBehaviorException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected UndefinedBehaviorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
