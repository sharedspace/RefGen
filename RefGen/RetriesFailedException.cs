using System;
using System.Runtime.Serialization;

namespace RefGen
{
    [Serializable]
    public class RetriesFailedException : Exception
    {
        public RetriesFailedException()
        {
        }

        public RetriesFailedException(string message) : base(message)
        {
        }

        public RetriesFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RetriesFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}