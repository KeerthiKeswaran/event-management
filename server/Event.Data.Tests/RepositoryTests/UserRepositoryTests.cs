using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Models;
using Event.Data.Repositories;

namespace Event.Data.Tests.RepositoryTests
{
    [TestFixture]
    public class UserRepositoryTests : RepositoryTestBase
    {
        private UserRepository? _repository;
        private Seed.DbSeeder? _seeder;
        private const string Repo = "UserRepository";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _repository = new UserRepository(Context);
            _seeder = new Seed.DbSeeder(Context);
        }
        #endregion

        #region Create Tests
        [Test]
        public async Task Test_AddAsync()
        {
            var terms = await _seeder.SeedTermsAndConditionsAsync("v1.0-add");
            var user = new User
            {
                Name = "John Add",
                Email = $"john_add_{new Random().Next(1000, 9999)}@example.com",
                Mobile_Number = "1234567890",
                Password_Hash = "hashed_pw",
                Consented_Terms_Id = terms.Terms_Id,
                Has_Marketing_Consent = false
            };

            try
            {
                await _repository.AddAsync(user);
                Assert.That(user.User_Id, Is.GreaterThan(0));
                LogTestDetail(Repo, "AddAsync", "Create new user in database", user, user, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddAsync", "Create new user in database", user, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Read Tests
        [Test]
        public async Task Test_GetByIdAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            try
            {
                var fetched = await _repository.GetByIdAsync(user.User_Id);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Name, Is.EqualTo(user.Name));
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve user by ID", user.User_Id, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve user by ID", user.User_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetByEmailAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            try
            {
                var fetched = await _repository.GetByEmailAsync(user.Email);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.User_Id, Is.EqualTo(user.User_Id));
                LogTestDetail(Repo, "GetByEmailAsync", "Retrieve user by email", user.Email, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByEmailAsync", "Retrieve user by email", user.Email, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetAllAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            try
            {
                var all = await _repository.GetAllAsync();
                Assert.That(all.Any(u => u.User_Id == user.User_Id), Is.True);
                LogTestDetail(Repo, "GetAllAsync", "List all users", null, $"Total Users: {all.Count()}", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetAllAsync", "List all users", null, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_ExistsAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            try
            {
                var exists = await _repository.ExistsAsync(user.User_Id);
                Assert.That(exists, Is.True);
                var notExists = await _repository.ExistsAsync(99999);
                Assert.That(notExists, Is.False);
                LogTestDetail(Repo, "ExistsAsync", "Check if user ID exists", new { ValidId = user.User_Id, InvalidId = 99999 }, new { ValidExists = exists, InvalidExists = notExists }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "ExistsAsync", "Check if user ID exists", user.User_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetUserProfileAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            try
            {
                var profile = await _repository.GetUserProfileAsync(user.User_Id);
                Assert.That(profile, Is.Not.Null);
                Assert.That(profile.Name, Is.EqualTo(user.Name));
                LogTestDetail(Repo, "GetUserProfileAsync", "Retrieve user profile with relations", user.User_Id, profile, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetUserProfileAsync", "Retrieve user profile with relations", user.User_Id, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Update Tests
        [Test]
        public async Task Test_UpdateAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            try
            {
                user.Name = "John Updated";
                await _repository.UpdateAsync(user);
                var updated = await _repository.GetByIdAsync(user.User_Id);
                Assert.That(updated!.Name, Is.EqualTo("John Updated"));
                LogTestDetail(Repo, "UpdateAsync", "Update user name property to John Updated", user, updated, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateAsync", "Update user name property to John Updated", user, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_UpdateInterestedRegionsAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var region = await _seeder.SeedRegionAsync("US-EAST", 5);
            try
            {
                await _repository.UpdateInterestedRegionsAsync(user.User_Id, new[] { "US-EAST" });
                var profile = await _repository.GetUserProfileAsync(user.User_Id);
                var hasRegion = profile.InterestedRegions.Any(r => r.Region_Id == "US-EAST");
                Assert.That(hasRegion, Is.True);
                LogTestDetail(Repo, "UpdateInterestedRegionsAsync", "Add interested region US-EAST to user", new { UserId = user.User_Id, Regions = new[] { "US-EAST" } }, new { HasRegion = hasRegion }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateInterestedRegionsAsync", "Add interested region US-EAST to user", new { UserId = user.User_Id }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Delete Tests
        [Test]
        public async Task Test_DeleteAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            try
            {
                await _repository.DeleteAsync(user);
                var deleted = await _repository.GetByIdAsync(user.User_Id);
                Assert.That(deleted, Is.Null);
                LogTestDetail(Repo, "DeleteAsync", "Remove user from database", user, deleted, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "DeleteAsync", "Remove user from database", user, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
