using System.Threading.Tasks;

namespace Event.Contracts.IServices
{
    public interface IPaymentService
    {
        Task<(bool Success, string TransactionReference, string ErrorMessage)> CreateChargeAsync(decimal amount, string currency, string paymentMethodToken, string description);
        Task<(bool Success, string RefundReference, string ErrorMessage)> CreateRefundAsync(string transactionReference, decimal amount);
        Task<(bool Success, string TransferReference, string ErrorMessage)> CreatePayoutAsync(string destinationConnectAccountId, decimal amount, string currency);
        Task<(bool Success, string SessionId, string ClientSecret, System.DateTime CreatedAtUTC, string ErrorMessage)> CreateCheckoutSessionAsync(decimal amount, string currency, string itemName, string returnUrl);
    }
}
