using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Models;
using Event.Data.Repositories;

namespace Event.Data.Tests.RepositoryTests
{
    [TestFixture]
    public class AdminRepositoryTests : RepositoryTestBase
    {
        private AdminRepository? _repository;
        private const string Repo = "AdminRepository";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _repository = new AdminRepository(Context);
        }

        private Admin CreateMockAdmin(string adminId)
        {
            return new Admin
            {
                Admin_Id = adminId,
                Name = "Platform Administrator",
                Email = $"admin_{new Random().Next(1000, 9999)}@example.com",
                Password_Hash = "hashed_admin_pw"
            };
        }

        private async Task<Admin> SeedAdminAsync(string adminId)
        {
            var admin = CreateMockAdmin(adminId);
            await _repository.AddAsync(admin);
            return admin;
        }
        #endregion

        #region Create Tests
        [Test]
        public async Task Test_AddAsync()
        {
            string uniqueAdminId = $"ADM_{new Random().Next(1000, 9999)}";
            var admin = CreateMockAdmin(uniqueAdminId);

            try
            {
                await _repository.AddAsync(admin);
                Assert.That(admin.Admin_Id, Is.EqualTo(uniqueAdminId));
                LogTestDetail(Repo, "AddAsync", "Create new admin", admin, admin, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddAsync", "Create new admin", admin, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Read Tests
        [Test]
        public async Task Test_GetByIdAsync()
        {
            string uniqueAdminId = $"ADM_{new Random().Next(1000, 9999)}";
            var admin = await SeedAdminAsync(uniqueAdminId);

            try
            {
                var fetched = await _repository.GetByIdAsync(uniqueAdminId);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Name, Is.EqualTo(admin.Name));
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve admin by ID", uniqueAdminId, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve admin by ID", uniqueAdminId, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetByAdminIdAsync()
        {
            string uniqueAdminId = $"ADM_{new Random().Next(1000, 9999)}";
            var admin = await SeedAdminAsync(uniqueAdminId);

            try
            {
                var fetched = await _repository.GetByAdminIdAsync(uniqueAdminId);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Password_Hash, Is.EqualTo(admin.Password_Hash));
                LogTestDetail(Repo, "GetByAdminIdAsync", "Retrieve admin by admin ID custom method", uniqueAdminId, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByAdminIdAsync", "Retrieve admin by admin ID custom method", uniqueAdminId, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetAllAsync()
        {
            string uniqueAdminId = $"ADM_{new Random().Next(1000, 9999)}";
            var admin = await SeedAdminAsync(uniqueAdminId);

            try
            {
                var all = await _repository.GetAllAsync();
                Assert.That(all.Any(a => a.Admin_Id == uniqueAdminId), Is.True);
                LogTestDetail(Repo, "GetAllAsync", "List all admins", null, $"Total Admins: {all.Count()}", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetAllAsync", "List all admins", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Update Tests
        [Test]
        public async Task Test_UpdateAsync()
        {
            string uniqueAdminId = $"ADM_{new Random().Next(1000, 9999)}";
            var admin = await SeedAdminAsync(uniqueAdminId);

            try
            {
                admin.Name = "Admin Updated";
                await _repository.UpdateAsync(admin);
                var updated = await _repository.GetByIdAsync(uniqueAdminId);
                Assert.That(updated!.Name, Is.EqualTo("Admin Updated"));
                LogTestDetail(Repo, "UpdateAsync", "Update admin name to Admin Updated", admin, updated, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateAsync", "Update admin name to Admin Updated", admin, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Delete Tests
        [Test]
        public async Task Test_DeleteAsync()
        {
            string uniqueAdminId = $"ADM_{new Random().Next(1000, 9999)}";
            var admin = await SeedAdminAsync(uniqueAdminId);

            try
            {
                await _repository.DeleteAsync(admin);
                var deleted = await _repository.GetByIdAsync(uniqueAdminId);
                Assert.That(deleted, Is.Null);
                LogTestDetail(Repo, "DeleteAsync", "Remove admin from database", admin, deleted, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "DeleteAsync", "Remove admin from database", admin, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
