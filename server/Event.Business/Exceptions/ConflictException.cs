namespace Event.Business.Exceptions
{
    public class ConflictException : BaseBusinessException
    {
        public override int StatusCode => 409;

        public ConflictException(string message) : base(message)
        {
        }
    }
}
