using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(EventDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByUserIdAsync(int userId)
        {
            string attendeePrefix = $"Attendee_User_{userId}";
            string organizerPrefix = $"Organizer_User_{userId}";

            return await _dbSet
                .Where(t => t.Sender_Id == attendeePrefix || 
                            t.Receiver_Id == attendeePrefix ||
                            t.Sender_Id == organizerPrefix ||
                            t.Receiver_Id == organizerPrefix)
                .ToListAsync();
        }

        public async Task<Transaction?> GetTransactionByReferenceAsync(string reference)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.Transaction_Reference == reference);
        }

        public async Task<Transaction?> GetPendingBookingTransactionAsync(int bookingId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.Related_Id == bookingId && t.Transaction_Type == "BookingPayment" && t.Status == "Pending");
        }

        public async Task<Transaction?> GetSuccessBookingTransactionAsync(int bookingId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.Related_Id == bookingId && t.Transaction_Type == "BookingPayment" && t.Status == "Success");
        }

        public async Task<Transaction?> GetPendingOrganizerUpfrontTransactionAsync(int eventId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.Related_Id == eventId && t.Transaction_Type == "OrganizerUpfrontPayment" && t.Status == "Pending");
        }

        public async Task<Transaction?> GetSuccessOrganizerUpfrontTransactionAsync(int eventId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(t => t.Related_Id == eventId && t.Transaction_Type == "OrganizerUpfrontPayment" && t.Status == "Success");
        }

        public async Task<decimal> GetGrossRevenueAsync()
        {
            return await _dbSet
                .Where(t => t.Status == "Success" && 
                            (t.Transaction_Type == "BookingPayment" || 
                             t.Transaction_Type == "OrganizerUpfrontPayment"))
                .SumAsync(t => t.Amount);
        }

        public async Task<System.Collections.Generic.IEnumerable<Transaction>> GetRecentTransactionsAsync(int count)
        {
            return await _dbSet
                .OrderByDescending(t => t.Created_At)
                .Take(count)
                .ToListAsync();
        }

        public async Task<PagedResult<Transaction>> GetTransactionRangeAsync(int from, int to)
        {
            if (from < 1) from = 1;
            if (to < from) to = from;

            var query = _dbSet.OrderByDescending(t => t.Created_At);
            int totalCount = await query.CountAsync();

            int skip = from - 1;
            int take = to - from + 1;

            var items = await query
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return new PagedResult<Transaction>(items, totalCount, from, take);
        }

        public async Task<PagedResult<Transaction>> GetTransactionsPagedAsync(
            string? keyword,
            string? transactionType,
            string? status,
            DateTime? startDate,
            DateTime? endDate,
            string? sortBy,
            int page,
            int size)
        {
            var query = _dbSet.AsQueryable();

            // Filter by keyword (searches reference, sender ID, receiver ID, or remarks)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var lowerKeyword = keyword.ToLower();
                query = query.Where(t => 
                    (t.Transaction_Reference != null && t.Transaction_Reference.ToLower().Contains(lowerKeyword)) ||
                    t.Sender_Id.ToLower().Contains(lowerKeyword) ||
                    t.Receiver_Id.ToLower().Contains(lowerKeyword) ||
                    (t.Remarks != null && t.Remarks.ToLower().Contains(lowerKeyword))
                );
            }

            // Filter by transaction type
            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                var lowerType = transactionType.ToLower();
                query = query.Where(t => t.Transaction_Type.ToLower() == lowerType);
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                var lowerStatus = status.ToLower();
                query = query.Where(t => t.Status.ToLower() == lowerStatus);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                var utcStart = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                query = query.Where(t => t.Created_At >= utcStart);
            }
            if (endDate.HasValue)
            {
                var utcEnd = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
                query = query.Where(t => t.Created_At <= utcEnd);
            }

            // Sort
            if (string.Equals(sortBy, "date_asc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderBy(t => t.Created_At);
            }
            else if (string.Equals(sortBy, "amount_asc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderBy(t => t.Amount);
            }
            else if (string.Equals(sortBy, "amount_desc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderByDescending(t => t.Amount);
            }
            else if (string.Equals(sortBy, "status_asc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderBy(t => t.Status);
            }
            else if (string.Equals(sortBy, "status_desc", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderByDescending(t => t.Status);
            }
            else
            {
                // Default: latest transactions first (date_desc)
                query = query.OrderByDescending(t => t.Created_At);
            }

            int totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            return new PagedResult<Transaction>(items, totalCount, page, size);
        }
    }
}
