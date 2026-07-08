using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface IBookingPaymentRepository : IGenericRepository<BookingPayment>
    {
        Task<IEnumerable<BookingPayment>> GetPaymentsByBookingIdAsync(int bookingId);
        Task<BookingPayment?> GetSuccessPaymentByBookingIdAsync(int bookingId);
        Task<decimal> GetTotalCommissionAsync();
    }
}
