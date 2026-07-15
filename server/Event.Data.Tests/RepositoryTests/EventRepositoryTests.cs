using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Models;
using Event.Data.Repositories;

namespace Event.Data.Tests.RepositoryTests
{
    [TestFixture]
    public class EventRepositoryTests : RepositoryTestBase
    {
        private EventRepository? _repository;
        private Seed.DbSeeder? _seeder;
        private const string Repo = "EventRepository";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _repository = new EventRepository(Context);
            _seeder = new Seed.DbSeeder(Context);
        }

        private Region CreateMockRegion(string regionId)
        {
            return new Region { Region_Id = regionId, No_Of_Staffs = 5 };
        }

        private Venue CreateMockVenue(string regionId)
        {
            return new Venue
            {
                Region_Id = regionId,
                Name = "Grand Hall " + new Random().Next(1000, 9999),
                Address = "123 Main St",
                Hourly_Price = 100.00m,
                Is_Available = true
            };
        }

        private Event.Models.Event CreateMockEvent(int organizerId, int venueId)
        {
            return new Event.Models.Event
            {
                Organizer_Id = organizerId,
                Venue_Id = venueId,
                Event_Type = "Physical",
                Category = "Music",
                Title = "Annual Tech Conference",
                Description_Url = "Largest event of the year",
                Date_Time = DateTime.UtcNow.AddDays(10),
                Duration_Hours = 4.5m,
                Status = "Live",
                Requires_Staff = true
            };
        }

        private async Task<(User organizer, Venue venue, string regionId)> SeedDependenciesAsync()
        {
            string uniqueRegionId = $"REG_{new Random().Next(1000, 9999)}";
            var region = CreateMockRegion(uniqueRegionId);
            Context.Regions.Add(region);

            var venue = CreateMockVenue(uniqueRegionId);
            Context.Venues.Add(venue);

            var organizer = await _seeder.SeedUserAsync("Organizer");
            await Context.SaveChangesAsync();

            return (organizer, venue, uniqueRegionId);
        }
        #endregion

