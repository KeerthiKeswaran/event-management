using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Event.Models;
using Event.Models.DTOs;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Business.Services;
using Event.Business.Exceptions;

namespace Event.Business.Tests.ServiceTests
{
    [TestFixture]
    public class BookingServiceTests : ServiceTestBase
    {
        private Mock<IBookingRepository> _bookingRepositoryMock = null!;
        private Mock<IEventRepository> _eventRepositoryMock = null!;
        private Mock<ITransactionRepository> _transactionRepositoryMock = null!;
        private Mock<IBookingPaymentRepository> _bookingPaymentRepositoryMock = null!;
        private Mock<IPlatformSettingsRepository> _settingsRepositoryMock = null!;
        private IPaymentService _paymentService = null!;
        private Mock<INotificationRepository> _notificationRepositoryMock = null!;
        private IRefundService _refundService = null!;

        private IConfiguration _configuration = null!;
        private IEmailService _emailService = null!;
        private IQrCodeService _qrCodeService = null!;
        private BookingService _bookingService = null!;
        private Mock<IServiceProvider> _serviceProviderMock = null!;
        private Mock<IWaitlistRepository> _waitlistRepoMock = null!;
        private Mock<IWaitlistService> _waitlistServiceMock = null!;

        private const string ServiceName = "BookingService";
        private const string TestEmail = "keshwarankeerthi@gmail.com";
        private const string TestName = "KeerthiKeswaran";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _bookingRepositoryMock = new Mock<IBookingRepository>();
            _eventRepositoryMock = new Mock<IEventRepository>();
            _transactionRepositoryMock = new Mock<ITransactionRepository>();
            _bookingPaymentRepositoryMock = new Mock<IBookingPaymentRepository>();
            _settingsRepositoryMock = new Mock<IPlatformSettingsRepository>();
            _notificationRepositoryMock = new Mock<INotificationRepository>();

            // Setup real configuration from the API project to get the Brevo credentials
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string apiDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Event.API"));
            string appSettingsPath = Path.Combine(apiDir, "appsettings.json");

            _configuration = new ConfigurationBuilder()
                .AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false)
                .Build();

            _emailService = new Mock<IEmailService>().Object;
            _paymentService = new Mock<IPaymentService>().Object;
            _qrCodeService = new Mock<IQrCodeService>().Object;

