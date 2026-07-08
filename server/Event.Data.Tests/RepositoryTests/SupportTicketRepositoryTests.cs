using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Models;
using Event.Data.Repositories;

namespace Event.Data.Tests.RepositoryTests
{
    [TestFixture]
    public class SupportTicketRepositoryTests : RepositoryTestBase
    {
        private SupportTicketRepository? _repository;
        private Seed.DbSeeder? _seeder;
        private const string Repo = "SupportTicketRepository";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _repository = new SupportTicketRepository(Context);
            _seeder = new Seed.DbSeeder(Context);
        }

        private SupportTicket CreateMockTicket(int userId)
        {
            return new SupportTicket
            {
                User_Id = userId,
                ConcernUrl = "/support_tickets/ticket_test.json",
                RequestType = "REF",
                Status = "Open",
                EsclationStatus = null
            };
        }
        #endregion

        #region Create Tests
        [Test]
        public async Task Test_AddAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var ticket = CreateMockTicket(user.User_Id);

            try
            {
                await _repository.AddAsync(ticket);
                Assert.That(ticket.Ticket_Id, Is.GreaterThan(0));
                LogTestDetail(Repo, "AddAsync", "Create support ticket", ticket, ticket, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddAsync", "Create support ticket", ticket, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Read Tests
        [Test]
        public async Task Test_GetByIdAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var ticket = CreateMockTicket(user.User_Id);
            await _repository.AddAsync(ticket);

            try
            {
                var fetched = await _repository.GetByIdAsync(ticket.Ticket_Id);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.ConcernUrl, Is.EqualTo("/support_tickets/ticket_test.json"));
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve support ticket by ID", ticket.Ticket_Id, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve support ticket by ID", ticket.Ticket_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetTicketsByUserIdAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var ticket = CreateMockTicket(user.User_Id);
            await _repository.AddAsync(ticket);

            try
            {
                var tickets = await _repository.GetTicketsByUserIdAsync(user.User_Id);
                Assert.That(tickets.Count(), Is.EqualTo(1));
                LogTestDetail(Repo, "GetTicketsByUserIdAsync", "Retrieve tickets for User ID", user.User_Id, tickets.Count(), true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetTicketsByUserIdAsync", "Retrieve tickets for User ID", user.User_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetPendingTicketsAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var ticket = CreateMockTicket(user.User_Id);
            await _repository.AddAsync(ticket);

            try
            {
                var pending = await _repository.GetPendingTicketsAsync();
                Assert.That(pending.Any(q => q.Ticket_Id == ticket.Ticket_Id), Is.True);
                LogTestDetail(Repo, "GetPendingTicketsAsync", "Retrieve pending tickets (Open status)", null, pending.Count(), true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetPendingTicketsAsync", "Retrieve pending tickets (Open status)", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Update Tests
        [Test]
        public async Task Test_UpdateAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var ticket = CreateMockTicket(user.User_Id);
            await _repository.AddAsync(ticket);

            try
            {
                ticket.ConcernUrl = "/support_tickets/ticket_test_updated.json";
                ticket.Status = "Resolved";
                await _repository.UpdateAsync(ticket);
                var updated = await _repository.GetByIdAsync(ticket.Ticket_Id);
                Assert.That(updated!.Status, Is.EqualTo("Resolved"));
                LogTestDetail(Repo, "UpdateAsync", "Update support ticket status to Resolved", ticket, updated, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateAsync", "Update support ticket status to Resolved", ticket, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Delete Tests
        [Test]
        public async Task Test_DeleteAsync()
        {
            var user = await _seeder.SeedUserAsync("Attendee");
            var ticket = CreateMockTicket(user.User_Id);
            await _repository.AddAsync(ticket);

            try
            {
                await _repository.DeleteAsync(ticket);
                var deleted = await _repository.GetByIdAsync(ticket.Ticket_Id);
                Assert.That(deleted, Is.Null);
                LogTestDetail(Repo, "DeleteAsync", "Remove support ticket from database", ticket, deleted, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "DeleteAsync", "Remove support ticket from database", ticket, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
