using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class VenueRepository : GenericRepository<Venue>, IVenueRepository
    {
        public VenueRepository(EventDbContext context) : base(context)
        {
        }

        public override async Task<Venue?> GetByIdAsync(object id)
        {
            return await _context.Venues
                .Include(v => v.SeatCapacities)
                .Include(v => v.Region)
                .FirstOrDefaultAsync(v => v.Venue_Id == (int)id);
        }

        public async Task<bool> IsVenueOccupiedAsync(int venueId, DateTime dateTime)
        {
            return await _context.Events
                .AnyAsync(e => e.Venue_Id == venueId && 
                               (e.Status == "Live" || e.Status == "Activation Pending") && 
                               e.Date_Time <= dateTime && 
                               dateTime < e.Date_Time.AddHours((double)e.Duration_Hours));
        }

        public async Task<IEnumerable<Venue>> GetAllWithDetailsAsync()
        {
            return await _context.Venues
                .Include(v => v.Region)
                .Include(v => v.SeatCapacities)
                .ToListAsync();
        }

        public async Task AddSeatCapacityAsync(VenueSeatCapacity seatCapacity)
        {
            await _context.VenueSeatCapacities.AddAsync(seatCapacity);
            await _context.SaveChangesAsync();
        }
    }
}

