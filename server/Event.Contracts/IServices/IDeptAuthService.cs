using System.Threading.Tasks;

namespace Event.Contracts.IServices
{
    public interface IDeptAuthService
    {
        Task<string?> LoginAdminAsync(string adminId, string password, string role);
        Task<string?> VerifyFinanceLoginOtpAsync(string adminId, string otp);
        Task<string> ResetAdminPasswordAsync(string email, string otp, string newPassword);
    }
}
