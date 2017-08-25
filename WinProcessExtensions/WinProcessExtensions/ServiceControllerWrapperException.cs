using System;
using System.Runtime.Serialization;

namespace WinProcessExtensions
{
    public class ServiceControllerWrapperException : Exception
    {
        public ServiceControllerWrapperException()
        {
        }

        public ServiceControllerWrapperException(string message) : base(message)
        {
        }

        public ServiceControllerWrapperException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ServiceControllerWrapperException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}