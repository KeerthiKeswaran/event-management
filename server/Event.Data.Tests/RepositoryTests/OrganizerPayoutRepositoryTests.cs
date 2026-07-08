using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Models;
using Event.Data.Repositories;

namespace Event.Data.Tests.RepositoryTests
{
    [TestFixture]
    public class OrganizerPayoutRepositoryTests : RepositoryTestBase
    {
        private OrganizerPayoutRepository? _repository;
        private Seed.DbSeeder? _seeder;
        private const string Repo = "OrganizerPayoutRepository";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _repository = new OrganizerPayoutRepository(Context);
            _seeder = new Seed.DbSeeder(Context);
        }

        private Event.Models.Event CreateMockEvent(int organizerId)
        {
            return new Event.Models.Event
            {
                Organizer_Id = organizerId,
                Event_Type = "Virtual",
                Title = "Online Workshop",
                Status = "Completed",
                Date_Time = DateTime.UtcNow
            };
        }

        private Transaction CreateMockTransaction(int organizerId)
        {
            return new Transaction
            {
                Sender_Id = "Platform_Escrow",
                Receiver_Id = $"Organizer_User_{organizerId}",
                Transaction_Type = "OrganizerPayout",
                Related_Id = 0,
                Amount = 1000.00m,
                Currency = "USD",
                Status = "Pending",
                Created_At = DateTime.UtcNow
            };
        }

        private OrganizerPayout CreateMockPayout(int eventId, long transactionId)
        {
            return new OrganizerPayout
            {
                Event_Id = eventId,
                Transaction_Id = transactionId,
                Total_Ticket_Sales = 1200.00m,
                Platform_Commission = 200.00m,
                Payout_Amount = 1000.00m,
                Payout_Status = "Pending",
                Processed_At = DateTime.UtcNow
            };
        }

        private async Task<(User organizer, Event.Models.Event newEvent, Transaction transaction)> SeedDependenciesAsync()
        {
            var organizer = await _seeder.SeedUserAsync("Organizer");
            var newEvent = CreateMockEvent(organizer.User_Id);
            Context.Events.Add(newEvent);

            var transaction = CreateMockTransaction(organizer.User_Id);
            Context.Transactions.Add(transaction);
            await Context.SaveChangesAsync();

            return (organizer, newEvent, transaction);
        }
        #endregion

        #region Create Tests
        [Test]
        public async Task Test_AddAsync()
        {
            var deps = await SeedDependenciesAsync();
            var payout = CreateMockPayout(deps.newEvent.Event_Id, deps.transaction.Transaction_Id);

            try
            {
                await _repository.AddAsync(payout);
                Assert.That(payout.Payout_Id, Is.GreaterThan(0));
                LogTestDetail(Repo, "AddAsync", "Create organizer payout", payout, payout, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddAsync", "Create organizer payout", payout, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Read Tests
        [Test]
        public async Task Test_GetByIdAsync()
        {
            var deps = await SeedDependenciesAsync();
            var payout = CreateMockPayout(deps.newEvent.Event_Id, deps.transaction.Transaction_Id);
            await _repository.AddAsync(payout);

            try
            {
                var fetched = await _repository.GetByIdAsync(payout.Payout_Id);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Payout_Amount, Is.EqualTo(1000.00m));
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve payout by ID", payout.Payout_Id, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve payout by ID", payout.Payout_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetPayoutByEventIdAsync()
        {
            var deps = await SeedDependenciesAsync();
            var payout = CreateMockPayout(deps.newEvent.Event_Id, deps.transaction.Transaction_Id);
            await _repository.AddAsync(payout);

            try
            {
                var eventPayout = await _repository.GetPayoutByEventIdAsync(deps.newEvent.Event_Id);
                Assert.That(eventPayout, Is.Not.Null);
                Assert.That(eventPayout!.Payout_Amount, Is.EqualTo(1000.00m));
                LogTestDetail(Repo, "GetPayoutByEventIdAsync", "Retrieve payout by Event ID", deps.newEvent.Event_Id, eventPayout, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetPayoutByEventIdAsync", "Retrieve payout by Event ID", deps.newEvent.Event_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetPendingPayoutsAsync()
        {
            var deps = await SeedDependenciesAsync();
            var payout = CreateMockPayout(deps.newEvent.Event_Id, deps.transaction.Transaction_Id);
            await _repository.AddAsync(payout);

            try
            {
                var pendingPayouts = await _repository.GetPendingPayoutsAsync();
                Assert.That(pendingPayouts.Any(p => p.Payout_Id == payout.Payout_Id), Is.True);
                LogTestDetail(Repo, "GetPendingPayoutsAsync", "Retrieve pending payouts", null, pendingPayouts.Count(), true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetPendingPayoutsAsync", "Retrieve pending payouts", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Update Tests
        [Test]
        public async Task Test_UpdateAsync()
        {
            var deps = await SeedDependenciesAsync();
            var payout = CreateMockPayout(deps.newEvent.Event_Id, deps.transaction.Transaction_Id);
            await _repository.AddAsync(payout);

            try
            {
                payout.Payout_Status = "Success";
                await _repository.UpdateAsync(payout);
                var updated = await _repository.GetByIdAsync(payout.Payout_Id);
                Assert.That(updated!.Payout_Status, Is.EqualTo("Success"));
                LogTestDetail(Repo, "UpdateAsync", "Update payout status to Success", payout, updated, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateAsync", "Update payout status to Success", payout, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Delete Tests
        [Test]
        public async Task Test_DeleteAsync()
        {
            var deps = await SeedDependenciesAsync();
            var payout = CreateMockPayout(deps.newEvent.Event_Id, deps.transaction.Transaction_Id);
            await _repository.AddAsync(payout);

            try
            {
                await _repository.DeleteAsync(payout);
                var deleted = await _repository.GetByIdAsync(payout.Payout_Id);
                Assert.That(deleted, Is.Null);
                LogTestDetail(Repo, "DeleteAsync", "Remove payout from database", payout, deleted, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "DeleteAsync", "Remove payout from database", payout, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
