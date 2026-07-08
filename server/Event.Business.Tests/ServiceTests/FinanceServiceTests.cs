using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
    public class FinanceServiceTests : ServiceTestBase
    {
        private Mock<IAdminActionRepository> _adminActionRepositoryMock = null!;
        private Mock<ISupportTicketRepository> _supportTicketRepositoryMock = null!;
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private IRefundService _refundService = null!;
        private IEmailService _emailService = null!;
        private Mock<INotificationRepository> _notificationRepositoryMock = null!;
        private Mock<ITransactionRepository> _transactionRepositoryMock = null!;

        private FinanceService _financeService = null!;

        private const string Service = "FinanceService";
        private const string TestEmail = "keshwarankeerthi@gmail.com";
        private const string TestName = "KeerthiKeswaran";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _adminActionRepositoryMock   = new Mock<IAdminActionRepository>();
            _supportTicketRepositoryMock = new Mock<ISupportTicketRepository>();
            _userRepositoryMock          = new Mock<IUserRepository>();
            _notificationRepositoryMock  = new Mock<INotificationRepository>();
            _transactionRepositoryMock   = new Mock<ITransactionRepository>();

            var bookingRepositoryMock = new Mock<IBookingRepository>();
            var eventRepositoryMock = new Mock<IEventRepository>();
            var bookingPaymentRepositoryMock = new Mock<IBookingPaymentRepository>();

            var configuration = CreateTestConfiguration();
            // _emailService = CreateConcreteEmailService(configuration);
            // var paymentService = CreateConcretePaymentService(configuration);
            _emailService = CreateMockEmailService();
            var paymentService = CreateMockPaymentService();

            _refundService = new RefundService(
                bookingRepositoryMock.Object,
                eventRepositoryMock.Object,
                _transactionRepositoryMock.Object,
                bookingPaymentRepositoryMock.Object,
                paymentService,
                new Mock<IServiceProvider>().Object,
                _emailService,
                _notificationRepositoryMock.Object
            );

            // Notification repository always succeeds
            _notificationRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .Returns(Task.CompletedTask);

            // Setup default mock responses for the concrete RefundService to use during finance actions:
            
            // 1. Booking details
            var booking501 = new Booking {
                Booking_Id = 501,
                Attendee_Id = 10,
                Booking_Status = "Pending",
                Attendee = new User { User_Id = 10001, Email = TestEmail },
                Event = new Event.Models.Event { Date_Time = DateTime.UtcNow.AddDays(3), Title = "Gala" }
            };
            var booking502 = new Booking {
                Booking_Id = 502,
                Attendee_Id = 10,
                Booking_Status = "Pending",
                Attendee = new User { User_Id = 10001, Email = TestEmail },
                Event = new Event.Models.Event { Date_Time = DateTime.UtcNow.AddDays(3), Title = "Gala" }
            };
            var booking503 = new Booking {
                Booking_Id = 503,
                Attendee_Id = 10,
                Booking_Status = "Pending",
                Attendee = new User { User_Id = 10001, Email = TestEmail },
                Event = new Event.Models.Event { Date_Time = DateTime.UtcNow.AddDays(3), Title = "Gala" }
            };

            bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(501)).ReturnsAsync(booking501);
            bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(502)).ReturnsAsync(booking502);
            bookingRepositoryMock.Setup(r => r.GetBookingDetailsAsync(503)).ReturnsAsync(booking503);

            // 2. Booking payments
            var payment501 = new BookingPayment { Amount = 150.00m, Payment_Status = "Success" };
            var payment502 = new BookingPayment { Amount = 60.00m, Payment_Status = "Success" };
            var payment503 = new BookingPayment { Amount = 100.00m, Payment_Status = "Success" };

            bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(501)).ReturnsAsync(payment501);
            bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(502)).ReturnsAsync(payment502);
            bookingPaymentRepositoryMock.Setup(r => r.GetSuccessPaymentByBookingIdAsync(503)).ReturnsAsync(payment503);

            // 3. Transactions
            var tx501 = new Transaction { Transaction_Reference = "ch_test_123", Amount = 150.00m, Refunded_Amount = 0m };
            var tx502 = new Transaction { Transaction_Reference = "ch_test_123", Amount = 60.00m, Refunded_Amount = 0m };
            var tx503 = new Transaction { Transaction_Reference = "ch_test_123", Amount = 100.00m, Refunded_Amount = 0m };

            _transactionRepositoryMock.Setup(r => r.GetSuccessBookingTransactionAsync(501)).ReturnsAsync(tx501);
            _transactionRepositoryMock.Setup(r => r.GetSuccessBookingTransactionAsync(502)).ReturnsAsync(tx502);
            _transactionRepositoryMock.Setup(r => r.GetSuccessBookingTransactionAsync(503)).ReturnsAsync(tx503);

            // 4. Event details
            var event201 = new Event.Models.Event {
                Event_Id = 201,
                Organizer_Id = 10,
                Title = "Organized Gala",
                Status = "Live",
                Date_Time = DateTime.UtcNow.AddDays(3),
                Organizer = new User { User_Id = 10001, Email = TestEmail }
            };
            eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(201)).ReturnsAsync(event201);

            // 5. Organizer transactions
            var txs201 = new List<Transaction> {
                new Transaction {
                    Related_Id = 201,
                    Transaction_Type = "OrganizerUpfrontPayment",
                    Status = "Success",
                    Amount = 100.00m,
                    Transaction_Reference = "ch_test_123"
                }
            };
            _transactionRepositoryMock.Setup(r => r.GetTransactionsByUserIdAsync(10)).ReturnsAsync(txs201);

             _financeService = new FinanceService(
                 _adminActionRepositoryMock.Object,
                 _supportTicketRepositoryMock.Object,
                 _userRepositoryMock.Object,
                 _refundService,
                 _emailService,
                 _notificationRepositoryMock.Object,
                 _transactionRepositoryMock.Object,
                 eventRepositoryMock.Object,
                 new Moq.Mock<Event.Contracts.IRepositories.IPlatformSettingsRepository>().Object
             );
        }
        #endregion

        #region Test_GetAdminActionsAsync_Success
        [Test]
        public async Task Test_GetAdminActionsAsync_Success()
        {
            // Arrange: two pending admin actions in the repository
            var actions = new List<AdminAction>
            {
                new AdminAction { ActionId = 1, ActionType = "REF", ActionStatus = "Pending" },
                new AdminAction { ActionId = 2, ActionType = "REF", ActionStatus = "Processed" }
            };

            _adminActionRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(actions);

            try
            {
                // Act: retrieve all admin actions for finance review
                var result = await _financeService.GetAdminActionsAsync();

                // Assert: both actions are returned
                Assert.That(result.Count(), Is.EqualTo(2));

                LogTestDetail(Service, "GetAdminActionsAsync", "Retrieve all admin actions for finance review", null, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetAdminActionsAsync", "Retrieve all admin actions for finance review", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_DeclineActionAsync_Success
        [Test]
        public async Task Test_DeclineActionAsync_Success()
        {
            int actionId = 1;

            // Arrange: an existing pending admin action
            var action = new AdminAction
            {
                ActionId     = 1,
                ActionType   = "REF",
                ActionStatus = "Pending",
                Remarks      = ""
            };

            _adminActionRepositoryMock.Setup(r => r.GetByIdAsync(actionId)).ReturnsAsync(action);
            _adminActionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<AdminAction>())).Returns(Task.CompletedTask);

            try
            {
                // Act: decline the action with a remark
                var result = await _financeService.DeclineActionAsync(actionId, "Insufficient evidence for refund.");

                // Assert: action marked Declined with the provided remark
                Assert.That(result, Is.True);
                Assert.That(action.ActionStatus, Is.EqualTo("Declined"));
                Assert.That(action.Remarks, Is.EqualTo("Insufficient evidence for refund."));

                LogTestDetail(Service, "DeclineActionAsync", "Finance declines a pending admin action", new { actionId }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "DeclineActionAsync", "Finance declines a pending admin action", new { actionId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_DeclineActionAsync_NotFound_ThrowsNotFoundException
        [Test]
        public async Task Test_DeclineActionAsync_NotFound_ThrowsNotFoundException()
        {
            // Arrange: action does not exist
            _adminActionRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((AdminAction?)null);

            try
            {
                // Act + Assert: declining a missing action throws NotFoundException
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _financeService.DeclineActionAsync(999, "Remarks"));

                LogTestDetail(Service, "DeclineActionAsync", "Decline non-existent action throws NotFoundException", new { actionId = 999 }, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "DeclineActionAsync", "Decline non-existent action throws NotFoundException", new { actionId = 999 }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_ApproveActionAsync_Attendee_FullRefund_Success
        [Test]
        public async Task Test_ApproveActionAsync_Attendee_FullRefund_Success()
        {
            int actionId = 10;

            // Arrange: REF action targeting an attendee (ATD)
            var action = new AdminAction
            {
                ActionId     = actionId,
                ActionType   = "REF",
                TargetType   = "ATD",
                TargetId     = 10,
                TicketId = 501,
                ActionStatus = "Pending"
            };

            _adminActionRepositoryMock.Setup(r => r.GetByIdAsync(actionId)).ReturnsAsync(action);
            _adminActionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<AdminAction>())).Returns(Task.CompletedTask);

            try
            {
                // Act: approve the action with FUL refund type
                var result = await _financeService.ApproveActionAsync(actionId, "FUL", "Full refund approved.");

                // Assert: action approved and status set to Processed
                Assert.That(result, Is.True);
                Assert.That(action.ActionStatus, Is.EqualTo("Processed"));

                LogTestDetail(Service, "ApproveActionAsync", "Approve attendee refund action with full refund type", new { actionId, refundType = "FUL" }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ApproveActionAsync", "Approve attendee refund action with full refund type", new { actionId, refundType = "FUL" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_ApproveActionAsync_Organizer_DynamicRefund_Success
        [Test]
        public async Task Test_ApproveActionAsync_Organizer_DynamicRefund_Success()
        {
            int actionId = 11;

            // Arrange: REF action targeting an organizer (ORG)
            var action = new AdminAction
            {
                ActionId     = actionId,
                ActionType   = "REF",
                TargetType   = "ORG",
                TargetId     = 5,
                TicketId = 201,
                ActionStatus = "Pending"
            };

            _adminActionRepositoryMock.Setup(r => r.GetByIdAsync(actionId)).ReturnsAsync(action);
            _adminActionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<AdminAction>())).Returns(Task.CompletedTask);

            try
            {
                // Act: approve with DYN refund type for organizer
                var result = await _financeService.ApproveActionAsync(actionId, "DYN", "Dynamic refund for event #201");

                // Assert: action approved and Processed status set
                Assert.That(result, Is.True);
                Assert.That(action.ActionStatus, Is.EqualTo("Processed"));

                LogTestDetail(Service, "ApproveActionAsync", "Approve organizer dynamic refund action", new { actionId, refundType = "DYN" }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ApproveActionAsync", "Approve organizer dynamic refund action", new { actionId, refundType = "DYN" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_ApproveActionAsync_InvalidRefundType_ThrowsValidationException
        [Test]
        public async Task Test_ApproveActionAsync_InvalidRefundType_ThrowsValidationException()
        {
            int actionId = 12;

            // Arrange: valid REF action exists
            var action = new AdminAction
            {
                ActionId     = actionId,
                ActionType   = "REF",
                TargetType   = "ATD",
                ActionStatus = "Pending"
            };

            _adminActionRepositoryMock.Setup(r => r.GetByIdAsync(actionId)).ReturnsAsync(action);

            try
            {
                // Act + Assert: unknown refund type code throws ValidationException
                Assert.ThrowsAsync<ValidationException>(async () =>
                    await _financeService.ApproveActionAsync(actionId, "XYZ", ""));

                LogTestDetail(Service, "ApproveActionAsync", "Invalid refund type code throws ValidationException", new { actionId, refundType = "XYZ" }, "ValidationException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ApproveActionAsync", "Invalid refund type code throws ValidationException", new { actionId, refundType = "XYZ" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_ApproveActionAsync_NonRefundActionType_ThrowsValidationException
        [Test]
        public async Task Test_ApproveActionAsync_NonRefundActionType_ThrowsValidationException()
        {
            int actionId = 13;

            // Arrange: action with unsupported type for approval
            var action = new AdminAction
            {
                ActionId     = actionId,
                ActionType   = "BAN",
                ActionStatus = "Pending"
            };

            _adminActionRepositoryMock.Setup(r => r.GetByIdAsync(actionId)).ReturnsAsync(action);

            try
            {
                // Act + Assert: non-REF action type throws ValidationException
                Assert.ThrowsAsync<ValidationException>(async () =>
                    await _financeService.ApproveActionAsync(actionId, "FUL", ""));

                LogTestDetail(Service, "ApproveActionAsync", "Non-REF action type throws ValidationException", new { actionId, actionType = "BAN" }, "ValidationException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ApproveActionAsync", "Non-REF action type throws ValidationException", new { actionId, actionType = "BAN" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_ApproveActionAsync_NotFound_ThrowsNotFoundException
        [Test]
        public async Task Test_ApproveActionAsync_NotFound_ThrowsNotFoundException()
        {
            // Arrange: action does not exist
            _adminActionRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((AdminAction?)null);

            try
            {
                // Act + Assert: approving a missing action throws NotFoundException
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _financeService.ApproveActionAsync(999, "FUL", ""));

                LogTestDetail(Service, "ApproveActionAsync", "Approve non-existent action throws NotFoundException", new { actionId = 999 }, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ApproveActionAsync", "Approve non-existent action throws NotFoundException", new { actionId = 999 }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RespondToTicketAsync_Success
        [Test]
        public async Task Test_RespondToTicketAsync_Success()
        {
            int ticketId = 5;

            // Arrange: an open ticket pointing to a temp JSON file
            string tempFile = Path.Combine(Path.GetTempPath(), $"finance_ticket_{ticketId}.json");
            await File.WriteAllTextAsync(tempFile,
                JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "Subject", "Refund Delay" },
                    { "Message", "My refund hasn't arrived after 7 days." }
                }));

            var ticket = new SupportTicket
            {
                Ticket_Id  = ticketId,
                User_Id    = 2,
                ConcernUrl = tempFile,
                Status     = "Open"
            };

            var user = new User { User_Id = 2, Name = TestName, Email = TestEmail };

            _supportTicketRepositoryMock.Setup(r => r.GetByIdAsync(ticketId)).ReturnsAsync(ticket);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);
            _supportTicketRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SupportTicket>())).Returns(Task.CompletedTask);

            try
            {
                // Act: finance team responds to the support ticket
                var result = await _financeService.RespondToTicketAsync(ticketId, "Your refund has been processed.");

                // Assert: ticket resolved and response true
                Assert.That(result, Is.True);
                Assert.That(ticket.Status, Is.EqualTo("Resolved"));

                LogTestDetail(Service, "RespondToTicketAsync", "Finance team responds to support ticket", new { ticketId }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RespondToTicketAsync", "Finance team responds to support ticket", new { ticketId }, null, false, ex.Message);
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
                // Act + Assert: responding to a missing ticket throws NotFoundException
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _financeService.RespondToTicketAsync(999, "Some response."));

                LogTestDetail(Service, "RespondToTicketAsync", "Respond to non-existent ticket throws NotFoundException", new { ticketId = 999 }, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RespondToTicketAsync", "Respond to non-existent ticket throws NotFoundException", new { ticketId = 999 }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_GetTransactionsPagedAsync_Success
        [Test]
        public async Task Test_GetTransactionsPagedAsync_Success()
        {
            // Arrange: a paged list of transactions
            var transactions = new List<Transaction>
            {
                new Transaction { Transaction_Id = 100001L, Amount = 500.00m, Status = "Success", Transaction_Type = "Booking" },
                new Transaction { Transaction_Id = 100002L, Amount = 200.00m, Status = "Refunded", Transaction_Type = "Refund" }
            };

            var pagedResult = new PagedResult<Transaction>(transactions, 2, 1, 10);

            _transactionRepositoryMock
                .Setup(r => r.GetTransactionsPagedAsync(null, null, null, null, null, null, 1, 10))
                .ReturnsAsync(pagedResult);

            try
            {
                // Act: retrieve paged transactions with no filters
                var result = await _financeService.GetTransactionsPagedAsync(null, null, null, null, null, null, 1, 10);

                // Assert: correct count and totals returned
                Assert.That(result, Is.Not.Null);
                Assert.That(result.TotalCount, Is.EqualTo(2));
                Assert.That(result.Items.Count, Is.EqualTo(2));

                LogTestDetail(Service, "GetTransactionsPagedAsync", "Retrieve paged transaction list for finance dashboard", null, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetTransactionsPagedAsync", "Retrieve paged transaction list for finance dashboard", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_ApproveActionAsync_RefundRemaining_Success
        [Test]
        public async Task Test_ApproveActionAsync_RefundRemaining_Success()
        {
            int actionId = 12;
            var action = new AdminAction
            {
                ActionId = actionId,
                ActionType = "REF",
                TargetType = "ATD",
                TicketId = 502,
                ActionStatus = "Pending"
            };

            _adminActionRepositoryMock.Setup(r => r.GetByIdAsync(actionId)).ReturnsAsync(action);
            _adminActionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<AdminAction>())).Returns(Task.CompletedTask);

            var result = await _financeService.ApproveActionAsync(actionId, "REM", "Remaining refund");
            Assert.That(result, Is.True);
            Assert.That(action.ActionStatus, Is.EqualTo("Processed"));
        }
        #endregion

        #region Test_ApproveActionAsync_RefundNoRefund_Success
        [Test]
        public async Task Test_ApproveActionAsync_RefundNoRefund_Success()
        {
            int actionId = 13;
            var action = new AdminAction
            {
                ActionId = actionId,
                ActionType = "REF",
                TargetType = "ATD",
                TicketId = 503,
                ActionStatus = "Pending"
            };

            _adminActionRepositoryMock.Setup(r => r.GetByIdAsync(actionId)).ReturnsAsync(action);
            _adminActionRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<AdminAction>())).Returns(Task.CompletedTask);

            var result = await _financeService.ApproveActionAsync(actionId, "NOR", "No refund approved");
            Assert.That(result, Is.True);
            Assert.That(action.ActionStatus, Is.EqualTo("Processed"));
        }
        #endregion

        #region Test_ApproveActionAsync_InvalidTargetType_ThrowsValidationException
        [Test]
        public void Test_ApproveActionAsync_InvalidTargetType_ThrowsValidationException()
        {
            int actionId = 14;
            var action = new AdminAction
            {
                ActionId = actionId,
                ActionType = "REF",
                TargetType = "INVALID_TYPE",
                TicketId = 504,
                ActionStatus = "Pending"
            };

            _adminActionRepositoryMock.Setup(r => r.GetByIdAsync(actionId)).ReturnsAsync(action);

            Assert.ThrowsAsync<ValidationException>(async () =>
                await _financeService.ApproveActionAsync(actionId, "FUL", ""));
        }
        #endregion

        #region Test_RespondToTicketAsync_UserNotFound_ThrowsNotFoundException
        [Test]
        public void Test_RespondToTicketAsync_UserNotFound_ThrowsNotFoundException()
        {
            var ticket = new SupportTicket { Ticket_Id = 50, User_Id = 999, ConcernUrl = "concerns/50.json" };
            _supportTicketRepositoryMock.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(ticket);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

            Assert.ThrowsAsync<NotFoundException>(async () =>
                await _financeService.RespondToTicketAsync(50, "Response"));
        }
        #endregion

        #region Test_RespondToTicketAsync_ConcernUrlEmpty_ThrowsValidationException
        [TestCase(null)]
        [TestCase("")]
        public void Test_RespondToTicketAsync_ConcernUrlEmpty_ThrowsValidationException(string? concernUrl)
        {
            var ticket = new SupportTicket { Ticket_Id = 50, User_Id = 10, ConcernUrl = concernUrl! };
            var user = new User { User_Id = 10, Name = TestName, Email = TestEmail };
            _supportTicketRepositoryMock.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(ticket);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);

            Assert.ThrowsAsync<ValidationException>(async () =>
                await _financeService.RespondToTicketAsync(50, "Response"));
        }
        #endregion
    }
}
