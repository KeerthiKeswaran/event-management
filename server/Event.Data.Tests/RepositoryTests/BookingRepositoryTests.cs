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
    public class BookingRepositoryTests : RepositoryTestBase
    {
        private BookingRepository? _repository;
        private Seed.DbSeeder? _seeder;
        private const string Repo = "BookingRepository";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _repository = new BookingRepository(Context);
            _seeder = new Seed.DbSeeder(Context);
        }

        private Event.Models.Event CreateMockEvent(int organizerId)
        {
            return new Event.Models.Event
            {
                Organizer_Id = organizerId,
                Event_Type = "Virtual",
                Title = "Webinar " + new Random().Next(1000, 9999),
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

        private async Task<(User attendee, User organizer, Event.Models.Event newEvent)> SeedDependenciesAsync()
        {
            var attendee = await _seeder.SeedUserAsync("Attendee");
            var organizer = await _seeder.SeedUserAsync("Organizer");
            var newEvent = CreateMockEvent(organizer.User_Id);
            Context.Events.Add(newEvent);
            await Context.SaveChangesAsync();
            return (attendee, organizer, newEvent);
        }
        #endregion

        #region Create Tests
        [Test]
        public async Task Test_AddAsync()
        {
            var deps = await SeedDependenciesAsync();
            var booking = CreateMockBooking(deps.attendee.User_Id, deps.newEvent.Event_Id);

            try
            {
                await _repository.AddAsync(booking);
                Assert.That(booking.Booking_Id, Is.GreaterThan(0));
                LogTestDetail(Repo, "AddAsync", "Create new booking", booking, booking, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddAsync", "Create new booking", booking, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Read Tests
        [Test]
        public async Task Test_GetByIdAsync()
        {
            var deps = await SeedDependenciesAsync();
            var booking = CreateMockBooking(deps.attendee.User_Id, deps.newEvent.Event_Id);
            await _repository.AddAsync(booking);

            try
            {
                var fetched = await _repository.GetByIdAsync(booking.Booking_Id);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Booking_Status, Is.EqualTo("Payment Pending"));
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve booking by ID", booking.Booking_Id, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve booking by ID", booking.Booking_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetBookingsByUserIdAsync()
        {
            var deps = await SeedDependenciesAsync();
            var booking = CreateMockBooking(deps.attendee.User_Id, deps.newEvent.Event_Id);
            await _repository.AddAsync(booking);

            try
            {
                var userBookings = await _repository.GetBookingsByUserIdAsync(deps.attendee.User_Id);
                Assert.That(userBookings.Count(), Is.EqualTo(1));
                LogTestDetail(Repo, "GetBookingsByUserIdAsync", "Retrieve bookings for User ID", deps.attendee.User_Id, userBookings, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetBookingsByUserIdAsync", "Retrieve bookings for User ID", deps.attendee.User_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetBookingDetailsAsync()
        {
            var deps = await SeedDependenciesAsync();
            var booking = CreateMockBooking(deps.attendee.User_Id, deps.newEvent.Event_Id);
            await _repository.AddAsync(booking);

            var detail = new BookingDetail { Booking_Id = booking.Booking_Id, Tier_Name = "Elite", Quantity = 2 };
            Context.BookingDetails.Add(detail);
            await Context.SaveChangesAsync();

            try
            {
                var bookingDetails = await _repository.GetBookingDetailsAsync(booking.Booking_Id);
                Assert.That(bookingDetails, Is.Not.Null);
                Assert.That(bookingDetails.Details.Count(), Is.EqualTo(1));
                Assert.That(bookingDetails.Details.First().Tier_Name, Is.EqualTo("Elite"));
                LogTestDetail(Repo, "GetBookingDetailsAsync", "Retrieve booking details with relations", booking.Booking_Id, bookingDetails, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetBookingDetailsAsync", "Retrieve booking details with relations", booking.Booking_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetBookingsByEventIdAsync()
        {
            var deps = await SeedDependenciesAsync();
            var booking = CreateMockBooking(deps.attendee.User_Id, deps.newEvent.Event_Id);
            await _repository.AddAsync(booking);

            try
            {
                var eventBookings = await _repository.GetBookingsByEventIdAsync(deps.newEvent.Event_Id);
                Assert.That(eventBookings.Count(), Is.EqualTo(1));
                LogTestDetail(Repo, "GetBookingsByEventIdAsync", "Retrieve bookings for Event ID", deps.newEvent.Event_Id, eventBookings, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetBookingsByEventIdAsync", "Retrieve bookings for Event ID", deps.newEvent.Event_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetBookingBySecretHashAsync()
        {
            var deps = await SeedDependenciesAsync();
            var booking = CreateMockBooking(deps.attendee.User_Id, deps.newEvent.Event_Id);
            booking.Qr_Secret_Hash = "hashed_secret_key_999";
            await _repository.AddAsync(booking);

            try
            {
                var fetched = await _repository.GetBookingBySecretHashAsync("hashed_secret_key_999");
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched.Booking_Id, Is.EqualTo(booking.Booking_Id));
                LogTestDetail(Repo, "GetBookingBySecretHashAsync", "Retrieve booking by secret hash", "hashed_secret_key_999", fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetBookingBySecretHashAsync", "Retrieve booking by secret hash", "hashed_secret_key_999", null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Transaction Tests
        [Test]
        public async Task Test_TransactionCommit()
        {
            // Rollback the existing outer test transaction so we can test repository transaction management
            if (Context.Database.CurrentTransaction != null)
            {
                await Context.Database.CurrentTransaction.RollbackAsync();
            }
            Context.ChangeTracker.Clear();

            var deps = await SeedDependenciesAsync();
            var txBooking = CreateMockBooking(deps.attendee.User_Id, deps.newEvent.Event_Id);

            try
            {
                await _repository.BeginTransactionAsync();
                await _repository.AddAsync(txBooking);
                await _repository.CommitTransactionAsync();
                
                Context.ChangeTracker.Clear();
                var fetched = await _repository.GetByIdAsync(txBooking.Booking_Id);
                Assert.That(fetched, Is.Not.Null);
                LogTestDetail(Repo, "TransactionCommit", "Begin, Add and Commit Transaction", txBooking.Booking_Id, fetched, true);

                // Clean up the committed data to keep the database clean
                Context.ChangeTracker.Clear();
                var toDelete = await _repository.GetByIdAsync(txBooking.Booking_Id);
                if (toDelete != null)
                {
                    await _repository.DeleteAsync(toDelete);
                }
                
                var ev = await Context.Events.FindAsync(deps.newEvent.Event_Id);
                if (ev != null) Context.Events.Remove(ev);
                
                var att = await Context.Users.FindAsync(deps.attendee.User_Id);
                if (att != null) Context.Users.Remove(att);
                
                var org = await Context.Users.FindAsync(deps.organizer.User_Id);
                if (org != null) Context.Users.Remove(org);
                
                await Context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "TransactionCommit", "Begin, Add and Commit Transaction", txBooking.Booking_Id, null, false, ex.Message);
                throw;
            }
            finally
            {
                Context.ChangeTracker.Clear();
                // Re-open a transaction so the base class's Dispose rollback doesn't fail or warn
                await Context.Database.BeginTransactionAsync();
            }
        }

        [Test]
        public async Task Test_TransactionRollback()
        {
            // Rollback the existing outer test transaction so we can test repository transaction management
            if (Context.Database.CurrentTransaction != null)
            {
                await Context.Database.CurrentTransaction.RollbackAsync();
            }
            Context.ChangeTracker.Clear();

            var deps = await SeedDependenciesAsync();
            var txBooking = CreateMockBooking(deps.attendee.User_Id, deps.newEvent.Event_Id);
            await _repository.AddAsync(txBooking);

            try
            {
                await _repository.BeginTransactionAsync();
                
                Context.ChangeTracker.Clear();
                var toRemove = await _repository.GetByIdAsync(txBooking.Booking_Id);
                if (toRemove != null)
                {
                    await _repository.DeleteAsync(toRemove);
                }
                
                await _repository.RollbackTransactionAsync();
                
                Context.ChangeTracker.Clear();
                var fetched = await _repository.GetByIdAsync(txBooking.Booking_Id);
                Assert.That(fetched, Is.Not.Null); // Should still exist because delete was rolled back
                LogTestDetail(Repo, "TransactionRollback", "Begin, Delete and Rollback Transaction", txBooking.Booking_Id, fetched, true);

                // Clean up the added booking to keep database clean
                Context.ChangeTracker.Clear();
                var toDelete = await _repository.GetByIdAsync(txBooking.Booking_Id);
                if (toDelete != null)
                {
                    await _repository.DeleteAsync(toDelete);
                }
                
                var ev = await Context.Events.FindAsync(deps.newEvent.Event_Id);
                if (ev != null) Context.Events.Remove(ev);
                
                var att = await Context.Users.FindAsync(deps.attendee.User_Id);
                if (att != null) Context.Users.Remove(att);
                
                var org = await Context.Users.FindAsync(deps.organizer.User_Id);
                if (org != null) Context.Users.Remove(org);
                
                await Context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "TransactionRollback", "Begin, Delete and Rollback Transaction", txBooking.Booking_Id, null, false, ex.Message);
                throw;
            }
            finally
            {
                Context.ChangeTracker.Clear();
                // Re-open a transaction so the base class's Dispose rollback doesn't fail or warn
                await Context.Database.BeginTransactionAsync();
            }
        }
        #endregion

        #region Update Tests
        [Test]
        public async Task Test_UpdateAsync()
        {
            var deps = await SeedDependenciesAsync();
            var booking = CreateMockBooking(deps.attendee.User_Id, deps.newEvent.Event_Id);
            await _repository.AddAsync(booking);

            try
            {
                booking.Booking_Status = "Confirmed";
                await _repository.UpdateAsync(booking);
                var updated = await _repository.GetByIdAsync(booking.Booking_Id);
                Assert.That(updated!.Booking_Status, Is.EqualTo("Confirmed"));
                LogTestDetail(Repo, "UpdateAsync", "Update booking status", booking, updated, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateAsync", "Update booking status", booking, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Delete Tests
        [Test]
        public async Task Test_DeleteAsync()
        {
            var deps = await SeedDependenciesAsync();
            var booking = CreateMockBooking(deps.attendee.User_Id, deps.newEvent.Event_Id);
            await _repository.AddAsync(booking);

            try
            {
                await _repository.DeleteAsync(booking);
                var deleted = await _repository.GetByIdAsync(booking.Booking_Id);
                Assert.That(deleted, Is.Null);
                LogTestDetail(Repo, "DeleteAsync", "Remove booking from database", booking, deleted, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "DeleteAsync", "Remove booking from database", booking, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
