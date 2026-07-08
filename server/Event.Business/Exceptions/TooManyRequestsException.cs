using System;

namespace Event.Business.Exceptions
{
    public class TooManyRequestsException : BaseBusinessException
    {
        public override int StatusCode => 429;

        public TooManyRequestsException(string message) : base(message)
        {
        }
    }
}
