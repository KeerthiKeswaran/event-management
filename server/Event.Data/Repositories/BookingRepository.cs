using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class BookingRepository : GenericRepository<Booking>, IBookingRepository
    {
        public BookingRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Booking>> GetBookingsByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .Include(b => b.Details)
                .Include(b => b.Payments)
                    .ThenInclude(p => p.Transaction)
                .Where(b => b.Attendee_Id == userId)
                .ToListAsync();
        }

        public async Task<Booking?> GetBookingDetailsAsync(int bookingId)
        {
            return await _dbSet
                .Include(b => b.Event)
                    .ThenInclude(e => e.TicketTiers)
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .Include(b => b.Attendee)
                .Include(b => b.Details)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Booking_Id == bookingId);
        }

        public async Task<IEnumerable<Booking>> GetBookingsByEventIdAsync(int eventId)
        {
            return await _dbSet
                .Include(b => b.Attendee)
                .Include(b => b.Details)
                .Include(b => b.Payments)
                    .ThenInclude(p => p.Transaction)
                .Where(b => b.Event_Id == eventId)
                .ToListAsync();
        }

        public async Task<Booking?> GetBookingBySecretHashAsync(string secretHash)
        {
            return await _dbSet
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .Include(b => b.Details)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Qr_Secret_Hash == secretHash);
        }

        public async Task<IEnumerable<Booking>> GetExpiredBookingsAsync(System.DateTime cutoffTime)
        {
            return await _dbSet
                .Include(b => b.Event)
                    .ThenInclude(e => e.TicketTiers)
                .Include(b => b.Details)
                .Where(b => b.Booking_Status == "Payment Pending" && b.Created_At <= cutoffTime)
                .ToListAsync();
        }

        public async Task BeginTransactionAsync()
        {
            await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.CurrentTransaction.CommitAsync();
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.CurrentTransaction.RollbackAsync();
            }
        }
    }
}
