using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Models;
using Event.Data.Repositories;

namespace Event.Data.Tests.RepositoryTests
{
    [TestFixture]
    public class StaffRepositoryTests : RepositoryTestBase
    {
        private StaffRepository? _repository;
        private Seed.DbSeeder? _seeder;
        private const string Repo = "StaffRepository";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _repository = new StaffRepository(Context);
            _seeder = new Seed.DbSeeder(Context);
        }

        private Region CreateMockRegion(string regionId)
        {
            return new Region { Region_Id = regionId, No_Of_Staffs = 2 };
        }

        private Staff CreateMockStaff(int employeeId, string regionId)
        {
            return new Staff { Employee_ID = employeeId, Region_Id = regionId, IsAllocated = false };
        }

        private Event.Models.Event CreateMockEvent(int organizerId, DateTime dateTime, decimal duration)
        {
            return new Event.Models.Event
            {
                Organizer_Id = organizerId,
                Event_Type = "Physical",
                Title = "Staffed Event",
                Date_Time = dateTime,
                Duration_Hours = duration,
                Status = "Live"
            };
        }

        private async Task<(string regionId, User organizer)> SeedDependenciesAsync()
        {
            string uniqueRegionId = $"REG_{new Random().Next(1000, 9999)}";
            var region = CreateMockRegion(uniqueRegionId);
            Context.Regions.Add(region);

            var organizer = await _seeder.SeedUserAsync("Organizer");
            await Context.SaveChangesAsync();

            return (uniqueRegionId, organizer);
        }
        #endregion

        #region Create Tests
        [Test]
        public async Task Test_AddAsync()
        {
            var deps = await SeedDependenciesAsync();
            int uniqueEmpId = new Random().Next(1000, 5000);
            var staff = CreateMockStaff(uniqueEmpId, deps.regionId);

            try
            {
                await _repository.AddAsync(staff);
                Assert.That(staff.Employee_ID, Is.GreaterThan(0));
                LogTestDetail(Repo, "AddAsync", "Create new staff member", staff, staff, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddAsync", "Create new staff member", staff, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Read Tests
        [Test]
        public async Task Test_GetByIdAsync()
        {
            var deps = await SeedDependenciesAsync();
            int uniqueEmpId = new Random().Next(1000, 5000);
            var staff = CreateMockStaff(uniqueEmpId, deps.regionId);
            await _repository.AddAsync(staff);

            try
            {
                var fetched = await _repository.GetByIdAsync(uniqueEmpId);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Region_Id, Is.EqualTo(deps.regionId));
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve staff by employee ID", uniqueEmpId, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve staff by employee ID", uniqueEmpId, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetAvailableStaffCountAsync()
        {
            var deps = await SeedDependenciesAsync();
            int uniqueEmpId1 = new Random().Next(1000, 5000);
            int uniqueEmpId2 = uniqueEmpId1 + 1;
            var staff1 = CreateMockStaff(uniqueEmpId1, deps.regionId);
            var staff2 = CreateMockStaff(uniqueEmpId2, deps.regionId);
            await _repository.AddAsync(staff1);
            await _repository.AddAsync(staff2);

            var checkTime = DateTime.UtcNow.Date.AddHours(10);

            try
            {
                var initialCount = await _repository.GetAvailableStaffCountAsync(deps.regionId, checkTime);
                Assert.That(initialCount, Is.EqualTo(2));
                LogTestDetail(Repo, "GetAvailableStaffCountAsync", "Retrieve available staff count initially", 
                    new { RegionId = deps.regionId, Time = checkTime }, initialCount, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetAvailableStaffCountAsync", "Retrieve available staff count initially", 
                    new { RegionId = deps.regionId, Time = checkTime }, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetAvailableStaffsAsync()
        {
            var deps = await SeedDependenciesAsync();
            int uniqueEmpId1 = new Random().Next(1000, 5000);
            int uniqueEmpId2 = uniqueEmpId1 + 1;
            var staff1 = CreateMockStaff(uniqueEmpId1, deps.regionId);
            var staff2 = CreateMockStaff(uniqueEmpId2, deps.regionId);
            await _repository.AddAsync(staff1);
            await _repository.AddAsync(staff2);

            var checkTime = DateTime.UtcNow.Date.AddHours(10);

            try
            {
                var initialList = await _repository.GetAvailableStaffsAsync(deps.regionId, checkTime);
                Assert.That(initialList.Count(), Is.EqualTo(2));
                LogTestDetail(Repo, "GetAvailableStaffsAsync", "Retrieve available staff members", 
                    new { RegionId = deps.regionId, Time = checkTime }, initialList.Count(), true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetAvailableStaffsAsync", "Retrieve available staff members", 
                    new { RegionId = deps.regionId, Time = checkTime }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Update Tests
        [Test]
        public async Task Test_UpdateAsync()
        {
            var deps = await SeedDependenciesAsync();
            int uniqueEmpId = new Random().Next(1000, 5000);
            var staff = CreateMockStaff(uniqueEmpId, deps.regionId);
            await _repository.AddAsync(staff);

            try
            {
                staff.IsAllocated = true;
                await _repository.UpdateAsync(staff);
                var updated = await _repository.GetByIdAsync(uniqueEmpId);
                Assert.That(updated!.IsAllocated, Is.True);
                LogTestDetail(Repo, "UpdateAsync", "Update staff allocation flag", staff, updated, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateAsync", "Update staff allocation flag", staff, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Delete Tests
        [Test]
        public async Task Test_DeleteAsync()
        {
            var deps = await SeedDependenciesAsync();
            int uniqueEmpId = new Random().Next(1000, 5000);
            var staff = CreateMockStaff(uniqueEmpId, deps.regionId);
            await _repository.AddAsync(staff);

            try
            {
                await _repository.DeleteAsync(staff);
                var deleted = await _repository.GetByIdAsync(uniqueEmpId);
                Assert.That(deleted, Is.Null);
                LogTestDetail(Repo, "DeleteAsync", "Remove staff from database", staff, deleted, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "DeleteAsync", "Remove staff from database", staff, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
