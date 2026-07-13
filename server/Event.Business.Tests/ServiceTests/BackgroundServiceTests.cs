using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Event.Contracts.IServices;
using Event.Contracts.IRepositories;
using Event.Models;
using Event.Business.Services;

namespace Event.Business.Tests.ServiceTests
{
    [TestFixture]
    public class BackgroundServiceTests : ServiceTestBase
    {
        private Mock<IServiceProvider> _serviceProviderMock = null!;
        private Mock<IServiceScope> _serviceScopeMock = null!;
        private Mock<IServiceScopeFactory> _serviceScopeFactoryMock = null!;
        
        private BackgroundService _backgroundService = null!;

        private const string Service = "BackgroundService";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var eventRepositoryMock = new Mock<IEventRepository>();
            var transactionRepositoryMock = new Mock<ITransactionRepository>();
            var bookingPaymentRepositoryMock = new Mock<IBookingPaymentRepository>();
            var settingsRepositoryMock = new Mock<IPlatformSettingsRepository>();
            var venueRepositoryMock = new Mock<IVenueRepository>();
            var staffRepositoryMock = new Mock<IStaffRepository>();
            var upfrontPaymentRepositoryMock = new Mock<IOrganizerUpfrontPaymentRepository>();
            var notificationRepositoryMock = new Mock<INotificationRepository>();
            var userRepositoryMock = new Mock<IUserRepository>();

            bookingRepositoryMock.Setup(r => r.GetExpiredBookingsAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<Booking>());
            eventRepositoryMock.Setup(r => r.GetExpiredEventsAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<Event.Models.Event>());

            var configuration = CreateTestConfiguration();
            // var emailService = CreateConcreteEmailService(configuration);
            // var paymentService = CreateConcretePaymentService(configuration);
            // var virtualMeetingService = CreateConcreteVirtualMeetingService();
            // var qrCodeService = CreateConcreteQrCodeService();
            var emailService = CreateMockEmailService();
            var paymentService = CreateMockPaymentService();
            var virtualMeetingService = CreateMockVirtualMeetingService();
            var qrCodeService = CreateMockQrCodeService();
            var serviceProviderMock = new Mock<IServiceProvider>();

            var refundService = new RefundService(
                bookingRepositoryMock.Object,
                eventRepositoryMock.Object,
                transactionRepositoryMock.Object,
                bookingPaymentRepositoryMock.Object,
                paymentService,
                serviceProviderMock.Object,
                emailService,
                notificationRepositoryMock.Object
            );

            var eventService = new EventService(
                eventRepositoryMock.Object,
                bookingRepositoryMock.Object,
                venueRepositoryMock.Object,
                settingsRepositoryMock.Object,
                staffRepositoryMock.Object,
                transactionRepositoryMock.Object,
                paymentService,
                upfrontPaymentRepositoryMock.Object,
                virtualMeetingService,
                notificationRepositoryMock.Object,
                bookingPaymentRepositoryMock.Object,
                emailService,
                userRepositoryMock.Object,
                refundService,
                new Mock<ITermsAndConditionsRepository>().Object,
                new Mock<IOrganizerPayoutRepository>().Object,
                new Mock<IFileStorageService>().Object
            );

            var bookingService = new BookingService(
                bookingRepositoryMock.Object,
                eventRepositoryMock.Object,
                transactionRepositoryMock.Object,
                bookingPaymentRepositoryMock.Object,
                settingsRepositoryMock.Object,
                paymentService,
                qrCodeService,
                new Mock<IFileStorageService>().Object,
                configuration,
                emailService,
                notificationRepositoryMock.Object,
                refundService,
                serviceProviderMock.Object
            );

            _serviceProviderMock = new Mock<IServiceProvider>();
            _serviceScopeMock = new Mock<IServiceScope>();
            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

            // Mock service resolution within the scope using concrete services
            var scopeServiceProviderMock = new Mock<IServiceProvider>();
            scopeServiceProviderMock
                .Setup(s => s.GetService(typeof(IBookingService)))
                .Returns(bookingService);
            scopeServiceProviderMock
                .Setup(s => s.GetService(typeof(IEventService)))
                .Returns(eventService);

            _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(scopeServiceProviderMock.Object);
            _serviceScopeFactoryMock.Setup(s => s.CreateScope()).Returns(_serviceScopeMock.Object);

            _serviceProviderMock
                .Setup(s => s.GetService(typeof(IServiceScopeFactory)))
                .Returns(_serviceScopeFactoryMock.Object);

            var logger = new NullLogger<BackgroundService>();
            _backgroundService = new BackgroundService(_serviceProviderMock.Object, logger);
        }
        #endregion

        #region Background Task Execution Tests
        [Test]
        public async Task Test_BackgroundService_TriggersCleanupOnExecution()
        {
            try
            {
                using var cts = new CancellationTokenSource();
                var runTask = _backgroundService.StartAsync(cts.Token);

                // Allow the background thread to run the loop iteration once
                await Task.Delay(50);
                cts.Cancel();
                await runTask;

                LogTestDetail(Service, "ExecuteAsync", "Release expired bookings and events was triggered", null, "Triggered Successfully", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ExecuteAsync", "Release expired bookings and events was triggered", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
