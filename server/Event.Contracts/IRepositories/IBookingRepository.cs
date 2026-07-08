using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface IBookingRepository : IGenericRepository<Booking>
    {
        Task<IEnumerable<Booking>> GetBookingsByUserIdAsync(int userId);
        Task<Booking?> GetBookingDetailsAsync(int bookingId);
        Task<IEnumerable<Booking>> GetBookingsByEventIdAsync(int eventId);
        Task<Booking?> GetBookingBySecretHashAsync(string secretHash);
        Task<IEnumerable<Booking>> GetExpiredBookingsAsync(System.DateTime cutoffTime);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
