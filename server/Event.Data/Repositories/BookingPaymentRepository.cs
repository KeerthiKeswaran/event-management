using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class BookingPaymentRepository : GenericRepository<BookingPayment>, IBookingPaymentRepository
    {
        public BookingPaymentRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<BookingPayment>> GetPaymentsByBookingIdAsync(int bookingId)
        {
            return await _dbSet
                .Include(bp => bp.Transaction)
                .Where(bp => bp.Booking_Id == bookingId)
                .ToListAsync();
        }

        public async Task<BookingPayment?> GetSuccessPaymentByBookingIdAsync(int bookingId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(bp => bp.Booking_Id == bookingId && 
                                           (bp.Payment_Status == "Success" || bp.Payment_Status == "Refunded"));
        }

        public async Task<decimal> GetTotalCommissionAsync()
        {
            return await _dbSet
                .Where(p => p.Payment_Status == "Success")
                .SumAsync(p => p.Platform_Fee_Cut);
        }
    }
}
