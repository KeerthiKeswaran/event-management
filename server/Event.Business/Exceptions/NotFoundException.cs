namespace Event.Business.Exceptions
{
    public class NotFoundException : BaseBusinessException
    {
        public override int StatusCode => 404;

        public NotFoundException(string message) : base(message)
        {
        }
    }
}
