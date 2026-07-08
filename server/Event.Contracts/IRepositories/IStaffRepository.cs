using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models;

namespace Event.Contracts.IRepositories
{
    public interface IStaffRepository : IGenericRepository<Staff>
    {
        Task<int> GetAvailableStaffCountAsync(string regionId, DateTime dateTime);
        Task<IEnumerable<Staff>> GetAvailableStaffsAsync(string regionId, DateTime dateTime);
    }
}
