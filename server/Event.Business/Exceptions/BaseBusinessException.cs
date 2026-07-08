using System;

namespace Event.Business.Exceptions
{
    public abstract class BaseBusinessException : Exception
    {
        public abstract int StatusCode { get; }

        protected BaseBusinessException(string message) : base(message)
        {
        }
    }
}
