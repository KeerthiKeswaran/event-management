namespace Event.Business.Exceptions
{
    public class UnauthorizedException : BaseBusinessException
    {
        public override int StatusCode => 401;

        public UnauthorizedException(string message) : base(message)
        {
        }
    }
}
