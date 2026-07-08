using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        Task<IEnumerable<Transaction>> GetTransactionsByUserIdAsync(int userId);
        Task<Transaction?> GetTransactionByReferenceAsync(string reference);
        Task<Transaction?> GetPendingBookingTransactionAsync(int bookingId);
        Task<Transaction?> GetSuccessBookingTransactionAsync(int bookingId);
        Task<Transaction?> GetPendingOrganizerUpfrontTransactionAsync(int eventId);
        Task<Transaction?> GetSuccessOrganizerUpfrontTransactionAsync(int eventId);
        Task<decimal> GetGrossRevenueAsync();
        Task<System.Collections.Generic.IEnumerable<Transaction>> GetRecentTransactionsAsync(int count);
        Task<PagedResult<Transaction>> GetTransactionRangeAsync(int from, int to);
        Task<PagedResult<Transaction>> GetTransactionsPagedAsync(
            string? keyword,
            string? transactionType,
            string? status,
            DateTime? startDate,
            DateTime? endDate,
            string? sortBy,
            int page,
            int size);
    }
}
