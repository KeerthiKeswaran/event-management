using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Models;
using Event.Data.Repositories;

namespace Event.Data.Tests.RepositoryTests
{
    [TestFixture]
    public class BookingPaymentRepositoryTests : RepositoryTestBase
    {
        private BookingPaymentRepository? _repository;
        private Seed.DbSeeder? _seeder;
        private const string Repo = "BookingPaymentRepository";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _repository = new BookingPaymentRepository(Context);
            _seeder = new Seed.DbSeeder(Context);
        }

        private Event.Models.Event CreateMockEvent(int organizerId)
        {
            return new Event.Models.Event
            {
                Organizer_Id = organizerId,
                Event_Type = "Virtual",
                Title = "Live Stream",
                Status = "Live",
                Date_Time = DateTime.UtcNow
            };
        }

        private Booking CreateMockBooking(int attendeeId, int eventId)
        {
            return new Booking
            {
                Attendee_Id = attendeeId,
                Event_Id = eventId,
                Booking_Status = "Payment Pending",
                Created_At = DateTime.UtcNow
            };
        }

        private Transaction CreateMockTransaction(int attendeeId)
        {
            return new Transaction
            {
                Sender_Id = $"Attendee_User_{attendeeId}",
                Receiver_Id = "Platform_Escrow",
                Transaction_Type = "BookingPayment",
                Related_Id = 0,
                Amount = 150.00m,
                Currency = "USD",
                Status = "Success",
                Created_At = DateTime.UtcNow
            };
        }

        private BookingPayment CreateMockPayment(int bookingId, long transactionId)
        {
            return new BookingPayment
            {
                Booking_Id = bookingId,
                Transaction_Id = transactionId,
                Amount = 150.00m,
                Platform_Fee_Cut = 7.50m,
                Payment_Status = "Success",
                Created_At = DateTime.UtcNow
            };
        }

        private async Task<(User attendee, User organizer, Event.Models.Event newEvent, Booking booking, Transaction transaction)> SeedDependenciesAsync()
        {
            var attendee = await _seeder.SeedUserAsync("Attendee");
            var organizer = await _seeder.SeedUserAsync("Organizer");

            var newEvent = CreateMockEvent(organizer.User_Id);
            Context.Events.Add(newEvent);
            await Context.SaveChangesAsync();

            var booking = CreateMockBooking(attendee.User_Id, newEvent.Event_Id);
            Context.Bookings.Add(booking);

            var transaction = CreateMockTransaction(attendee.User_Id);
            Context.Transactions.Add(transaction);
            await Context.SaveChangesAsync();

            return (attendee, organizer, newEvent, booking, transaction);
        }
        #endregion

        #region Create Tests
        [Test]
        public async Task Test_AddAsync()
        {
            var deps = await SeedDependenciesAsync();
            var payment = CreateMockPayment(deps.booking.Booking_Id, deps.transaction.Transaction_Id);

            try
            {
                await _repository.AddAsync(payment);
                Assert.That(payment.Booking_Payment_Id, Is.GreaterThan(0));
                LogTestDetail(Repo, "AddAsync", "Create booking payment", payment, payment, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddAsync", "Create booking payment", payment, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Read Tests
        [Test]
        public async Task Test_GetByIdAsync()
        {
            var deps = await SeedDependenciesAsync();
            var payment = CreateMockPayment(deps.booking.Booking_Id, deps.transaction.Transaction_Id);
            await _repository.AddAsync(payment);

            try
            {
                var fetched = await _repository.GetByIdAsync(payment.Booking_Payment_Id);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Amount, Is.EqualTo(150.00m));
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve booking payment by ID", payment.Booking_Payment_Id, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve booking payment by ID", payment.Booking_Payment_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetPaymentsByBookingIdAsync()
        {
            var deps = await SeedDependenciesAsync();
            var payment = CreateMockPayment(deps.booking.Booking_Id, deps.transaction.Transaction_Id);
            await _repository.AddAsync(payment);

            try
            {
                var payments = await _repository.GetPaymentsByBookingIdAsync(deps.booking.Booking_Id);
                Assert.That(payments.Count(), Is.EqualTo(1));
                LogTestDetail(Repo, "GetPaymentsByBookingIdAsync", "Retrieve payments for Booking ID", deps.booking.Booking_Id, payments, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetPaymentsByBookingIdAsync", "Retrieve payments for Booking ID", deps.booking.Booking_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetSuccessPaymentByBookingIdAsync()
        {
            var deps = await SeedDependenciesAsync();
            var payment = CreateMockPayment(deps.booking.Booking_Id, deps.transaction.Transaction_Id);
            await _repository.AddAsync(payment);

            try
            {
                var successPayment = await _repository.GetSuccessPaymentByBookingIdAsync(deps.booking.Booking_Id);
                Assert.That(successPayment, Is.Not.Null);
                Assert.That(successPayment!.Booking_Payment_Id, Is.EqualTo(payment.Booking_Payment_Id));
                LogTestDetail(Repo, "GetSuccessPaymentByBookingIdAsync", "Retrieve success payment for Booking ID", deps.booking.Booking_Id, successPayment, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetSuccessPaymentByBookingIdAsync", "Retrieve success payment for Booking ID", deps.booking.Booking_Id, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Update Tests
        [Test]
        public async Task Test_UpdateAsync()
        {
            var deps = await SeedDependenciesAsync();
            var payment = CreateMockPayment(deps.booking.Booking_Id, deps.transaction.Transaction_Id);
            await _repository.AddAsync(payment);

            try
            {
                payment.Payment_Status = "Refunded";
                await _repository.UpdateAsync(payment);
                var updated = await _repository.GetByIdAsync(payment.Booking_Payment_Id);
                Assert.That(updated!.Payment_Status, Is.EqualTo("Refunded"));
                LogTestDetail(Repo, "UpdateAsync", "Update payment status", payment, updated, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateAsync", "Update payment status", payment, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Delete Tests
        [Test]
        public async Task Test_DeleteAsync()
        {
            var deps = await SeedDependenciesAsync();
            var payment = CreateMockPayment(deps.booking.Booking_Id, deps.transaction.Transaction_Id);
            await _repository.AddAsync(payment);

            try
            {
                await _repository.DeleteAsync(payment);
                var deleted = await _repository.GetByIdAsync(payment.Booking_Payment_Id);
                Assert.That(deleted, Is.Null);
                LogTestDetail(Repo, "DeleteAsync", "Remove payment from database", payment, deleted, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "DeleteAsync", "Remove payment from database", payment, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
