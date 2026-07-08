using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Models;
using Event.Data.Repositories;

namespace Event.Data.Tests.RepositoryTests
{
    [TestFixture]
    public class PlatformSettingsRepositoryTests : RepositoryTestBase
    {
        private PlatformSettingsRepository? _repository;
        private const string Repo = "PlatformSettingsRepository";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _repository = new PlatformSettingsRepository(Context);
            var existing = Context.PlatformSettings.ToList();
            if (existing.Count > 0)
            {
                Context.PlatformSettings.RemoveRange(existing);
                Context.SaveChanges();
            }
        }

        private Admin CreateMockAdmin()
        {
            var suffix = new Random().Next(1000, 9999);
            return new Admin
            {
                Admin_Id = $"ADM_777_{suffix}",
                Name = "Settings Admin",
                Email = $"settings_admin_{suffix}@example.com",
                Password_Hash = "hashed"
            };
        }

        private PlatformSettings CreateMockSettings(string adminId)
        {
            return new PlatformSettings
            {
                Settings_Id = new Random().Next(1000, 99999),
                Staff_Flat_Rate = 25.00m,
                Virtual_Event_Activation_Fee = 10.00m,
                Physical_Event_Activation_Fee = 50.00m,
                Ticket_Commission_Percentage = 5.00m,
                Ticket_Fixed_Fee = 0.99m,
                Max_Tickets_Per_Booking = 10,
                Updated_At = DateTime.UtcNow,
                Updated_By_Admin_Id = adminId
            };
        }

        private async Task<Admin> SeedAdminAsync()
        {
            var admin = CreateMockAdmin();
            Context.Admins.Add(admin);
            await Context.SaveChangesAsync();
            return admin;
        }
        #endregion

        #region Create Tests
        [Test]
        public async Task Test_AddAsync()
        {
            var admin = await SeedAdminAsync();
            var settings = CreateMockSettings(admin.Admin_Id);

            try
            {
                await _repository.AddAsync(settings);
                Assert.That(settings.Settings_Id, Is.GreaterThan(0));
                LogTestDetail(Repo, "AddAsync", "Create platform settings", settings, settings, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddAsync", "Create platform settings", settings, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Read Tests
        [Test]
        public async Task Test_GetByIdAsync()
        {
            var admin = await SeedAdminAsync();
            var settings = CreateMockSettings(admin.Admin_Id);
            await _repository.AddAsync(settings);

            try
            {
                var fetched = await _repository.GetByIdAsync(settings.Settings_Id);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Staff_Flat_Rate, Is.EqualTo(25.00m));
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve settings by ID", settings.Settings_Id, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve settings by ID", settings.Settings_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetSettingsAsync()
        {
            var admin = await SeedAdminAsync();
            var settings = CreateMockSettings(admin.Admin_Id);
            settings.Settings_Id = 1; // Force ID to 1 for global settings singleton test

            try
            {
                var existing = await _repository.GetByIdAsync(1);
                if (existing == null)
                {
                    await _repository.AddAsync(settings);
                }
                
                var globalSettings = await _repository.GetSettingsAsync();
                Assert.That(globalSettings, Is.Not.Null);
                LogTestDetail(Repo, "GetSettingsAsync", "Retrieve global settings singleton", null, globalSettings, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetSettingsAsync", "Retrieve global settings singleton", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Update Tests
        [Test]
        public async Task Test_UpdateAsync()
        {
            var admin = await SeedAdminAsync();
            var settings = CreateMockSettings(admin.Admin_Id);
            await _repository.AddAsync(settings);

            try
            {
                settings.Staff_Flat_Rate = 30.00m;
                await _repository.UpdateAsync(settings);
                var updated = await _repository.GetByIdAsync(settings.Settings_Id);
                Assert.That(updated!.Staff_Flat_Rate, Is.EqualTo(30.00m));
                LogTestDetail(Repo, "UpdateAsync", "Update flat rate of staff", settings, updated, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateAsync", "Update flat rate of staff", settings, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Delete Tests
        [Test]
        public async Task Test_DeleteAsync()
        {
            var admin = await SeedAdminAsync();
            var settings = CreateMockSettings(admin.Admin_Id);
            await _repository.AddAsync(settings);

            try
            {
                await _repository.DeleteAsync(settings);
                var deleted = await _repository.GetByIdAsync(settings.Settings_Id);
                Assert.That(deleted, Is.Null);
                LogTestDetail(Repo, "DeleteAsync", "Remove settings from database", settings, deleted, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "DeleteAsync", "Remove settings from database", settings, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
