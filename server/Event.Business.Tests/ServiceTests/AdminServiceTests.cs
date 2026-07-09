using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    public class AdminServiceTests : ServiceTestBase
    {
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<IEventRepository> _eventRepositoryMock = null!;
        private Mock<ITransactionRepository> _transactionRepositoryMock = null!;
        private Mock<IBookingPaymentRepository> _bookingPaymentRepositoryMock = null!;
        private Mock<IStaffRepository> _staffRepositoryMock = null!;
        private Mock<ISupportTicketRepository> _supportTicketRepositoryMock = null!;
        private Mock<IAdminActionRepository> _adminActionRepositoryMock = null!;
        private IEmailService _emailService = null!;
        private IEventService _eventService = null!;
        private Mock<IRegionRepository> _regionRepositoryMock = null!;
        private Mock<IVenueRepository> _venueRepositoryMock = null!;
        private Mock<INotificationRepository> _notificationRepositoryMock = null!;

        private AdminService _adminService = null!;

        private const string Service = "AdminService";
        private const string TestEmail = "keshwarankeerthi@gmail.com";
        private const string TestName = "KeerthiKeswaran";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _userRepositoryMock           = new Mock<IUserRepository>();
            _eventRepositoryMock          = new Mock<IEventRepository>();
            _transactionRepositoryMock    = new Mock<ITransactionRepository>();
            _bookingPaymentRepositoryMock = new Mock<IBookingPaymentRepository>();
            _staffRepositoryMock          = new Mock<IStaffRepository>();
            _supportTicketRepositoryMock  = new Mock<ISupportTicketRepository>();
            _adminActionRepositoryMock    = new Mock<IAdminActionRepository>();
            _regionRepositoryMock         = new Mock<IRegionRepository>();
            _venueRepositoryMock          = new Mock<IVenueRepository>();
            _notificationRepositoryMock   = new Mock<INotificationRepository>();

            // Notification repository always succeeds
            _notificationRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .Returns(Task.CompletedTask);

            var configuration = CreateTestConfiguration();
            // _emailService = CreateConcreteEmailService(configuration);
            // var paymentService = CreateConcretePaymentService(configuration);
            // var virtualMeetingService = CreateConcreteVirtualMeetingService();
            _emailService = CreateMockEmailService();
            var paymentService = CreateMockPaymentService();
            var virtualMeetingService = CreateMockVirtualMeetingService();
            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var upfrontPaymentRepositoryMock = new Mock<IOrganizerUpfrontPaymentRepository>();

            var refundService = new RefundService(
                bookingRepositoryMock.Object,
                _eventRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                _bookingPaymentRepositoryMock.Object,
                paymentService,
                new Mock<IServiceProvider>().Object,
                _emailService,
                _notificationRepositoryMock.Object
            );

            _eventService = new EventService(
                _eventRepositoryMock.Object,
                bookingRepositoryMock.Object,
                _venueRepositoryMock.Object,
                new Mock<IPlatformSettingsRepository>().Object,
                _staffRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                paymentService,
                upfrontPaymentRepositoryMock.Object,
                virtualMeetingService,
                _notificationRepositoryMock.Object,
                _bookingPaymentRepositoryMock.Object,
                _emailService,
                _userRepositoryMock.Object,
                refundService,
                new Mock<ITermsAndConditionsRepository>().Object,
                new Mock<IOrganizerPayoutRepository>().Object
            );

            _adminService = new AdminService(
                _userRepositoryMock.Object,
                _eventRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                bookingRepositoryMock.Object,
                _bookingPaymentRepositoryMock.Object,
                _staffRepositoryMock.Object,
                _supportTicketRepositoryMock.Object,
                _adminActionRepositoryMock.Object,
                new Mock<IAdminRepository>().Object,
                _emailService,
                _eventService,
                _regionRepositoryMock.Object,
                _venueRepositoryMock.Object,
                _notificationRepositoryMock.Object,
                refundService
            );
        }
        #endregion

        #region Test_GetDashboardStatsAsync_Success
        [Test]
        public async Task Test_GetDashboardStatsAsync_Success()
        {
            // Arrange: mock aggregated data from all repositories
            _userRepositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<User> { new User { User_Id = 10001 }, new User { User_Id = 10001 } });

            _eventRepositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Event.Models.Event>
                {
                    new Event.Models.Event { Status = "Live" },
                    new Event.Models.Event { Status = "Draft" }
                });

            _transactionRepositoryMock.Setup(r => r.GetGrossRevenueAsync())
                .ReturnsAsync(50000.00m);

            _bookingPaymentRepositoryMock.Setup(r => r.GetTotalCommissionAsync())
                .ReturnsAsync(2500.00m);

            _staffRepositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Staff>
                {
                    new Staff { IsAllocated = true },
                    new Staff { IsAllocated = false }
                });

            try
            {
                // Act: fetch dashboard stats
                var stats = await _adminService.GetDashboardStatsAsync();

                // Assert: summary and staff metrics are correctly computed
                Assert.That(stats, Is.Not.Null);
                Assert.That(stats.Summary.TotalUsers, Is.EqualTo(2));
                Assert.That(stats.Summary.TotalLiveEvents, Is.EqualTo(1));
                Assert.That(stats.Summary.GrossRevenue, Is.EqualTo(50000.00m));
                Assert.That(stats.StaffMetrics.TotalStaff, Is.EqualTo(2));
                Assert.That(stats.StaffMetrics.AllocatedStaffCount, Is.EqualTo(1));

                LogTestDetail(Service, "GetDashboardStatsAsync", "Retrieve dashboard summary statistics", null, stats, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetDashboardStatsAsync", "Retrieve dashboard summary statistics", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_GetEventsPagedAsync_Success
        [Test]
        public async Task Test_GetEventsPagedAsync_Success()
        {
            // Arrange: a paged event list with one physical event with venue and seat capacities
            var mockEvent = new Event.Models.Event
            {
                Event_Id   = 10001,
                Title      = "Tech Summit",
                Event_Type = "Physical",
                Status     = "Live",
                Date_Time  = DateTime.UtcNow.AddDays(10),
                Requires_Staff = true,
                Venue = new Venue
                {
                    Name = "Grand Hall",
                    SeatCapacities = new List<VenueSeatCapacity>
                    {
                        new VenueSeatCapacity { Tier_Name = "Elite", Total_Seats = 50 }
                    }
                },
                Organizer        = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                StaffAllocations = new List<EventStaffAllocation>()
            };

            var pagedResult = new PagedResult<Event.Models.Event>(
                new List<Event.Models.Event> { mockEvent }, 1, 1, 10);

            _eventRepositoryMock
                .Setup(r => r.GetEventsPagedAsync(null, null, null, null, null, null, 1, 10))
                .ReturnsAsync(pagedResult);

            try
            {
                // Act: get paged events with no filters
                var result = await _adminService.GetEventsPagedAsync(null, null, null, null, null, null, 1, 10);

                // Assert: mapping is correct
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Items.Count, Is.EqualTo(1));
                Assert.That(result.Items[0].Title, Is.EqualTo("Tech Summit"));

                LogTestDetail(Service, "GetEventsPagedAsync", "Retrieve paged event list for admin view", null, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetEventsPagedAsync", "Retrieve paged event list for admin view", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_GetSupportTicketsAsync_Success
        [Test]
        public async Task Test_GetSupportTicketsAsync_Success()
        {
            // Arrange: two support tickets in repository
            var tickets = new List<SupportTicket>
            {
                new SupportTicket { Ticket_Id = 10001, Status = "Open" },
                new SupportTicket { Ticket_Id = 10002, Status = "Resolved" }
            };

            _supportTicketRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tickets);

            try
            {
                // Act
                var result = await _adminService.GetSupportTicketsAsync(null, null, null, null);

                // Assert: all tickets returned
                Assert.That(result.Count(), Is.EqualTo(2));

                LogTestDetail(Service, "GetSupportTicketsAsync", "Retrieve all support tickets", null, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetSupportTicketsAsync", "Retrieve all support tickets", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RespondToTicketAsync_Success
        [Test]
        public async Task Test_RespondToTicketAsync_Success()
        {
            int ticketId = 10001;

            // Arrange: a ticket pointing to a temp JSON file
            string tempFile = Path.Combine(Path.GetTempPath(), $"ticket_{ticketId}.json");
            await File.WriteAllTextAsync(tempFile,
                System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "Subject", "Payment Issue" },
                    { "Message", "I was double charged." }
                }));

            var ticket = new SupportTicket
            {
                Ticket_Id  = ticketId,
                User_Id    = 10001,
                ConcernUrl = tempFile,
                Status     = "Open"
            };

            var user = new User { User_Id = 10001, Name = TestName, Email = TestEmail };

            _supportTicketRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(10001)).ReturnsAsync(user);
            _supportTicketRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SupportTicket>())).Returns(Task.CompletedTask);

            try
            {
                // Act: respond to the support ticket
                var result = await _adminService.RespondToTicketAsync(ticketId, "We have processed a refund.");

                // Assert: response saved and returns true
                Assert.That(result, Is.True);

                LogTestDetail(Service, "RespondToTicketAsync", "Admin responds to open support ticket", new { ticketId }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RespondToTicketAsync", "Admin responds to open support ticket", new { ticketId }, null, false, ex.Message);
                throw;
            }
            finally
            {
                // Cleanup temp file after test
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
        #endregion

        #region Test_RespondToTicketAsync_TicketNotFound_ThrowsNotFoundException
        [Test]
        public async Task Test_RespondToTicketAsync_TicketNotFound_ThrowsNotFoundException()
        {
            // Arrange: ticket does not exist
            _supportTicketRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((SupportTicket?)null);

            try
            {
                // Act + Assert: should throw NotFoundException
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _adminService.RespondToTicketAsync(999, "Some response"));

                LogTestDetail(Service, "RespondToTicketAsync", "Respond to non-existent ticket throws NotFoundException", new { ticketId = 999 }, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RespondToTicketAsync", "Respond to non-existent ticket throws NotFoundException", new { ticketId = 999 }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_EscalateTicketAsync_Success
        [Test]
        public async Task Test_EscalateTicketAsync_Success()
        {
            int ticketId = 10002;
            string adminId = "ADM_1001";

            // Arrange: an existing open ticket
            var ticket = new SupportTicket { Ticket_Id = ticketId, User_Id = 10001, Status = "Open" };

            _supportTicketRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);
            _adminActionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AdminAction>())).Returns(Task.CompletedTask);
            _supportTicketRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SupportTicket>())).Returns(Task.CompletedTask);

            var request = new EscalateTicketRequest
            {
                ActionType  = "REF",
                TargetType  = "ATD",
                TargetId    = 10001,
                TicketId = 10101
            };

            try
            {
                // Act: escalate the ticket
                var result = await _adminService.EscalateTicketAsync(ticketId, adminId, request);

                // Assert: escalation returns true and ticket status is updated
                Assert.That(result, Is.True);
                Assert.That(ticket.EsclationStatus, Is.EqualTo("Escalated"));

                LogTestDetail(Service, "EscalateTicketAsync", "Escalate an open support ticket", new { ticketId, adminId }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "EscalateTicketAsync", "Escalate an open support ticket", new { ticketId, adminId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_EscalateTicketAsync_TicketNotFound_ThrowsNotFoundException
        [Test]
        public async Task Test_EscalateTicketAsync_TicketNotFound_ThrowsNotFoundException()
        {
            // Arrange: ticket does not exist
            _supportTicketRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((SupportTicket?)null);

            try
            {
                // Act + Assert: escalating a missing ticket throws NotFoundException
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _adminService.EscalateTicketAsync(999, "ADM_1001", new EscalateTicketRequest()));

                LogTestDetail(Service, "EscalateTicketAsync", "Escalate non-existent ticket throws NotFoundException", new { ticketId = 999 }, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "EscalateTicketAsync", "Escalate non-existent ticket throws NotFoundException", new { ticketId = 999 }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_GetFlaggedEventsReportsAsync_Success
        [Test]
        public async Task Test_GetFlaggedEventsReportsAsync_Success()
        {
            // Arrange: two reports on the same event
            var reports = new List<EventReport>
            {
                new EventReport
                {
                    Report_Id  = 10001,
                    Event_Id   = 10100,
                    Reporter_Id = 10001,
                    Reporter   = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                    ReportUrl  = "/assets/events/10100/reports/10001_report.json",
                    Created_At = DateTime.UtcNow
                },
                new EventReport
                {
                    Report_Id  = 10002,
                    Event_Id   = 10100,
                    Reporter_Id = 10002,
                    Reporter   = new User { User_Id = 10001, Name = TestName, Email = TestEmail },
                    ReportUrl  = "/assets/events/10100/reports/10002_report.json",
                    Created_At = DateTime.UtcNow
                }
            };

            _eventRepositoryMock.Setup(r => r.GetAllReportsAsync()).ReturnsAsync(reports);

            try
            {
                // Act: retrieve flagged event reports grouped by event
                var result = await _adminService.GetFlaggedEventsReportsAsync();

                // Assert: reports are returned (as a dictionary-like object)
                Assert.That(result, Is.Not.Null);

                LogTestDetail(Service, "GetFlaggedEventsReportsAsync", "Retrieve grouped flagged event reports", null, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetFlaggedEventsReportsAsync", "Retrieve grouped flagged event reports", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_DismissEventReportAsync_Success
        [Test]
        public async Task Test_DismissEventReportAsync_Success()
        {
            int reportId = 1;

            // Arrange: an existing report
            var report = new EventReport { Report_Id = reportId, ResponseAction = "Pending" };
            _eventRepositoryMock.Setup(r => r.GetReportByIdAsync(reportId)).ReturnsAsync(report);
            _eventRepositoryMock.Setup(r => r.UpdateReportAsync(It.IsAny<EventReport>())).Returns(Task.CompletedTask);

            try
            {
                // Act: dismiss the report
                var result = await _adminService.DismissEventReportAsync(reportId);

                // Assert: report action updated to Dismissed
                Assert.That(result, Is.True);
                Assert.That(report.ResponseAction, Is.EqualTo("Dismissed"));

                LogTestDetail(Service, "DismissEventReportAsync", "Dismiss a flagged event report", new { reportId }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "DismissEventReportAsync", "Dismiss a flagged event report", new { reportId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_DismissEventReportAsync_NotFound_ThrowsNotFoundException
        [Test]
        public async Task Test_DismissEventReportAsync_NotFound_ThrowsNotFoundException()
        {
            // Arrange: report does not exist
            _eventRepositoryMock.Setup(r => r.GetReportByIdAsync(999)).ReturnsAsync((EventReport?)null);

            try
            {
                // Act + Assert: dismissing a missing report throws NotFoundException
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _adminService.DismissEventReportAsync(999));

                LogTestDetail(Service, "DismissEventReportAsync", "Dismiss non-existent report throws NotFoundException", new { reportId = 999 }, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "DismissEventReportAsync", "Dismiss non-existent report throws NotFoundException", new { reportId = 999 }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_UpholdEventReportAsync_Success_RestrictOrganizer
        [Test]
        public async Task Test_UpholdEventReportAsync_Success_RestrictOrganizer()
        {
            int reportId = 1;
            string adminId = "ADM_1001";

            // Arrange: report tied to an event with an organizer
            var organizer = new User { User_Id = 5, Name = TestName, Email = TestEmail, Status = "Active" };
            var ev = new Event.Models.Event { Event_Id = 10, Title = "Fake Gala", Organizer_Id = 5, Organizer = organizer };
            var report = new EventReport { Report_Id = reportId, Event_Id = 10, Event = ev };

            _eventRepositoryMock.Setup(r => r.GetReportByIdAsync(reportId)).ReturnsAsync(report);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(organizer);
            _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _supportTicketRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SupportTicket>())).Returns(Task.CompletedTask);
            _adminActionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AdminAction>())).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.UpdateReportAsync(It.IsAny<EventReport>())).Returns(Task.CompletedTask);

            try
            {
                // Act: uphold report and restrict the organizer
                var result = await _adminService.UpholdEventReportAsync(reportId, adminId, "Policy violation", "Restrict");

                // Assert: report upheld and organizer restricted
                Assert.That(result, Is.True);
                Assert.That(organizer.Status, Is.EqualTo("Restricted"));
                Assert.That(report.ResponseAction, Is.EqualTo("Upholds"));

                LogTestDetail(Service, "UpholdEventReportAsync", "Uphold report and restrict organizer", new { reportId, adminId }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "UpholdEventReportAsync", "Uphold report and restrict organizer", new { reportId, adminId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_UpholdEventReportAsync_Success_DeactivateOrganizer
        [Test]
        public async Task Test_UpholdEventReportAsync_Success_DeactivateOrganizer()
        {
            int reportId = 2;
            string adminId = "ADM_1001";

            // Arrange: report with event and organizer for deactivation path
            var organizer = new User { User_Id = 6, Name = TestName, Email = TestEmail, Status = "Active" };
            var ev = new Event.Models.Event { Event_Id = 20, Title = "Bad Event", Organizer_Id = 6, Organizer = organizer };
            var report = new EventReport { Report_Id = reportId, Event_Id = 20, Event = ev };

            _eventRepositoryMock.Setup(r => r.GetReportByIdAsync(reportId)).ReturnsAsync(report);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(organizer);
            _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _supportTicketRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SupportTicket>())).Returns(Task.CompletedTask);
            _adminActionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AdminAction>())).Returns(Task.CompletedTask);
            _eventRepositoryMock.Setup(r => r.UpdateReportAsync(It.IsAny<EventReport>())).Returns(Task.CompletedTask);

            try
            {
                // Act: uphold report and deactivate the organizer
                var result = await _adminService.UpholdEventReportAsync(reportId, adminId, "Fraud", "Deactivate");

                // Assert: organizer deactivated
                Assert.That(result, Is.True);
                Assert.That(organizer.Status, Is.EqualTo("Deactivated"));

                LogTestDetail(Service, "UpholdEventReportAsync", "Uphold report and deactivate organizer", new { reportId, adminId }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "UpholdEventReportAsync", "Uphold report and deactivate organizer", new { reportId, adminId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_UpholdEventReportAsync_ReportNotFound_ThrowsNotFoundException
        [Test]
        public async Task Test_UpholdEventReportAsync_ReportNotFound_ThrowsNotFoundException()
        {
            // Arrange: report does not exist
            _eventRepositoryMock.Setup(r => r.GetReportByIdAsync(999)).ReturnsAsync((EventReport?)null);

            try
            {
                // Act + Assert: should throw NotFoundException immediately
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _adminService.UpholdEventReportAsync(999, "ADM_1001", "Reason", "Restrict"));

                LogTestDetail(Service, "UpholdEventReportAsync", "Uphold non-existent report throws NotFoundException", new { reportId = 999 }, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "UpholdEventReportAsync", "Uphold non-existent report throws NotFoundException", new { reportId = 999 }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_GetAllRegionsAsync_Success
        [Test]
        public async Task Test_GetAllRegionsAsync_Success()
        {
            // Arrange: three regions in the system
            var regions = new List<Region>
            {
                new Region { Region_Id = "US-EAST" },
                new Region { Region_Id = "US-WEST" },
                new Region { Region_Id = "EU-CENTRAL" }
            };

            _regionRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(regions);

            try
            {
                // Act: retrieve all regions
                var result = await _adminService.GetAllRegionsAsync();

                // Assert: all three regions returned
                Assert.That(result.Count(), Is.EqualTo(3));

                LogTestDetail(Service, "GetAllRegionsAsync", "Retrieve all platform regions", null, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetAllRegionsAsync", "Retrieve all platform regions", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_GetAllVenuesAsync_Success
        [Test]
        public async Task Test_GetAllVenuesAsync_Success()
        {
            // Arrange: a venue with seat capacities
            var venues = new List<Venue>
            {
                new Venue
                {
                    Venue_Id     = 1,
                    Name         = "Grand Arena",
                    Region_Id    = "US-EAST",
                    Hourly_Price = 1200.00m,
                    Is_Available = true,
                    SeatCapacities = new List<VenueSeatCapacity>
                    {
                        new VenueSeatCapacity { Tier_Name = "Elite", Total_Seats = 100 }
                    }
                }
            };

            _venueRepositoryMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(venues);

            try
            {
                // Act: retrieve all venues with seat details
                var result = await _adminService.GetAllVenuesAsync();

                // Assert: venue mapped correctly
                Assert.That(result.Count(), Is.EqualTo(1));
                Assert.That(result.First().Name, Is.EqualTo("Grand Arena"));
                Assert.That(result.First().SeatTiers.Count, Is.EqualTo(1));

                LogTestDetail(Service, "GetAllVenuesAsync", "Retrieve all venues with seat tier details", null, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetAllVenuesAsync", "Retrieve all venues with seat tier details", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_CreateVenueAsync_Success
        [Test]
        public async Task Test_CreateVenueAsync_Success()
        {
            // Arrange: valid request with all three required seat tiers
            var request = new CreateVenueRequest
            {
                Region_Id    = "US-EAST",
                Name         = "Convention Center",
                Address      = "123 Main St",
                Hourly_Price = 800.00m,
                Is_Available = true,
                SeatTiers = new List<SeatTierRequest>
                {
                    new SeatTierRequest { Tier_Name = "Elite",  Total_Seats = 50  },
                    new SeatTierRequest { Tier_Name = "Gold",   Total_Seats = 150 },
                    new SeatTierRequest { Tier_Name = "Silver", Total_Seats = 300 }
                }
            };

            var createdVenue = new Venue
            {
                Venue_Id     = 10,
                Region_Id    = "US-EAST",
                Name         = "Convention Center",
                Hourly_Price = 800.00m,
                Is_Available = true,
                SeatCapacities = new List<VenueSeatCapacity>
                {
                    new VenueSeatCapacity { Tier_Name = "Elite",  Total_Seats = 50  },
                    new VenueSeatCapacity { Tier_Name = "Gold",   Total_Seats = 150 },
                    new VenueSeatCapacity { Tier_Name = "Silver", Total_Seats = 300 }
                }
            };

            _regionRepositoryMock.Setup(r => r.GetByRegionIdAsync("US-EAST"))
                .ReturnsAsync(new Region { Region_Id = "US-EAST" });

            _venueRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Venue>()))
                .Callback<Venue>(v => v.Venue_Id = 10)
                .Returns(Task.CompletedTask);

            _venueRepositoryMock.Setup(r => r.AddSeatCapacityAsync(It.IsAny<VenueSeatCapacity>()))
                .Returns(Task.CompletedTask);

            // Return the created venue in the full details list
            _venueRepositoryMock.Setup(r => r.GetAllWithDetailsAsync())
                .ReturnsAsync(new List<Venue> { createdVenue });

            try
            {
                // Act: create the venue
                var result = await _adminService.CreateVenueAsync(request);

                // Assert: created venue response is correct
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Name, Is.EqualTo("Convention Center"));
                Assert.That(result.SeatTiers.Count, Is.EqualTo(3));

                LogTestDetail(Service, "CreateVenueAsync", "Create a venue with all three seat tiers", request, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CreateVenueAsync", "Create a venue with all three seat tiers", request, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_CreateVenueAsync_MissingSeatTiers_ThrowsValidationException
        [Test]
        public async Task Test_CreateVenueAsync_MissingSeatTiers_ThrowsValidationException()
        {
            // Arrange: request is missing Silver tier — invalid
            var request = new CreateVenueRequest
            {
                Region_Id    = "US-EAST",
                Name         = "Incomplete Venue",
                Hourly_Price = 500.00m,
                SeatTiers = new List<SeatTierRequest>
                {
                    new SeatTierRequest { Tier_Name = "Elite", Total_Seats = 50 },
                    new SeatTierRequest { Tier_Name = "Gold",  Total_Seats = 100 }
                    // Missing Silver
                }
            };

            try
            {
                // Act + Assert: venue creation without all tiers throws ValidationException
                Assert.ThrowsAsync<ValidationException>(async () =>
                    await _adminService.CreateVenueAsync(request));

                LogTestDetail(Service, "CreateVenueAsync", "Venue with incomplete seat tiers throws ValidationException", request, "ValidationException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CreateVenueAsync", "Venue with incomplete seat tiers throws ValidationException", request, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_GetStaffDirectoryAsync_Success
        [Test]
        public async Task Test_GetStaffDirectoryAsync_Success()
        {
            // Arrange: two staff members in different regions
            var staffList = new List<Staff>
            {
                new Staff { Employee_ID = 1, Region_Id = "US-EAST", IsAllocated = false },
                new Staff { Employee_ID = 2, Region_Id = "EU-CENTRAL", IsAllocated = true }
            };

            _staffRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(staffList);

            try
            {
                // Act
                var result = await _adminService.GetStaffDirectoryAsync(null, null, null, null, 1, 10);

                // Assert: both staff members returned with correct data
                Assert.That(result.Items.Count(), Is.EqualTo(2));

                LogTestDetail(Service, "GetStaffDirectoryAsync", "Retrieve full staff directory", null, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetStaffDirectoryAsync", "Retrieve full staff directory", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_AllocateStaffToEventAsync_Success
        [Test]
        public async Task Test_AllocateStaffToEventAsync_Success()
        {
            int eventId    = 10001;
            int employeeId = 10010;

            // Arrange: a Physical event with a venue in US-EAST region and no staff yet
            var staff = new Staff { Employee_ID = employeeId, Region_Id = "US-EAST", IsAllocated = false };

            var ev = new Event.Models.Event
            {
                Event_Id   = eventId,
                Event_Type = "Physical",
                Venue = new Venue { Region_Id = "US-EAST" },
                StaffAllocations = new List<EventStaffAllocation>()
            };

            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(ev);
            _staffRepositoryMock.Setup(r => r.GetByIdAsync(employeeId)).ReturnsAsync(staff);
            _eventRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Event.Models.Event>())).Returns(Task.CompletedTask);
            _staffRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Staff>())).Returns(Task.CompletedTask);

            try
            {
                // Act: allocate staff member to the event
                var result = await _adminService.AllocateStaffToEventAsync(eventId, employeeId);

                // Assert: allocation succeeds and staff is marked allocated
                Assert.That(result, Is.True);
                Assert.That(staff.IsAllocated, Is.True);

                LogTestDetail(Service, "AllocateStaffToEventAsync", "Allocate staff to a physical event", new { eventId, employeeId }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "AllocateStaffToEventAsync", "Allocate staff to a physical event", new { eventId, employeeId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_AllocateStaffToEventAsync_VirtualEvent_ThrowsValidationException
        [Test]
        public async Task Test_AllocateStaffToEventAsync_VirtualEvent_ThrowsValidationException()
        {
            int eventId    = 10002;
            int employeeId = 10010;

            // Arrange: a Virtual event — staff cannot be allocated to virtual events
            var ev = new Event.Models.Event
            {
                Event_Id   = eventId,
                Event_Type = "Virtual",
                StaffAllocations = new List<EventStaffAllocation>()
            };

            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(ev);

            try
            {
                // Act + Assert: allocating to a virtual event throws ValidationException
                Assert.ThrowsAsync<ValidationException>(async () =>
                    await _adminService.AllocateStaffToEventAsync(eventId, employeeId));

                LogTestDetail(Service, "AllocateStaffToEventAsync", "Allocate staff to virtual event throws ValidationException", new { eventId, employeeId }, "ValidationException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "AllocateStaffToEventAsync", "Allocate staff to virtual event throws ValidationException", new { eventId, employeeId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_AllocateStaffToEventAsync_RegionMismatch_ThrowsValidationException
        [Test]
        public async Task Test_AllocateStaffToEventAsync_RegionMismatch_ThrowsValidationException()
        {
            int eventId    = 10003;
            int employeeId = 10011;

            // Arrange: staff works in EU-CENTRAL but event venue is in US-EAST
            var staff = new Staff { Employee_ID = employeeId, Region_Id = "EU-CENTRAL", IsAllocated = false };

            var ev = new Event.Models.Event
            {
                Event_Id   = eventId,
                Event_Type = "Physical",
                Venue = new Venue { Region_Id = "US-EAST" },
                StaffAllocations = new List<EventStaffAllocation>()
            };

            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(ev);
            _staffRepositoryMock.Setup(r => r.GetByIdAsync(employeeId)).ReturnsAsync(staff);

            try
            {
                // Act + Assert: region mismatch throws ValidationException
                Assert.ThrowsAsync<ValidationException>(async () =>
                    await _adminService.AllocateStaffToEventAsync(eventId, employeeId));

                LogTestDetail(Service, "AllocateStaffToEventAsync", "Staff region mismatch throws ValidationException", new { eventId, employeeId }, "ValidationException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "AllocateStaffToEventAsync", "Staff region mismatch throws ValidationException", new { eventId, employeeId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_AllocateStaffToEventAsync_AlreadyAllocated_ThrowsValidationException
        [Test]
        public async Task Test_AllocateStaffToEventAsync_AlreadyAllocated_ThrowsValidationException()
        {
            int eventId    = 10004;
            int employeeId = 10012;

            // Arrange: staff is already in the event's allocation list
            var staff = new Staff { Employee_ID = employeeId, Region_Id = "US-EAST", IsAllocated = true };

            var ev = new Event.Models.Event
            {
                Event_Id   = eventId,
                Event_Type = "Physical",
                Venue = new Venue { Region_Id = "US-EAST" },
                StaffAllocations = new List<EventStaffAllocation>
                {
                    new EventStaffAllocation { Employee_ID = employeeId }
                }
            };

            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(eventId)).ReturnsAsync(ev);
            _staffRepositoryMock.Setup(r => r.GetByIdAsync(employeeId)).ReturnsAsync(staff);

            try
            {
                // Act + Assert: duplicate allocation throws ValidationException
                Assert.ThrowsAsync<ValidationException>(async () =>
                    await _adminService.AllocateStaffToEventAsync(eventId, employeeId));

                LogTestDetail(Service, "AllocateStaffToEventAsync", "Duplicate staff allocation throws ValidationException", new { eventId, employeeId }, "ValidationException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "AllocateStaffToEventAsync", "Duplicate staff allocation throws ValidationException", new { eventId, employeeId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_AllocateStaffToEventAsync_EventNotFound_ThrowsNotFoundException
        [Test]
        public void Test_AllocateStaffToEventAsync_EventNotFound_ThrowsNotFoundException()
        {
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10999)).ReturnsAsync((Event.Models.Event?)null);
            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _adminService.AllocateStaffToEventAsync(10999, 10010));
        }
        #endregion

        #region Test_AllocateStaffToEventAsync_StaffNotFound_ThrowsNotFoundException
        [Test]
        public void Test_AllocateStaffToEventAsync_StaffNotFound_ThrowsNotFoundException()
        {
            var ev = new Event.Models.Event { Event_Id = 10001, Event_Type = "Physical" };
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10001)).ReturnsAsync(ev);
            _staffRepositoryMock.Setup(r => r.GetByIdAsync(10999)).ReturnsAsync((Staff?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _adminService.AllocateStaffToEventAsync(10001, 10999));
        }
        #endregion

        #region Test_AllocateStaffToEventAsync_NoVenueAssigned_ThrowsValidationException
        [Test]
        public void Test_AllocateStaffToEventAsync_NoVenueAssigned_ThrowsValidationException()
        {
            var ev = new Event.Models.Event { Event_Id = 10001, Event_Type = "Physical", Venue = null };
            var staff = new Staff { Employee_ID = 10010, Region_Id = "US-EAST" };
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(10001)).ReturnsAsync(ev);
            _staffRepositoryMock.Setup(r => r.GetByIdAsync(10010)).ReturnsAsync(staff);

            Assert.ThrowsAsync<ValidationException>(async () =>
                await _adminService.AllocateStaffToEventAsync(10001, 10010));
        }
        #endregion

        #region Test_CreateVenueAsync_CreateRegionPath_Success
        [Test]
        public async Task Test_CreateVenueAsync_CreateRegionPath_Success()
        {
            var request = new CreateVenueRequest
            {
                Region_Id = "US-WEST",
                Name = "West Center",
                Address = "456 West St",
                Hourly_Price = 500m,
                Is_Available = true,
                SeatTiers = new List<SeatTierRequest>
                {
                    new SeatTierRequest { Tier_Name = "Elite", Total_Seats = 10 },
                    new SeatTierRequest { Tier_Name = "Gold", Total_Seats = 20 },
                    new SeatTierRequest { Tier_Name = "Silver", Total_Seats = 30 }
                }
            };

            var createdVenue = new Venue
            {
                Venue_Id = 20,
                Region_Id = "US-WEST",
                Name = "West Center",
                Hourly_Price = 500m,
                Is_Available = true,
                SeatCapacities = new List<VenueSeatCapacity>
                {
                    new VenueSeatCapacity { Tier_Name = "Elite", Total_Seats = 10 },
                    new VenueSeatCapacity { Tier_Name = "Gold", Total_Seats = 20 },
                    new VenueSeatCapacity { Tier_Name = "Silver", Total_Seats = 30 }
                }
            };

            _regionRepositoryMock.Setup(r => r.GetByRegionIdAsync("US-WEST")).ReturnsAsync((Region?)null);
            _regionRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Region>())).Returns(Task.CompletedTask);
            _venueRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Venue>())).Callback<Venue>(v => v.Venue_Id = 20).Returns(Task.CompletedTask);
            _venueRepositoryMock.Setup(r => r.AddSeatCapacityAsync(It.IsAny<VenueSeatCapacity>())).Returns(Task.CompletedTask);
            _venueRepositoryMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(new List<Venue> { createdVenue });

            var result = await _adminService.CreateVenueAsync(request);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Region_Id, Is.EqualTo("US-WEST"));
            _regionRepositoryMock.Verify(r => r.AddAsync(It.Is<Region>(reg => reg.Region_Id == "US-WEST")), Times.Once);
        }
        #endregion

        #region Test_RespondToTicketAsync_UserNotFound_ThrowsNotFoundException
        [Test]
        public void Test_RespondToTicketAsync_UserNotFound_ThrowsNotFoundException()
        {
            var ticket = new SupportTicket { Ticket_Id = 5, User_Id = 999 };
            _supportTicketRepositoryMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(ticket);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _adminService.RespondToTicketAsync(5, "Response"));
        }
        #endregion

        #region Test_RespondToTicketAsync_ConcernUrlEmpty_ThrowsValidationException
        [TestCase(null)]
        [TestCase("")]
        public void Test_RespondToTicketAsync_ConcernUrlEmpty_ThrowsValidationException(string? concernUrl)
        {
            var ticket = new SupportTicket { Ticket_Id = 10005, User_Id = 10001, ConcernUrl = concernUrl! };
            var user = new User { User_Id = 10001, Name = TestName, Email = TestEmail };
            _supportTicketRepositoryMock.Setup(r => r.GetByIdAsync(10005)).ReturnsAsync(ticket);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(10001)).ReturnsAsync(user);

            Assert.ThrowsAsync<ValidationException>(async () =>
                await _adminService.RespondToTicketAsync(10005, "Response"));
        }
        #endregion
    }
}
