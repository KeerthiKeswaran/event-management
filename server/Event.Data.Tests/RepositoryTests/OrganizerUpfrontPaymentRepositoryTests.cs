using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Models;
using Event.Data.Repositories;

namespace Event.Data.Tests.RepositoryTests
{
    [TestFixture]
    public class OrganizerUpfrontPaymentRepositoryTests : RepositoryTestBase
    {
        private OrganizerUpfrontPaymentRepository? _repository;
        private Seed.DbSeeder? _seeder;
        private const string Repo = "OrganizerUpfrontPaymentRepository";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _repository = new OrganizerUpfrontPaymentRepository(Context);
            _seeder = new Seed.DbSeeder(Context);
        }

        private Event.Models.Event CreateMockEvent(int organizerId)
        {
            return new Event.Models.Event
            {
                Organizer_Id = organizerId,
                Event_Type = "Physical",
                Title = "Exposition",
                Status = "Live",
                Date_Time = DateTime.UtcNow
            };
        }

        private Transaction CreateMockTransaction(int organizerId)
        {
            return new Transaction
            {
                Sender_Id = $"Organizer_User_{organizerId}",
                Receiver_Id = "Platform_Escrow",
                Transaction_Type = "OrganizerUpfrontPayment",
                Related_Id = 0,
                Amount = 500.00m,
                Currency = "USD",
                Status = "Success",
                Created_At = DateTime.UtcNow
            };
        }

        private OrganizerUpfrontPayment CreateMockUpfrontPayment(int eventId, long transactionId)
        {
            return new OrganizerUpfrontPayment
            {
                Event_Id = eventId,
                Transaction_Id = transactionId,
                Amount = 500.00m,
                Payment_Status = "Success",
                Created_At = DateTime.UtcNow
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
            var upfrontPayment = CreateMockUpfrontPayment(deps.newEvent.Event_Id, deps.transaction.Transaction_Id);

            try
            {
                await _repository.AddAsync(upfrontPayment);
                Assert.That(upfrontPayment.Upfront_Payment_Id, Is.GreaterThan(0));
                LogTestDetail(Repo, "AddAsync", "Create organizer upfront payment", upfrontPayment, upfrontPayment, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddAsync", "Create organizer upfront payment", upfrontPayment, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Read Tests
        [Test]
        public async Task Test_GetByIdAsync()
        {
            var deps = await SeedDependenciesAsync();
            var upfrontPayment = CreateMockUpfrontPayment(deps.newEvent.Event_Id, deps.transaction.Transaction_Id);
            await _repository.AddAsync(upfrontPayment);

            try
            {
                var fetched = await _repository.GetByIdAsync(upfrontPayment.Upfront_Payment_Id);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Amount, Is.EqualTo(500.00m));
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve upfront payment by ID", upfrontPayment.Upfront_Payment_Id, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve upfront payment by ID", upfrontPayment.Upfront_Payment_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetUpfrontPaymentsByEventIdAsync()
        {
            var deps = await SeedDependenciesAsync();
            var upfrontPayment = CreateMockUpfrontPayment(deps.newEvent.Event_Id, deps.transaction.Transaction_Id);
            await _repository.AddAsync(upfrontPayment);

            try
            {
                var payments = await _repository.GetUpfrontPaymentsByEventIdAsync(deps.newEvent.Event_Id);
                Assert.That(payments.Count(), Is.EqualTo(1));
                LogTestDetail(Repo, "GetUpfrontPaymentsByEventIdAsync", "Retrieve upfront payments for Event ID", deps.newEvent.Event_Id, payments, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetUpfrontPaymentsByEventIdAsync", "Retrieve upfront payments for Event ID", deps.newEvent.Event_Id, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Update Tests
        [Test]
        public async Task Test_UpdateAsync()
        {
            var deps = await SeedDependenciesAsync();
            var upfrontPayment = CreateMockUpfrontPayment(deps.newEvent.Event_Id, deps.transaction.Transaction_Id);
            await _repository.AddAsync(upfrontPayment);

            try
            {
                upfrontPayment.Payment_Status = "Refunded";
                await _repository.UpdateAsync(upfrontPayment);
                var updated = await _repository.GetByIdAsync(upfrontPayment.Upfront_Payment_Id);
                Assert.That(updated!.Payment_Status, Is.EqualTo("Refunded"));
                LogTestDetail(Repo, "UpdateAsync", "Update upfront payment status to Refunded", upfrontPayment, updated, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateAsync", "Update upfront payment status to Refunded", upfrontPayment, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Delete Tests
        [Test]
        public async Task Test_DeleteAsync()
        {
            var deps = await SeedDependenciesAsync();
            var upfrontPayment = CreateMockUpfrontPayment(deps.newEvent.Event_Id, deps.transaction.Transaction_Id);
            await _repository.AddAsync(upfrontPayment);

            try
            {
                await _repository.DeleteAsync(upfrontPayment);
                var deleted = await _repository.GetByIdAsync(upfrontPayment.Upfront_Payment_Id);
                Assert.That(deleted, Is.Null);
                LogTestDetail(Repo, "DeleteAsync", "Remove upfront payment from database", upfrontPayment, deleted, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "DeleteAsync", "Remove upfront payment from database", upfrontPayment, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