        #region Create Tests
        [Test]
        public async Task Test_AddAsync()
        {
            var deps = await SeedDependenciesAsync();
            var newEvent = CreateMockEvent(deps.organizer.User_Id, deps.venue.Venue_Id);

            try
            {
                await _repository.AddAsync(newEvent);
                Assert.That(newEvent.Event_Id, Is.GreaterThan(0));
                LogTestDetail(Repo, "AddAsync", "Create new event", newEvent, newEvent, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddAsync", "Create new event", newEvent, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Read Tests
        [Test]
        public async Task Test_GetByIdAsync()
        {
            var deps = await SeedDependenciesAsync();
            var newEvent = CreateMockEvent(deps.organizer.User_Id, deps.venue.Venue_Id);
            await _repository.AddAsync(newEvent);

            try
            {
                var fetched = await _repository.GetByIdAsync(newEvent.Event_Id);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Title, Is.EqualTo("Annual Tech Conference"));
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve event by ID", newEvent.Event_Id, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve event by ID", newEvent.Event_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetEventDetailsAsync()
        {
            var deps = await SeedDependenciesAsync();
            var newEvent = CreateMockEvent(deps.organizer.User_Id, deps.venue.Venue_Id);
            await _repository.AddAsync(newEvent);

            try
            {
                var details = await _repository.GetEventDetailsAsync(newEvent.Event_Id);
                Assert.That(details, Is.Not.Null);
                Assert.That(details!.Venue, Is.Not.Null);
                Assert.That(details.Venue!.Name, Is.EqualTo(deps.venue.Name));
                LogTestDetail(Repo, "GetEventDetailsAsync", "Retrieve event details with venue", newEvent.Event_Id, details, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetEventDetailsAsync", "Retrieve event details with venue", newEvent.Event_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_SearchEventsAsync()
        {
            var deps = await SeedDependenciesAsync();
            var newEvent = CreateMockEvent(deps.organizer.User_Id, deps.venue.Venue_Id);
            await _repository.AddAsync(newEvent);

            try
            {
                var searchKeyword = await _repository.SearchEventsAsync("Tech", null, null, null, null, null, null, 1, 10);
                Assert.That(searchKeyword.Items.Any(e => e.Event_Id == newEvent.Event_Id), Is.True);

                var searchCategory = await _repository.SearchEventsAsync(null, "Music", null, null, null, null, null, 1, 10);
                Assert.That(searchCategory.Items.Any(e => e.Event_Id == newEvent.Event_Id), Is.True);

                var searchRegion = await _repository.SearchEventsAsync(null, null, null, deps.regionId, null, null, null, 1, 10);
                Assert.That(searchRegion.Items.Any(e => e.Event_Id == newEvent.Event_Id), Is.True);

                LogTestDetail(Repo, "SearchEventsAsync", "Search events with multiple filters", 
                    new { Keyword = "Tech", Category = "Physical", Region = deps.regionId }, 
                    new { KeywordMatches = searchKeyword.Items.Count(), RegionMatches = searchRegion.Items.Count() }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "SearchEventsAsync", "Search events with multiple filters", null, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_ExistsAsync()
        {
            var deps = await SeedDependenciesAsync();
            var newEvent = CreateMockEvent(deps.organizer.User_Id, deps.venue.Venue_Id);
            await _repository.AddAsync(newEvent);

            try
            {
                var exists = await _repository.ExistsAsync(newEvent.Event_Id);
                Assert.That(exists, Is.True);
                var notExists = await _repository.ExistsAsync(99999);
                Assert.That(notExists, Is.False);
                LogTestDetail(Repo, "ExistsAsync", "Check if event ID exists", new { ValidId = newEvent.Event_Id, InvalidId = 99999 }, new { ValidExists = exists, InvalidExists = notExists }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "ExistsAsync", "Check if event ID exists", newEvent.Event_Id, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Update Tests
        [Test]
        public async Task Test_UpdateAsync()
        {
            var deps = await SeedDependenciesAsync();
            var newEvent = CreateMockEvent(deps.organizer.User_Id, deps.venue.Venue_Id);
            await _repository.AddAsync(newEvent);

            try
            {
                newEvent.Title = "Updated Title";
                await _repository.UpdateAsync(newEvent);
                var updated = await _repository.GetByIdAsync(newEvent.Event_Id);
                Assert.That(updated!.Title, Is.EqualTo("Updated Title"));
                LogTestDetail(Repo, "UpdateAsync", "Update event title", newEvent, updated, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateAsync", "Update event title", newEvent, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Custom Operations Tests
        [Test]
        public async Task Test_AddReportAsync()
        {
            var deps = await SeedDependenciesAsync();
            var newEvent = CreateMockEvent(deps.organizer.User_Id, deps.venue.Venue_Id);
            await _repository.AddAsync(newEvent);

            var report = new EventReport
            {
                Event_Id = newEvent.Event_Id,
                Reporter_Id = deps.organizer.User_Id,
                ReportUrl = "/assets/events/10001/reports/10002_report.json",
                Created_At = DateTime.UtcNow
            };

            try
            {
                await _repository.AddReportAsync(report);
                var reportInDb = Context.EventReports.FirstOrDefault(r => r.Event_Id == newEvent.Event_Id);
                Assert.That(reportInDb, Is.Not.Null);
                Assert.That(reportInDb.ReportUrl, Is.EqualTo("/assets/events/10001/reports/10002_report.json"));
                LogTestDetail(Repo, "AddReportAsync", "Submit event report", report, reportInDb, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddReportAsync", "Submit event report", report, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_AddFeedbackAsync()
        {
            var deps = await SeedDependenciesAsync();
            var newEvent = CreateMockEvent(deps.organizer.User_Id, deps.venue.Venue_Id);
            await _repository.AddAsync(newEvent);

            var feedback = new EventFeedback
            {
                Event_Id = newEvent.Event_Id,
                Attendee_Id = deps.organizer.User_Id,
                Rating = 5,
                Review = "Great event!"
            };

            try
            {
                await _repository.AddFeedbackAsync(feedback);
                var feedbackInDb = Context.EventFeedbacks.FirstOrDefault(f => f.Event_Id == newEvent.Event_Id);
                Assert.That(feedbackInDb, Is.Not.Null);
                Assert.That(feedbackInDb.Review, Is.EqualTo("Great event!"));
                LogTestDetail(Repo, "AddFeedbackAsync", "Submit event feedback", feedback, feedbackInDb, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddFeedbackAsync", "Submit event feedback", feedback, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Delete Tests
        [Test]
        public async Task Test_DeleteAsync()
        {
            var deps = await SeedDependenciesAsync();
            var newEvent = CreateMockEvent(deps.organizer.User_Id, deps.venue.Venue_Id);
            await _repository.AddAsync(newEvent);

            try
            {
                await _repository.DeleteAsync(newEvent);
                var deleted = await _repository.GetByIdAsync(newEvent.Event_Id);
                Assert.That(deleted, Is.Null);
                LogTestDetail(Repo, "DeleteAsync", "Remove event from database", newEvent, deleted, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "DeleteAsync", "Remove event from database", newEvent, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
