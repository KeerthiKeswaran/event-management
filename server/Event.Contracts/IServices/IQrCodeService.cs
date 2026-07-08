using System.Threading.Tasks;

namespace Event.Contracts.IServices
{
    public interface IQrCodeService
    {
        Task<byte[]> GenerateQrCodeAsync(string text);
    }
}
