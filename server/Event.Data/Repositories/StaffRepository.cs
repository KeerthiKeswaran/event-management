using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Event.Models;
using Event.Data.Contexts;
using Event.Contracts.IRepositories;

namespace Event.Data.Repositories
{
    public class StaffRepository : GenericRepository<Staff>, IStaffRepository
    {
        public StaffRepository(EventDbContext context) : base(context)
        {
        }

        public override async Task<IEnumerable<Staff>> GetAllAsync()
        {
            return await _dbSet
                .Include(s => s.Region)
                .ToListAsync();
        }

        public async Task<int> GetAvailableStaffCountAsync(string regionId, DateTime dateTime)
        {
            var allocatedStaffIds = _context.EventStaffAllocations
                .Where(esa => esa.Event.Status == "Live" &&
                              esa.Event.Date_Time <= dateTime &&
                              dateTime < esa.Event.Date_Time.AddHours((double)esa.Event.Duration_Hours))
                .Select(esa => esa.Employee_ID);

            return await _dbSet
                .CountAsync(s => s.Region_Id == regionId && 
                                 !s.IsAllocated && 
                                 !allocatedStaffIds.Contains(s.Employee_ID));
        }

        public async Task<IEnumerable<Staff>> GetAvailableStaffsAsync(string regionId, DateTime dateTime)
        {
            var allocatedStaffIds = _context.EventStaffAllocations
                .Where(esa => esa.Event.Status == "Live" &&
                              esa.Event.Date_Time <= dateTime &&
                              dateTime < esa.Event.Date_Time.AddHours((double)esa.Event.Duration_Hours))
                .Select(esa => esa.Employee_ID);

            return await _dbSet
                .Where(s => s.Region_Id == regionId && 
                            !s.IsAllocated && 
                            !allocatedStaffIds.Contains(s.Employee_ID))
                .ToListAsync();
        }
    }
}
