using System.Runtime.Serialization;

namespace SingleCycleMIPS.Exceptions
{
    public class UnkownInstructionException : Exception
    {
        public UnkownInstructionException()
        {
        }

        public UnkownInstructionException(string? message) : base(message)
        {
        }

        public UnkownInstructionException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected UnkownInstructionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}