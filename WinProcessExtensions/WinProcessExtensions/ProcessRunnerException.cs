using System;
using System.Runtime.Serialization;

namespace WinProcessExtensions
{
    public class ProcessRunnerException : Exception
    {
        public ProcessRunnerException()
        {
        }

        public ProcessRunnerException(string message) : base(message)
        {
        }

        public ProcessRunnerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ProcessRunnerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}