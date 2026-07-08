namespace Event.Business.Exceptions
{
    public class ValidationException : BaseBusinessException
    {
        public override int StatusCode => 400;

        public ValidationException(string message) : base(message)
        {
        }
    }
}
