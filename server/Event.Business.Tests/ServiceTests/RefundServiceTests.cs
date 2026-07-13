using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Event.Models;
using Event.Models.DTOs;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Business.Services;
using Event.Business.Exceptions;
using Moq;
using NUnit.Framework;

namespace Event.Business.Tests.ServiceTests
{
    [TestFixture]
    public class RefundServiceTests : ServiceTestBase
    {
        private Mock<IBookingRepository> _bookingRepositoryMock = null!;
        private Mock<IEventRepository> _eventRepositoryMock = null!;
        private Mock<ITransactionRepository> _transactionRepositoryMock = null!;
        private Mock<IBookingPaymentRepository> _bookingPaymentRepositoryMock = null!;
        private IPaymentService _paymentService = null!;
        private Mock<IServiceProvider> _serviceProviderMock = null!;
        private IEmailService _emailService = null!;
        private Mock<INotificationRepository> _notificationRepositoryMock = null!;

        private RefundService _refundService = null!;
        private const string Service = "RefundService";
        private const string TestEmail = "keshwarankeerthi@gmail.com";
        private const string TestName = "KeerthiKeswaran";

        [SetUp]
        public void SetUp()
        {
            _bookingRepositoryMock = new Mock<IBookingRepository>();
            _eventRepositoryMock = new Mock<IEventRepository>();
            _transactionRepositoryMock = new Mock<ITransactionRepository>();
            _bookingPaymentRepositoryMock = new Mock<IBookingPaymentRepository>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _notificationRepositoryMock = new Mock<INotificationRepository>();

            var configuration = CreateTestConfiguration();
            // _emailService = CreateConcreteEmailService(configuration);
            // _paymentService = CreateConcretePaymentService(configuration);
            _emailService = CreateMockEmailService();
            _paymentService = CreateMockPaymentService();

            var venueRepositoryMock = new Mock<IVenueRepository>();
            var staffRepositoryMock = new Mock<IStaffRepository>();

            // Register services in the IServiceProvider mock
            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IVenueRepository)))
                .Returns(venueRepositoryMock.Object);
            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IStaffRepository)))
                .Returns(staffRepositoryMock.Object);

            _refundService = new RefundService(
                _bookingRepositoryMock.Object,
                _eventRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                _bookingPaymentRepositoryMock.Object,
                _paymentService,
                _serviceProviderMock.Object,
                _emailService,
                _notificationRepositoryMock.Object
            );
        }

        #region Test_RefundAttendeeAsync_BookingNotFound_ThrowsNotFoundException
        [Test]
        public async Task Test_RefundAttendeeAsync_BookingNotFound_ThrowsNotFoundException()
        {
            // Arrange
            int bookingId = 10999;
            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync((Booking?)null);

            try
            {
                // Act & Assert
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _refundService.RefundAttendeeAsync(bookingId));

                LogTestDetail(Service, "RefundAttendeeAsync", "Throws NotFoundException for non-existent booking", new { BookingId = bookingId }, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundAttendeeAsync", "Throws NotFoundException for non-existent booking", new { BookingId = bookingId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundAttendeeAsync_Success_FullRefund
        [Test]
        public async Task Test_RefundAttendeeAsync_Success_FullRefund()
        {
            // Arrange
            int bookingId = 10001;
            var booking = new Booking
            {
                Booking_Id = bookingId,
                Booking_Status = "Confirmed",
                Attendee_Id = 10010,
                Attendee = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Event = new Event.Models.Event { Title = "Concert", Date_Time = DateTime.UtcNow.AddDays(3), Status = "Live" }
            };

            var originalPayment = new BookingPayment { Booking_Id = bookingId, Amount = 100m, Payment_Status = "Success" };
            var originalTx = new Transaction { Related_Id = bookingId, Amount = 100m, Refunded_Amount = 0m, Transaction_Reference = "ch_123" };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync(booking);
            _bookingRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);
            _bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(bookingId)).ReturnsAsync(originalPayment);
            _transactionRepositoryMock.Setup(r => r.GetSuccessBookingTransactionAsync(bookingId)).ReturnsAsync(originalTx);
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            try
            {
                // Act
                var (refundAmount, remarks) = await _refundService.RefundAttendeeAsync(bookingId, "Full");

                // Assert
                Assert.That(refundAmount, Is.EqualTo(100m));
                Assert.That(booking.Booking_Status, Is.EqualTo("Cancelled"));
                Assert.That(originalPayment.Payment_Status, Is.EqualTo("Refunded"));
                LogTestDetail(Service, "RefundAttendeeAsync", "Attendee refund: Full refund path", new { BookingId = bookingId, Type = "Full" }, new { RefundAmount = refundAmount, Remarks = remarks }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundAttendeeAsync", "Attendee refund: Full refund path", new { BookingId = bookingId, Type = "Full" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundAttendeeAsync_Success_NoRefund
        [Test]
        public async Task Test_RefundAttendeeAsync_Success_NoRefund()
        {
            // Arrange
            int bookingId = 10001;
            var booking = new Booking
            {
                Booking_Id = bookingId,
                Booking_Status = "Cancelled", // already cancelled
                Attendee_Id = 10010,
                Attendee = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Event = new Event.Models.Event { Title = "Concert", Date_Time = DateTime.UtcNow.AddDays(3), Status = "Live" }
            };

            var originalPayment = new BookingPayment { Booking_Id = bookingId, Amount = 100m, Payment_Status = "Success" };
            var originalTx = new Transaction { Related_Id = bookingId, Amount = 100m, Refunded_Amount = 0m };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync(booking);
            _bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(bookingId)).ReturnsAsync(originalPayment);
            _transactionRepositoryMock.Setup(r => r.GetSuccessBookingTransactionAsync(bookingId)).ReturnsAsync(originalTx);
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            try
            {
                // Act
                var (refundAmount, remarks) = await _refundService.RefundAttendeeAsync(bookingId, "NoRefund");

                // Assert
                Assert.That(refundAmount, Is.EqualTo(0m));
                LogTestDetail(Service, "RefundAttendeeAsync", "Attendee refund: NoRefund path", new { BookingId = bookingId, Type = "NoRefund" }, new { RefundAmount = refundAmount, Remarks = remarks }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundAttendeeAsync", "Attendee refund: NoRefund path", new { BookingId = bookingId, Type = "NoRefund" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundAttendeeAsync_Success_RemainingRefund
        [Test]
        public async Task Test_RefundAttendeeAsync_Success_RemainingRefund()
        {
            // Arrange
            int bookingId = 1;
            var booking = new Booking
            {
                Booking_Id = bookingId,
                Booking_Status = "Cancelled",
                Attendee_Id = 10010,
                Attendee = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Event = new Event.Models.Event { Title = "Concert", Date_Time = DateTime.UtcNow.AddDays(3), Status = "Live" }
            };

            var originalPayment = new BookingPayment { Booking_Id = bookingId, Amount = 100m, Payment_Status = "Success" };
            var originalTx = new Transaction { Related_Id = bookingId, Amount = 100m, Refunded_Amount = 40m, Transaction_Reference = "ch_123" };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync(booking);
            _bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(bookingId)).ReturnsAsync(originalPayment);
            _transactionRepositoryMock.Setup(r => r.GetSuccessBookingTransactionAsync(bookingId)).ReturnsAsync(originalTx);
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            try
            {
                // Act
                var (refundAmount, remarks) = await _refundService.RefundAttendeeAsync(bookingId, "Remaining");

                // Assert
                Assert.That(refundAmount, Is.EqualTo(60m));
                LogTestDetail(Service, "RefundAttendeeAsync", "Attendee refund: Remaining refund path", new { BookingId = bookingId, Type = "Remaining" }, new { RefundAmount = refundAmount, Remarks = remarks }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundAttendeeAsync", "Attendee refund: Remaining refund path", new { BookingId = bookingId, Type = "Remaining" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundAttendeeAsync_Dynamic_Over48Hours
        [Test]
        public async Task Test_RefundAttendeeAsync_Dynamic_Over48Hours()
        {
            // Arrange
            int bookingId = 10001;
            var booking = new Booking
            {
                Booking_Id = bookingId,
                Booking_Status = "Cancelled",
                Attendee_Id = 10010,
                Attendee = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Event = new Event.Models.Event { Date_Time = DateTime.UtcNow.AddHours(50) }
            };

            var originalPayment = new BookingPayment { Booking_Id = bookingId, Amount = 100m, Payment_Status = "Success" };
            var originalTx = new Transaction { Related_Id = bookingId, Amount = 100m, Refunded_Amount = 0m, Transaction_Reference = "ch_123" };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync(booking);
            _bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(bookingId)).ReturnsAsync(originalPayment);
            _transactionRepositoryMock.Setup(r => r.GetSuccessBookingTransactionAsync(bookingId)).ReturnsAsync(originalTx);
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            try
            {
                // Act
                var (refundAmount, remarks) = await _refundService.RefundAttendeeAsync(bookingId, "Dynamic");

                // Assert
                Assert.That(refundAmount, Is.EqualTo(90m));
                LogTestDetail(Service, "RefundAttendeeAsync", "Attendee refund: Dynamic > 48h (90%)", new { BookingId = bookingId }, new { RefundAmount = refundAmount, Remarks = remarks }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundAttendeeAsync", "Attendee refund: Dynamic > 48h (90%)", new { BookingId = bookingId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundAttendeeAsync_Dynamic_Between12And48Hours
        [Test]
        public async Task Test_RefundAttendeeAsync_Dynamic_Between12And48Hours()
        {
            // Arrange
            int bookingId = 10001;
            var booking = new Booking
            {
                Booking_Id = bookingId,
                Booking_Status = "Cancelled",
                Attendee_Id = 10010,
                Attendee = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Event = new Event.Models.Event { Date_Time = DateTime.UtcNow.AddHours(20) }
            };

            var originalPayment = new BookingPayment { Booking_Id = bookingId, Amount = 100m, Payment_Status = "Success" };
            var originalTx = new Transaction { Related_Id = bookingId, Amount = 100m, Refunded_Amount = 0m, Transaction_Reference = "ch_123" };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync(booking);
            _bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(bookingId)).ReturnsAsync(originalPayment);
            _transactionRepositoryMock.Setup(r => r.GetSuccessBookingTransactionAsync(bookingId)).ReturnsAsync(originalTx);
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            try
            {
                // Act
                var (refundAmount, remarks) = await _refundService.RefundAttendeeAsync(bookingId, "Dynamic");

                // Assert
                Assert.That(refundAmount, Is.EqualTo(50m));
                LogTestDetail(Service, "RefundAttendeeAsync", "Attendee refund: Dynamic 12-48h (50%)", new { BookingId = bookingId }, new { RefundAmount = refundAmount, Remarks = remarks }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundAttendeeAsync", "Attendee refund: Dynamic 12-48h (50%)", new { BookingId = bookingId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundAttendeeAsync_Dynamic_LessThan12Hours
        [Test]
        public async Task Test_RefundAttendeeAsync_Dynamic_LessThan12Hours()
        {
            // Arrange
            int bookingId = 10001;
            var booking = new Booking
            {
                Booking_Id = bookingId,
                Booking_Status = "Cancelled",
                Attendee_Id = 10010,
                Attendee = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Event = new Event.Models.Event { Date_Time = DateTime.UtcNow.AddHours(5) }
            };

            var originalPayment = new BookingPayment { Booking_Id = bookingId, Amount = 100m, Payment_Status = "Success" };
            var originalTx = new Transaction { Related_Id = bookingId, Amount = 100m, Refunded_Amount = 0m, Transaction_Reference = "ch_123" };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync(booking);
            _bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(bookingId)).ReturnsAsync(originalPayment);
            _transactionRepositoryMock.Setup(r => r.GetSuccessBookingTransactionAsync(bookingId)).ReturnsAsync(originalTx);
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            try
            {
                // Act
                var (refundAmount, remarks) = await _refundService.RefundAttendeeAsync(bookingId, "Dynamic");

                // Assert
                Assert.That(refundAmount, Is.EqualTo(0m));
                LogTestDetail(Service, "RefundAttendeeAsync", "Attendee refund: Dynamic < 12h (0%)", new { BookingId = bookingId }, new { RefundAmount = refundAmount, Remarks = remarks }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundAttendeeAsync", "Attendee refund: Dynamic < 12h (0%)", new { BookingId = bookingId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundAttendeeAsync_StripeRefundFailure
        [Test]
        public async Task Test_RefundAttendeeAsync_StripeRefundFailure()
        {
            // Arrange
            int bookingId = 10001;
            var booking = new Booking
            {
                Booking_Id = bookingId,
                Booking_Status = "Cancelled",
                Attendee_Id = 10010,
                Attendee = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Event = new Event.Models.Event { Date_Time = DateTime.UtcNow.AddDays(3) }
            };

            var originalPayment = new BookingPayment { Booking_Id = bookingId, Amount = 100m, Payment_Status = "Success" };
            var originalTx = new Transaction { Related_Id = bookingId, Amount = 100m, Refunded_Amount = 0m, Transaction_Reference = "ch_fail_declined" };

            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(bookingId)).ReturnsAsync(booking);
            _bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(bookingId)).ReturnsAsync(originalPayment);
            _transactionRepositoryMock.Setup(r => r.GetSuccessBookingTransactionAsync(bookingId)).ReturnsAsync(originalTx);
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            try
            {
                // Act
                var (refundAmount, remarks) = await _refundService.RefundAttendeeAsync(bookingId, "Full");

                // Assert
                Assert.That(refundAmount, Is.EqualTo(0m));
                Assert.That(remarks, Does.Contain("Stripe refund failed"));
                LogTestDetail(Service, "RefundAttendeeAsync", "Stripe payment refund failure handling", new { BookingId = bookingId }, new { RefundAmount = refundAmount, Remarks = remarks }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundAttendeeAsync", "Stripe payment refund failure handling", new { BookingId = bookingId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundOrganizerAsync_EventNotFound_ThrowsNotFoundException
        [Test]
        public async Task Test_RefundOrganizerAsync_EventNotFound_ThrowsNotFoundException()
        {
            // Arrange
            int eventId = 10999;
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync((Event.Models.Event?)null);

            try
            {
                // Act & Assert
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _refundService.RefundOrganizerAsync(eventId));

                LogTestDetail(Service, "RefundOrganizerAsync", "Throws NotFoundException for non-existent event", new { EventId = eventId }, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundOrganizerAsync", "Throws NotFoundException for non-existent event", new { EventId = eventId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundOrganizerAsync_Success_FullRefund
        [Test]
        public async Task Test_RefundOrganizerAsync_Success_FullRefund()
        {
            // Arrange
            int eventId = 10010;
            var ev = new Event.Models.Event
            {
                Event_Id = eventId,
                Title = "Gala Night",
                Status = "Live",
                Organizer_Id = 10005,
                Organizer = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Date_Time = DateTime.UtcNow.AddDays(5)
            };

            var upfrontTx = new Transaction
            {
                Related_Id = eventId,
                Transaction_Type = "OrganizerUpfrontPayment",
                Status = "Success",
                Amount = 500m,
                Refunded_Amount = 0m,
                Transaction_Reference = "ch_555"
            };

            var bookings = new List<Booking>
            {
                new Booking { Booking_Id = 10101, Booking_Status = "Confirmed", Event = ev }
            };

            var attendeeTx = new Transaction { Related_Id = 10101, Amount = 50m, Refunded_Amount = 0m };

            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(ev);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Event.Models.Event>())).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.GetTransactionsByUserIdAsync(10005)).ReturnsAsync(new List<Transaction> { upfrontTx });
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _transactionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

            _bookingRepositoryMock.Setup(r => r.GetBookingsByEventIdAsync(eventId)).ReturnsAsync(bookings);
            _transactionRepositoryMock.Setup(r => r.GetSuccessBookingTransactionAsync(10101)).ReturnsAsync(attendeeTx);
            _bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(10101)).ReturnsAsync(bookings[0]);
            _bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(10101)).ReturnsAsync(new BookingPayment { Amount = 50m });

            try
            {
                // Act
                var (orgRefundAmount, orgRemarks, attRefunds) = await _refundService.RefundOrganizerAsync(eventId, "Full");

                // Assert
                Assert.That(orgRefundAmount, Is.EqualTo(500m));
                Assert.That(ev.Status, Is.EqualTo("Cancelled"));
                LogTestDetail(Service, "RefundOrganizerAsync", "Event organizer cancellation and full refund path", new { EventId = eventId, Type = "Full" }, new { OrgRefund = orgRefundAmount, AttendeeRefunds = attRefunds }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundOrganizerAsync", "Event organizer cancellation and full refund path", new { EventId = eventId, Type = "Full" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundOrganizerAsync_Success_RemainingRefund
        [Test]
        public async Task Test_RefundOrganizerAsync_Success_RemainingRefund()
        {
            // Arrange
            int eventId = 10010;
            var ev = new Event.Models.Event
            {
                Event_Id = eventId,
                Title = "Gala Night",
                Status = "Cancelled",
                Organizer_Id = 10005,
                Organizer = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Date_Time = DateTime.UtcNow.AddDays(5)
            };

            var upfrontTx = new Transaction
            {
                Related_Id = eventId,
                Transaction_Type = "OrganizerUpfrontPayment",
                Status = "Success",
                Amount = 500m,
                Refunded_Amount = 200m,
                Transaction_Reference = "ch_555"
            };

            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(ev);
            _transactionRepositoryMock.Setup(r => r.GetTransactionsByUserIdAsync(10005)).ReturnsAsync(new List<Transaction> { upfrontTx });
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.GetBookingsByEventIdAsync(eventId)).ReturnsAsync(new List<Booking>());

            try
            {
                // Act
                var (orgRefundAmount, orgRemarks, attRefunds) = await _refundService.RefundOrganizerAsync(eventId, "Remaining");

                // Assert
                Assert.That(orgRefundAmount, Is.EqualTo(300m));
                LogTestDetail(Service, "RefundOrganizerAsync", "Organizer refund: Remaining path", new { EventId = eventId }, new { OrgRefund = orgRefundAmount }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundOrganizerAsync", "Organizer refund: Remaining path", new { EventId = eventId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundOrganizerAsync_Success_NoRefund
        [Test]
        public async Task Test_RefundOrganizerAsync_Success_NoRefund()
        {
            // Arrange
            int eventId = 10010;
            var ev = new Event.Models.Event
            {
                Event_Id = eventId,
                Title = "Gala Night",
                Status = "Cancelled",
                Organizer_Id = 10005,
                Organizer = new User { User_Id = 10001, Name = TestName, Email = TestEmail }
            };

            var upfrontTx = new Transaction
            {
                Related_Id = eventId,
                Transaction_Type = "OrganizerUpfrontPayment",
                Status = "Success",
                Amount = 500m,
                Refunded_Amount = 0m
            };

            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(ev);
            _transactionRepositoryMock.Setup(r => r.GetTransactionsByUserIdAsync(10005)).ReturnsAsync(new List<Transaction> { upfrontTx });
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.GetBookingsByEventIdAsync(eventId)).ReturnsAsync(new List<Booking>());

            try
            {
                // Act
                var (orgRefundAmount, orgRemarks, attRefunds) = await _refundService.RefundOrganizerAsync(eventId, "NoRefund");

                // Assert
                Assert.That(orgRefundAmount, Is.EqualTo(0m));
                LogTestDetail(Service, "RefundOrganizerAsync", "Organizer refund: NoRefund path", new { EventId = eventId }, new { OrgRefund = orgRefundAmount }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundOrganizerAsync", "Organizer refund: NoRefund path", new { EventId = eventId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundOrganizerAsync_Dynamic_Over48Hours
        [Test]
        public async Task Test_RefundOrganizerAsync_Dynamic_Over48Hours()
        {
            // Arrange
            int eventId = 10010;
            var ev = new Event.Models.Event
            {
                Event_Id = eventId,
                Title = "Gala Night",
                Status = "Cancelled",
                Organizer_Id = 10005,
                Organizer = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Date_Time = DateTime.UtcNow.AddHours(50)
            };

            var upfrontTx = new Transaction
            {
                Related_Id = eventId,
                Transaction_Type = "OrganizerUpfrontPayment",
                Status = "Success",
                Amount = 500m,
                Refunded_Amount = 0m,
                Transaction_Reference = "ch_555"
            };

            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(ev);
            _transactionRepositoryMock.Setup(r => r.GetTransactionsByUserIdAsync(10005)).ReturnsAsync(new List<Transaction> { upfrontTx });
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.GetBookingsByEventIdAsync(eventId)).ReturnsAsync(new List<Booking>());

            try
            {
                // Act
                var (orgRefundAmount, orgRemarks, attRefunds) = await _refundService.RefundOrganizerAsync(eventId, "Dynamic");

                // Assert
                Assert.That(orgRefundAmount, Is.EqualTo(450m));
                LogTestDetail(Service, "RefundOrganizerAsync", "Organizer refund: Dynamic > 48h (90%)", new { EventId = eventId }, new { OrgRefund = orgRefundAmount }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundOrganizerAsync", "Organizer refund: Dynamic > 48h (90%)", new { EventId = eventId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundOrganizerAsync_Dynamic_Between24And48Hours
        [Test]
        public async Task Test_RefundOrganizerAsync_Dynamic_Between24And48Hours()
        {
            // Arrange
            int eventId = 10010;
            var ev = new Event.Models.Event
            {
                Event_Id = eventId,
                Title = "Gala Night",
                Status = "Cancelled",
                Organizer_Id = 10005,
                Organizer = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Date_Time = DateTime.UtcNow.AddHours(30)
            };

            var upfrontTx = new Transaction
            {
                Related_Id = eventId,
                Transaction_Type = "OrganizerUpfrontPayment",
                Status = "Success",
                Amount = 500m,
                Refunded_Amount = 0m,
                Transaction_Reference = "ch_555"
            };

            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(ev);
            _transactionRepositoryMock.Setup(r => r.GetTransactionsByUserIdAsync(10005)).ReturnsAsync(new List<Transaction> { upfrontTx });
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.GetBookingsByEventIdAsync(eventId)).ReturnsAsync(new List<Booking>());

            try
            {
                // Act
                var (orgRefundAmount, orgRemarks, attRefunds) = await _refundService.RefundOrganizerAsync(eventId, "Dynamic");

                // Assert
                Assert.That(orgRefundAmount, Is.EqualTo(250m));
                LogTestDetail(Service, "RefundOrganizerAsync", "Organizer refund: Dynamic 24-48h (50%)", new { EventId = eventId }, new { OrgRefund = orgRefundAmount }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundOrganizerAsync", "Organizer refund: Dynamic 24-48h (50%)", new { EventId = eventId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundOrganizerAsync_Dynamic_LessThan24Hours
        [Test]
        public async Task Test_RefundOrganizerAsync_Dynamic_LessThan24Hours()
        {
            // Arrange
            int eventId = 10010;
            var ev = new Event.Models.Event
            {
                Event_Id = eventId,
                Title = "Gala Night",
                Status = "Cancelled",
                Organizer_Id = 10005,
                Organizer = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Date_Time = DateTime.UtcNow.AddHours(10)
            };

            var upfrontTx = new Transaction
            {
                Related_Id = eventId,
                Transaction_Type = "OrganizerUpfrontPayment",
                Status = "Success",
                Amount = 500m,
                Refunded_Amount = 0m,
                Transaction_Reference = "ch_555"
            };

            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(ev);
            _transactionRepositoryMock.Setup(r => r.GetTransactionsByUserIdAsync(10005)).ReturnsAsync(new List<Transaction> { upfrontTx });
            _transactionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
            _bookingRepositoryMock.Setup(r => r.GetBookingsByEventIdAsync(eventId)).ReturnsAsync(new List<Booking>());

            try
            {
                // Act
                var (orgRefundAmount, orgRemarks, attRefunds) = await _refundService.RefundOrganizerAsync(eventId, "Dynamic");

                // Assert
                Assert.That(orgRefundAmount, Is.EqualTo(0m));
                LogTestDetail(Service, "RefundOrganizerAsync", "Organizer refund: Dynamic < 24h (0%)", new { EventId = eventId }, new { OrgRefund = orgRefundAmount }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundOrganizerAsync", "Organizer refund: Dynamic < 24h (0%)", new { EventId = eventId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RefundOrganizerAsync_StripeRefundFailure
        [Test]
        public async Task Test_RefundOrganizerAsync_StripeRefundFailure()
        {
            // Arrange
            int eventId = 10010;
            var ev = new Event.Models.Event
            {
                Event_Id = eventId,
                Title = "Gala Night",
                Status = "Cancelled",
                Organizer_Id = 10005,
                Organizer = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                Date_Time = DateTime.UtcNow.AddDays(5)
            };

            var upfrontTx = new Transaction
            {
                Related_Id = eventId,
                Transaction_Type = "OrganizerUpfrontPayment",
                Status = "Success",
                Amount = 500m,
                Refunded_Amount = 0m,
                Transaction_Reference = "ch_fail_timeout"
            };

            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(ev);
            _transactionRepositoryMock.Setup(r => r.GetTransactionsByUserIdAsync(10005)).ReturnsAsync(new List<Transaction> { upfrontTx });
            _bookingRepositoryMock.Setup(r => r.GetBookingsByEventIdAsync(eventId)).ReturnsAsync(new List<Booking>());

            try
            {
                // Act
                var (orgRefundAmount, orgRemarks, attRefunds) = await _refundService.RefundOrganizerAsync(eventId, "Full");

                // Assert
                Assert.That(orgRefundAmount, Is.EqualTo(0m));
                Assert.That(orgRemarks, Does.Contain("Stripe refund failed"));
                LogTestDetail(Service, "RefundOrganizerAsync", "Stripe payment refund failure handling for organizer", new { EventId = eventId }, new { OrgRefund = orgRefundAmount, Remarks = orgRemarks }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RefundOrganizerAsync", "Stripe payment refund failure handling for organizer", new { EventId = eventId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
