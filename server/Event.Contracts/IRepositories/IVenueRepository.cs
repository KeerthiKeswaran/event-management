using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface IVenueRepository : IGenericRepository<Venue>
    {
        Task<bool> IsVenueOccupiedAsync(int venueId, DateTime dateTime);

        Task<IEnumerable<Venue>> GetAllWithDetailsAsync();

        Task AddSeatCapacityAsync(VenueSeatCapacity seatCapacity);
    }
}
