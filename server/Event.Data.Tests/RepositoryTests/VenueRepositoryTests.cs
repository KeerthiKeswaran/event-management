using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Models;
using Event.Data.Repositories;

namespace Event.Data.Tests.RepositoryTests
{
    [TestFixture]
    public class VenueRepositoryTests : RepositoryTestBase
    {
        private VenueRepository? _repository;
        private Seed.DbSeeder? _seeder;
        private const string Repo = "VenueRepository";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _repository = new VenueRepository(Context);
            _seeder = new Seed.DbSeeder(Context);
        }

        private Region CreateMockRegion(string regionId)
        {
            return new Region { Region_Id = regionId, No_Of_Staffs = 3 };
        }

        private Venue CreateMockVenue(string regionId)
        {
            return new Venue
            {
                Region_Id = regionId,
                Name = "Conference Room " + new Random().Next(1000, 9999),
                Address = "Tech Plaza",
                Hourly_Price = 50.00m,
                Is_Available = true
            };
        }

        private Event.Models.Event CreateMockEvent(int organizerId, int venueId, DateTime dateTime, decimal duration)
        {
            return new Event.Models.Event
            {
                Organizer_Id = organizerId,
                Venue_Id = venueId,
                Event_Type = "Physical",
                Title = "Board Meeting",
                Date_Time = dateTime,
                Duration_Hours = duration,
                Status = "Live"
            };
        }

        private async Task<Region> SeedRegionAsync(string regionId)
        {
            var region = CreateMockRegion(regionId);
            Context.Regions.Add(region);
            await Context.SaveChangesAsync();
            return region;
        }

        private async Task<Venue> SeedVenueAsync(string regionId)
        {
            var venue = CreateMockVenue(regionId);
            await _repository.AddAsync(venue);
            return venue;
        }
        #endregion

        #region Create Tests
        [Test]
        public async Task Test_AddAsync()
        {
            string uniqueRegionId = $"REG_{new Random().Next(1000, 9999)}";
            await SeedRegionAsync(uniqueRegionId);
            var venue = CreateMockVenue(uniqueRegionId);

            try
            {
                await _repository.AddAsync(venue);
                Assert.That(venue.Venue_Id, Is.GreaterThan(0));
                LogTestDetail(Repo, "AddAsync", "Create new venue", venue, venue, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddAsync", "Create new venue", venue, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Read Tests
        [Test]
        public async Task Test_GetByIdAsync()
        {
            string uniqueRegionId = $"REG_{new Random().Next(1000, 9999)}";
            await SeedRegionAsync(uniqueRegionId);
            var venue = await SeedVenueAsync(uniqueRegionId);

            try
            {
                var fetched = await _repository.GetByIdAsync(venue.Venue_Id);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Name, Is.EqualTo(venue.Name));
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve venue by ID", venue.Venue_Id, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve venue by ID", venue.Venue_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_IsVenueOccupiedAsync_NoEvents_ReturnsFalse()
        {
            string uniqueRegionId = $"REG_{new Random().Next(1000, 9999)}";
            await SeedRegionAsync(uniqueRegionId);
            var venue = await SeedVenueAsync(uniqueRegionId);
            var targetTime = DateTime.UtcNow.Date.AddHours(14);

            try
            {
                var isOccupied = await _repository.IsVenueOccupiedAsync(venue.Venue_Id, targetTime);
                Assert.That(isOccupied, Is.False);
                LogTestDetail(Repo, "IsVenueOccupiedAsync", "Check occupancy when no events booked", new { VenueId = venue.Venue_Id, Time = targetTime }, isOccupied, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "IsVenueOccupiedAsync", "Check occupancy when no events booked", new { VenueId = venue.Venue_Id, Time = targetTime }, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_IsVenueOccupiedAsync_EventBooked_ReturnsTrue()
        {
            string uniqueRegionId = $"REG_{new Random().Next(1000, 9999)}";
            await SeedRegionAsync(uniqueRegionId);
            var venue = await SeedVenueAsync(uniqueRegionId);
            var organizer = await _seeder.SeedUserAsync("Organizer");
            var targetTime = DateTime.UtcNow.Date.AddHours(14);

            var liveEvent = CreateMockEvent(organizer.User_Id, venue.Venue_Id, DateTime.UtcNow.Date.AddHours(13), 3.0m);
            Context.Events.Add(liveEvent);
            await Context.SaveChangesAsync();

            try
            {
                var isOccupied = await _repository.IsVenueOccupiedAsync(venue.Venue_Id, targetTime);
                Assert.That(isOccupied, Is.True);
                LogTestDetail(Repo, "IsVenueOccupiedAsync", "Check occupancy during a booked live event", new { VenueId = venue.Venue_Id, Time = targetTime }, isOccupied, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "IsVenueOccupiedAsync", "Check occupancy during a booked live event", new { VenueId = venue.Venue_Id, Time = targetTime }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Update Tests
        [Test]
        public async Task Test_UpdateAsync()
        {
            string uniqueRegionId = $"REG_{new Random().Next(1000, 9999)}";
            await SeedRegionAsync(uniqueRegionId);
            var venue = await SeedVenueAsync(uniqueRegionId);

            try
            {
                venue.Name = "Conference Room B";
                await _repository.UpdateAsync(venue);
                var updated = await _repository.GetByIdAsync(venue.Venue_Id);
                Assert.That(updated!.Name, Is.EqualTo("Conference Room B"));
                LogTestDetail(Repo, "UpdateAsync", "Update venue name to Conference Room B", venue, updated, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateAsync", "Update venue name to Conference Room B", venue, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Delete Tests
        [Test]
        public async Task Test_DeleteAsync()
        {
            string uniqueRegionId = $"REG_{new Random().Next(1000, 9999)}";
            await SeedRegionAsync(uniqueRegionId);
            var venue = await SeedVenueAsync(uniqueRegionId);

            try
            {
                await _repository.DeleteAsync(venue);
                var deleted = await _repository.GetByIdAsync(venue.Venue_Id);
                Assert.That(deleted, Is.Null);
                LogTestDetail(Repo, "DeleteAsync", "Remove venue from database", venue, deleted, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "DeleteAsync", "Remove venue from database", venue, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