            _serviceProviderMock = new Mock<IServiceProvider>();
            _waitlistRepoMock = new Mock<IWaitlistRepository>();
            _waitlistServiceMock = new Mock<IWaitlistService>();

            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IWaitlistRepository))).Returns(_waitlistRepoMock.Object);
            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IWaitlistService))).Returns(_waitlistServiceMock.Object);
            _emailService = CreateMockEmailService();
            _paymentService = CreateMockPaymentService();

            _notificationRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .Returns(Task.CompletedTask);

            _notificationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Notification>()))
                .Returns(Task.CompletedTask);

            _refundService = new RefundService(
                _bookingRepositoryMock.Object,
                _eventRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                _bookingPaymentRepositoryMock.Object,
                _paymentService,
                new Mock<IServiceProvider>().Object,
                _emailService,
                _notificationRepositoryMock.Object
            );

            _bookingService = new BookingService(
                _bookingRepositoryMock.Object,
                _eventRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                _bookingPaymentRepositoryMock.Object,
                _settingsRepositoryMock.Object,
                _paymentService,
                _qrCodeService,
                _configuration,
                _emailService,
                _notificationRepositoryMock.Object,
                _refundService,
                _serviceProviderMock.Object
            );
        }
        #endregion

        #region BookTicketsAsync Tests
        [Test]
        public async Task Test_BookTicketsAsync_Success()
        {
            int attendeeId = 10001;
            int eventId = 10100;
            var tierQuantities = new Dictionary<string, int> { { "VIP", 2 } };

            var mockSettings = new PlatformSettings { Max_Tickets_Per_Booking = 5 };
            var mockEvent = new Event.Models.Event
            {
                Event_Id = eventId,
                Title = "Annual Gala",
                Status = "Live",
                Event_Type = "Virtual",
                TicketTiers = new List<EventTicketTier>
                {
                    new EventTicketTier { Tier_Name = "VIP", Price = 150.00m, Tickets_Sold = 0 }
                }
            };

            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(mockSettings);
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(mockEvent);
            _bookingRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(mockEvent)).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            try
            {
                var booking = await _bookingService.BookTicketsAsync(attendeeId, eventId, tierQuantities);
                Assert.That(booking, Is.Not.Null);
                
                LogTestDetail(ServiceName, "BookTicketsAsync", "Book tickets for live event successfully", new { attendeeId, eventId, tierQuantities }, booking, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(ServiceName, "BookTicketsAsync", "Book tickets for live event successfully", new { attendeeId, eventId, tierQuantities }, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_BookTicketsAsync_ExceedsMaxTickets_ThrowsValidationException()
        {
            int attendeeId = 10001;
            int eventId = 10100;
            var tierQuantities = new Dictionary<string, int> { { "VIP", 10 } }; // Exceeds limit

            var mockSettings = new PlatformSettings { Max_Tickets_Per_Booking = 5 };
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(mockSettings);

            try
            {
                Assert.ThrowsAsync<ValidationException>(async () =>
                    await _bookingService.BookTicketsAsync(attendeeId, eventId, tierQuantities)
                );
                LogTestDetail(ServiceName, "BookTicketsAsync", "Booking exceeds max platform limit throws exception", new { attendeeId, eventId, tierQuantities }, "ValidationException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(ServiceName, "BookTicketsAsync", "Booking exceeds max platform limit throws exception", new { attendeeId, eventId, tierQuantities }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region ConfirmBookingPaymentAsync Tests
        [Test]
        public async Task Test_ConfirmBookingPaymentAsync_Success()
        {
            int bookingId = 10500;
            var attendee = new User
            {
                User_Id = 10001,
                Name = "KeerthiKeswaran",
                Email = "keshwarankeerthi@gmail.com"
            };

            var mockBooking = new Booking
            {
                Booking_Id = bookingId,
                Booking_Status = "Payment Pending",
                Attendee_Id = attendee.User_Id,
                Attendee = attendee,
                Event = new Event.Models.Event { Title = "Keerthi Test Event" },
                Details = new List<BookingDetail>
                {
                    new BookingDetail { Tier_Name = "VIP", Quantity = 1 }
                }
            };

            var mockTx = new Transaction
            {
                Transaction_Id = 1000000000000123L,
                Amount = 150.00m,
                Currency = "USD",
                Status = "Pending"
            };

            var mockSettings = new PlatformSettings
            {
                Ticket_Commission_Percentage = 5.0m,
                Ticket_Fixed_Fee = 0.99m
            };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync(mockBooking);
            _transactionRepositoryMock.Setup(r => r.GetPendingBookingTransactionAsync(bookingId)).ReturnsAsync(mockTx);
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(mockSettings);

            _bookingPaymentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<BookingPayment>())).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            _notificationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);

            try
            {
                var result = await _bookingService.ConfirmBookingPaymentAsync(bookingId, "tok_visa", "StripeCard");
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Qr_Code_Path, Is.Not.Empty);
                LogTestDetail(ServiceName, "ConfirmBookingPaymentAsync", "Confirm booking payment with real email delivery", new { bookingId, stripeToken = "tok_visa" }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(ServiceName, "ConfirmBookingPaymentAsync", "Confirm booking payment with real email delivery", new { bookingId, stripeToken = "tok_visa" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region GetMyBookingsAsync Tests
        [Test]
        public async Task Test_GetMyBookingsAsync_Success()
        {
            int attendeeId = 10001;
            var mockBookings = new List<Booking>
            {
                new Booking { Booking_Id = 10001, Attendee_Id = attendeeId }
            };

            _bookingRepositoryMock.Setup(r => r.GetBookingsByUserIdAsync(attendeeId)).ReturnsAsync(mockBookings);

            try
            {
                var result = await _bookingService.GetMyBookingsAsync(attendeeId);
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count(), Is.EqualTo(1));
                LogTestDetail(ServiceName, "GetMyBookingsAsync", "Retrieve user bookings", attendeeId, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(ServiceName, "GetMyBookingsAsync", "Retrieve user bookings", attendeeId, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region CancelBookingAsync Tests
        [Test]
        public async Task Test_CancelBookingAsync_Success()
        {
            int bookingId = 10001;
            var attendee = new User
            {
                User_Id = 10001,
                Name = "KeerthiKeswaran",
                Email = "keshwarankeerthi@gmail.com"
            };

            var mockBooking = new Booking
            {
                Booking_Id = bookingId,
                Booking_Status = "Confirmed",
                Attendee_Id = attendee.User_Id,
                Attendee = attendee,
                Event = new Event.Models.Event
                {
                    Title = "Tech Summit",
                    TicketTiers = new List<EventTicketTier> { new EventTicketTier { Tier_Name = "VIP", Tickets_Sold = 1 } }
                },
                Details = new List<BookingDetail>
                {
                    new BookingDetail { Tier_Name = "VIP", Quantity = 1 }
                }
            };

            var mockTx = new Transaction
            {
                Transaction_Id = 1000000000000099L,
                Amount = 100.00m,
                Currency = "USD",
                Status = "Success"
            };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync(mockBooking);
            _transactionRepositoryMock.Setup(r => r.GetSuccessBookingTransactionAsync(bookingId)).ReturnsAsync(mockTx);
            _bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(bookingId))
                .ReturnsAsync(new BookingPayment { Amount = 100.00m, Payment_Status = "Success" });
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Event.Models.Event>())).Returns(Task.CompletedTask);

            _notificationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Notification>())).Returns(Task.CompletedTask);

            try
            {
                var success = await _bookingService.CancelBookingAsync(bookingId);
                Assert.That(success, Is.True);
                LogTestDetail(ServiceName, "CancelBookingAsync", "Cancel a confirmed booking", bookingId, success, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(ServiceName, "CancelBookingAsync", "Cancel a confirmed booking", bookingId, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region ReleaseExpiredEventBookingAsync Tests
        [Test]
        public async Task Test_ReleaseExpiredEventBookingAsync_Success()
        {
            var mockExpiredBookings = new List<Booking>
            {
                new Booking
                {
                    Booking_Id = 10202,
                    Booking_Status = "Payment Pending",
                    Event = new Event.Models.Event
                    {
                        Title = "Past Event",
                        TicketTiers = new List<EventTicketTier> { new EventTicketTier { Tier_Name = "General", Tickets_Sold = 5 } }
                    },
                    Details = new List<BookingDetail> { new BookingDetail { Tier_Name = "General", Quantity = 2 } }
                }
            };

            _bookingRepositoryMock.Setup(r => r.GetExpiredBookingsAsync(It.IsAny<DateTime>())).ReturnsAsync(mockExpiredBookings);
            _transactionRepositoryMock.Setup(r => r.GetPendingBookingTransactionAsync(10202)).ReturnsAsync(new Transaction { Status = "Pending" });

            _bookingRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Event.Models.Event>())).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            try
            {
                await _bookingService.ReleaseExpiredEventBookingAsync();
                LogTestDetail(ServiceName, "ReleaseExpiredEventBookingAsync", "Releases all expired event bookings", null, "Completed", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(ServiceName, "ReleaseExpiredEventBookingAsync", "Releases all expired event bookings", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region BookTicketsAsync Additional Tests
        [Test]
        public void Test_BookTicketsAsync_EventNotFound_ThrowsNotFoundException()
        {
            int attendeeId = 10001;
            int eventId = 10999;
            var tierQuantities = new Dictionary<string, int> { { "VIP", 2 } };

            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Max_Tickets_Per_Booking = 5 });
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync((Event.Models.Event?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _bookingService.BookTicketsAsync(attendeeId, eventId, tierQuantities)
            );
        }

        [Test]
        public void Test_BookTicketsAsync_EventNotLive_ThrowsValidationException()
        {
            int attendeeId = 10001;
            int eventId = 10100;
            var tierQuantities = new Dictionary<string, int> { { "VIP", 2 } };

            var mockEvent = new Event.Models.Event { Event_Id = eventId, Status = "Draft" };

            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Max_Tickets_Per_Booking = 5 });
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(mockEvent);

            Assert.ThrowsAsync<ValidationException>(async () =>
                await _bookingService.BookTicketsAsync(attendeeId, eventId, tierQuantities)
            );
        }

        [Test]
        public void Test_BookTicketsAsync_TierNotFound_ThrowsValidationException()
        {
            int attendeeId = 10001;
            int eventId = 10100;
            var tierQuantities = new Dictionary<string, int> { { "NonExistentTier", 2 } };

            var mockEvent = new Event.Models.Event
            {
                Event_Id = eventId,
                Status = "Live",
                TicketTiers = new List<EventTicketTier> { new EventTicketTier { Tier_Name = "VIP" } }
            };

            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Max_Tickets_Per_Booking = 5 });
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(mockEvent);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _bookingService.BookTicketsAsync(attendeeId, eventId, tierQuantities)
            );
        }

        [Test]
        public void Test_BookTicketsAsync_QuantityExceedsCapacity_ThrowsValidationException()
        {
            int attendeeId = 10001;
            int eventId = 10100;
            var tierQuantities = new Dictionary<string, int> { { "VIP", 3 } };

            var mockVenue = new Venue
            {
                SeatCapacities = new List<VenueSeatCapacity>
                {
                    new VenueSeatCapacity { Tier_Name = "VIP", Total_Seats = 5 }
                }
            };

            var mockEvent = new Event.Models.Event
            {
                Event_Id = eventId,
                Status = "Live",
                Event_Type = "Physical",
                Venue = mockVenue,
                TicketTiers = new List<EventTicketTier>
                {
                    new EventTicketTier { Tier_Name = "VIP", Tickets_Sold = 4 } // Only 1 ticket left
                }
            };

            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(new PlatformSettings { Max_Tickets_Per_Booking = 5 });
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(mockEvent);

            Assert.ThrowsAsync<ConflictException>(async () =>
                await _bookingService.BookTicketsAsync(attendeeId, eventId, tierQuantities)
            );
        }
        #endregion

        #region ConfirmBookingPaymentAsync Additional Tests
        [Test]
        public void Test_ConfirmBookingPaymentAsync_BookingNotFound_ThrowsNotFoundException()
        {
            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(10999)).ReturnsAsync((Booking?)null);
            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _bookingService.ConfirmBookingPaymentAsync(10999, "tok_visa", "Card")
            );
        }

        [Test]
        public void Test_ConfirmBookingPaymentAsync_StatusNotPending_ThrowsValidationException()
        {
            var booking = new Booking { Booking_Status = "Confirmed" };
            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(10100)).ReturnsAsync(booking);

            Assert.ThrowsAsync<ValidationException>(async () =>
                await _bookingService.ConfirmBookingPaymentAsync(10100, "tok_visa", "Card")
            );
        }

        [Test]
        public void Test_ConfirmBookingPaymentAsync_TxNotFound_ThrowsNotFoundException()
        {
            var booking = new Booking { Booking_Status = "Payment Pending" };
            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(10100)).ReturnsAsync(booking);
            _transactionRepositoryMock.Setup(r => r.GetPendingBookingTransactionAsync(10100)).ReturnsAsync((Transaction?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _bookingService.ConfirmBookingPaymentAsync(10100, "tok_visa", "Card")
            );
        }

        [Test]
        public void Test_ConfirmBookingPaymentAsync_ChargeFails_ThrowsValidationException()
        {
            var booking = new Booking
            {
                Booking_Status = "Payment Pending",
                Attendee = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Event = new Event.Models.Event { Title = "Gala" }
            };
            var tx = new Transaction { Status = "Pending", Amount = 100m, Currency = "USD" };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(10100)).ReturnsAsync(booking);
            _transactionRepositoryMock.Setup(r => r.GetPendingBookingTransactionAsync(10100)).ReturnsAsync(tx);

            Assert.ThrowsAsync<ValidationException>(async () =>
                await _bookingService.ConfirmBookingPaymentAsync(10100, "tok_fail", "Card")
            );
            Assert.That(tx.Status, Is.EqualTo("Failed"));
        }

        [Test]
        public void Test_ConfirmBookingPaymentAsync_DbError_RollbacksTransaction()
        {
            var booking = new Booking
            {
                Booking_Status = "Payment Pending",
                Attendee = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Event = new Event.Models.Event { Title = "Gala" }
            };
            var tx = new Transaction { Status = "Pending", Amount = 100m, Currency = "USD" };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(10100)).ReturnsAsync(booking);
            _transactionRepositoryMock.Setup(r => r.GetPendingBookingTransactionAsync(10100)).ReturnsAsync(tx);
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.UpdateAsync(tx)).ThrowsAsync(new Exception("DB Down"));
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            Assert.ThrowsAsync<ValidationException>(async () =>
                await _bookingService.ConfirmBookingPaymentAsync(10100, "tok_visa", "Card")
            );
            _bookingRepositoryMock.Verify(r => r.RollbackTransactionAsync(), Times.Once);
        }
        #endregion

        #region CancelBookingAsync Additional Tests
        [Test]
        public void Test_CancelBookingAsync_BookingNotFound_ThrowsNotFoundException()
        {
            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(10999)).ReturnsAsync((Booking?)null);
            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _bookingService.CancelBookingAsync(10999)
            );
        }

        [Test]
        public void Test_CancelBookingAsync_AlreadyCancelled_ThrowsValidationException()
        {
            var booking = new Booking { Booking_Status = "Cancelled" };
            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(10100)).ReturnsAsync(booking);

            Assert.ThrowsAsync<ValidationException>(async () =>
                await _bookingService.CancelBookingAsync(10100)
            );
        }

        [Test]
        public void Test_CancelBookingAsync_DbError_RollbacksTransaction()
        {
            var booking = new Booking { Booking_Status = "Confirmed" };
            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(10100)).ReturnsAsync(booking);
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(10100)).ThrowsAsync(new Exception("Refund failed"));
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            Assert.ThrowsAsync<ValidationException>(async () =>
                await _bookingService.CancelBookingAsync(10100)
            );
            _bookingRepositoryMock.Verify(r => r.RollbackTransactionAsync(), Times.Once);
        }
        #endregion

        #region RevertPendingBookingAsync Tests
        [Test]
        public async Task Test_RevertPendingBookingAsync_Success()
        {
            var ev = new Event.Models.Event
            {
                TicketTiers = new List<EventTicketTier>
                {
                    new EventTicketTier { Tier_Name = "VIP", Tickets_Sold = 5 }
                }
            };
            var booking = new Booking
            {
                Booking_Id = 10100,
                Booking_Status = "Payment Pending",
                Event = ev,
                Details = new List<BookingDetail>
                {
                    new BookingDetail { Tier_Name = "VIP", Quantity = 2 }
                }
            };
            var tx = new Transaction { Status = "Pending" };

            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(10100)).ReturnsAsync(booking);
            _transactionRepositoryMock.Setup(r => r.GetPendingBookingTransactionAsync(10100)).ReturnsAsync(tx);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(ev)).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.UpdateAsync(booking)).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.UpdateAsync(tx)).Returns(Task.CompletedTask);

            try
            {
                var result = await _bookingService.RevertPendingBookingAsync(10100);
                Assert.That(result, Is.True);
                Assert.That(booking.Booking_Status, Is.EqualTo("Payment Failed"));
                Assert.That(tx.Status, Is.EqualTo("Failed"));
                Assert.That(ev.TicketTiers.First().Tickets_Sold, Is.EqualTo(3)); // 5 - 2
                LogTestDetail(ServiceName, "RevertPendingBookingAsync", "Reverts booking in pending state successfully", 10100, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(ServiceName, "RevertPendingBookingAsync", "Reverts booking in pending state successfully", 10100, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_RevertPendingBookingAsync_BookingNotFound_ThrowsNotFoundException()
        {
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(10999)).ReturnsAsync((Booking?)null);
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _bookingService.RevertPendingBookingAsync(10999)
            );
        }

        [Test]
        public void Test_RevertPendingBookingAsync_StatusNotPending_ThrowsValidationException()
        {
            var booking = new Booking { Booking_Status = "Confirmed" };
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(10100)).ReturnsAsync(booking);
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            Assert.ThrowsAsync<ValidationException>(async () =>
                await _bookingService.RevertPendingBookingAsync(10100)
            );
        }

        [Test]
        public void Test_RevertPendingBookingAsync_DbError_RollbacksTransaction()
        {
            var booking = new Booking { Booking_Status = "Payment Pending" };
            _bookingRepositoryMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(10100)).ReturnsAsync(booking);
            _transactionRepositoryMock.Setup(r => r.GetPendingBookingTransactionAsync(10100)).ThrowsAsync(new Exception("DB Timeout"));
            _bookingRepositoryMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            Assert.ThrowsAsync<Exception>(async () =>
                await _bookingService.RevertPendingBookingAsync(10100)
            );
            _bookingRepositoryMock.Verify(r => r.RollbackTransactionAsync(), Times.Once);
        }
        #endregion

        #region Virtual Passcode Tests
        [Test]
        public async Task Test_ConfirmBookingPaymentAsync_HybridEvent_SharesPasscodeAndUrl()
        {
            int bookingId = 10600;
            var attendee = new User
            {
                User_Id = 10001,
                Name = "Test Attendee",
                Email = "attendee@example.com"
            };

            var mockBooking = new Booking
            {
                Booking_Id = bookingId,
                Booking_Status = "Payment Pending",
                Attendee_Id = attendee.User_Id,
                Attendee = attendee,
                Event_Id = 200,
                Event = new Event.Models.Event
                {
                    Event_Id = 200,
                    Title = "Hybrid Masterclass",
                    Event_Type = "Hybrid",
                    Virtual_Url = "https://meet.jit.si/hybrid-masterclass",
                    Virtual_Password_Hash = "hashed_passcode"
                },
                Details = new List<BookingDetail>
                {
                    new BookingDetail { Tier_Name = "Virtual", Quantity = 2 }
                }
            };

            var mockTx = new Transaction
            {
                Transaction_Id = 1000000000000999L,
                Amount = 300.00m,
                Currency = "INR",
                Status = "Pending"
            };

            var mockSettings = new PlatformSettings
            {
                Ticket_Commission_Percentage = 5.0m,
                Ticket_Fixed_Fee = 0.0m
            };

            var mockUpfrontTx = new Transaction
            {
                Transaction_Id = 1000000000000888L,
                Status = "Success",
                Transaction_Type = "OrganizerUpfrontPayment",
                Related_Id = 200,
                Remarks = "Upfront payment \n[Virtual Access Passcode]: secretpasscode123"
            };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync(mockBooking);
            _transactionRepositoryMock.Setup(r => r.GetPendingBookingTransactionAsync(bookingId)).ReturnsAsync(mockTx);
            _settingsRepositoryMock.Setup(r => r.GetSettingsAsync()).ReturnsAsync(mockSettings);
            _transactionRepositoryMock.Setup(r => r.GetSuccessOrganizerUpfrontTransactionAsync(200)).ReturnsAsync(mockUpfrontTx);

            _bookingPaymentRepositoryMock.Setup(r => r.AddAsync(It.IsAny<BookingPayment>())).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            var result = await _bookingService.ConfirmBookingPaymentAsync(bookingId, "tok_visa", "StripeCard");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Virtual_Url, Is.EqualTo("Disabled"));
            LogTestDetail(ServiceName, "ConfirmBookingPaymentAsync", "Confirm booking for Hybrid event maps passcode and url", bookingId, result, true);
        }

        [Test]
        public async Task Test_GetMyBookingsAsync_ReturnsVirtualPasswordHash()
        {
            var mockBookings = new List<Booking>
            {
                new Booking
                {
                    Booking_Id = 10700,
                    Attendee_Id = 10002,
                    Event_Id = 201,
                    Event = new Event.Models.Event
                    {
                        Event_Id = 201,
                        Title = "Virtual Webinar",
                        Event_Type = "Virtual",
                        Virtual_Password_Hash = "webinar_hash"
                    },
                    Virtual_Url = "https://meet.jit.si/webinar"
                }
            };

            _bookingRepositoryMock.Setup(r => r.GetBookingsByUserIdAsync(10002)).ReturnsAsync(mockBookings);

            var result = await _bookingService.GetMyBookingsAsync(10002);
            Assert.That(result, Is.Not.Null);
            var booking = result.FirstOrDefault();
            Assert.That(booking, Is.Not.Null);
            Assert.That(booking.Virtual_Url, Is.EqualTo("Disabled"));
            LogTestDetail(ServiceName, "GetMyBookingsAsync", "Get bookings returns passcode hash and url", 10002, result, true);
        }

        [Test]
        public async Task Test_GetMyBookingsAsync_FiltersByStatus()
        {
            var mockBookings = new List<Booking>
            {
                new Booking
                {
                    Booking_Id = 10700,
                    Attendee_Id = 10002,
                    Booking_Status = "Confirmed",
                    Event = new Event.Models.Event { Title = "Event 1" }
                },
                new Booking
                {
                    Booking_Id = 10701,
                    Attendee_Id = 10002,
                    Booking_Status = "Cancelled",
                    Event = new Event.Models.Event { Title = "Event 2" }
                }
            };

            _bookingRepositoryMock.Setup(r => r.GetBookingsByUserIdAsync(10002)).ReturnsAsync(mockBookings);

            var confirmedResult = await _bookingService.GetMyBookingsAsync(10002, "Confirmed");
            Assert.That(confirmedResult, Is.Not.Null);
            Assert.That(confirmedResult.Count(), Is.EqualTo(1));
            Assert.That(confirmedResult.First().Booking_Id, Is.EqualTo(10700));

            var cancelledResult = await _bookingService.GetMyBookingsAsync(10002, "cancelled");
            Assert.That(cancelledResult, Is.Not.Null);
            Assert.That(cancelledResult.Count(), Is.EqualTo(1));
            Assert.That(cancelledResult.First().Booking_Id, Is.EqualTo(10701));

            LogTestDetail(ServiceName, "GetMyBookingsAsync", "Get bookings filters by status successfully", 10002, confirmedResult, true);
        }
        #endregion

        #region GetBookingRefundDetailsAsync Tests
        [Test]
        public async Task Test_GetBookingRefundDetailsAsync_Success()
        {
            int bookingId = 12001;
            var eventTime = DateTime.UtcNow.AddHours(24);
            var mockBooking = new Booking
            {
                Booking_Id = bookingId,
                Event_Id = 101,
                Event = new Event.Models.Event { Date_Time = eventTime }
            };
            var mockPayment = new BookingPayment
            {
                Booking_Id = bookingId,
                Amount = 250.00m,
                Payment_Status = "Success"
            };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync(mockBooking);
            _bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(bookingId)).ReturnsAsync(mockPayment);

            try
            {
                var (returnedEventTime, returnedAmount) = await _bookingService.GetBookingRefundDetailsAsync(bookingId);
                Assert.That(returnedEventTime, Is.EqualTo(eventTime));
                Assert.That(returnedAmount, Is.EqualTo(250.00m));
                LogTestDetail(ServiceName, "GetBookingRefundDetailsAsync", "Get refund details successfully", bookingId, new { returnedEventTime, returnedAmount }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(ServiceName, "GetBookingRefundDetailsAsync", "Get refund details successfully", bookingId, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_GetBookingRefundDetailsAsync_BookingNotFound_ThrowsNotFoundException()
        {
            int bookingId = 12002;
            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync((Booking?)null);

            try
            {
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _bookingService.GetBookingRefundDetailsAsync(bookingId)
                );
                LogTestDetail(ServiceName, "GetBookingRefundDetailsAsync", "Booking not found throws NotFoundException", bookingId, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(ServiceName, "GetBookingRefundDetailsAsync", "Booking not found throws NotFoundException", bookingId, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_GetBookingRefundDetailsAsync_EventNotFound_ThrowsNotFoundException()
        {
            int bookingId = 12003;
            var mockBooking = new Booking
            {
                Booking_Id = bookingId,
                Event_Id = 101,
                Event = null
            };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync(mockBooking);

            try
            {
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _bookingService.GetBookingRefundDetailsAsync(bookingId)
                );
                LogTestDetail(ServiceName, "GetBookingRefundDetailsAsync", "Event null throws NotFoundException", bookingId, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(ServiceName, "GetBookingRefundDetailsAsync", "Event null throws NotFoundException", bookingId, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_GetBookingRefundDetailsAsync_PaymentNotFound_ThrowsValidationException()
        {
            int bookingId = 12004;
            var mockBooking = new Booking
            {
                Booking_Id = bookingId,
                Event_Id = 101,
                Event = new Event.Models.Event { Date_Time = DateTime.UtcNow.AddHours(2) }
            };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync(mockBooking);
            _bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(bookingId)).ReturnsAsync((BookingPayment?)null);

            try
            {
                Assert.ThrowsAsync<ValidationException>(async () =>
                    await _bookingService.GetBookingRefundDetailsAsync(bookingId)
                );
                LogTestDetail(ServiceName, "GetBookingRefundDetailsAsync", "Payment null throws ValidationException", bookingId, "ValidationException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(ServiceName, "GetBookingRefundDetailsAsync", "Payment null throws ValidationException", bookingId, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
