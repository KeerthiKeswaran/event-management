using System;
using System.IO;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Event.Models;
using Event.Contracts.IRepositories;
using Event.Business.Services;
using Event.Business.Exceptions;
using Event.Contracts.IServices;

namespace Event.Business.Tests.ServiceTests
{
    [TestFixture]
    public class SupportServiceTests : ServiceTestBase
    {
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<ISupportTicketRepository> _supportTicketRepositoryMock = null!;
        private Mock<IFileStorageService> _fileStorageServiceMock = null!;
        private SupportService _supportService = null!;

        private const string Service = "SupportService";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _supportTicketRepositoryMock = new Mock<ISupportTicketRepository>();
            _fileStorageServiceMock = new Mock<IFileStorageService>();

            // Default: SaveTextAsync returns a URL path for the saved file
            _fileStorageServiceMock
                .Setup(x => x.SaveTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string path, string _) => $"/assets/{path}");

            _supportService = new SupportService(
                _userRepositoryMock.Object,
                _supportTicketRepositoryMock.Object,
                _fileStorageServiceMock.Object
            );
        }
        #endregion

        #region Submit Ticket Tests
        [Test]
        public async Task Test_SubmitSupportTicketAsync_Success()
        {
            _userRepositoryMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _supportTicketRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SupportTicket>())).Returns(Task.CompletedTask);

            try
            {
                var result = await _supportService.SubmitSupportTicketAsync(1, "Test Subject", "Test Message", "GEN");
                Assert.That(result, Is.True);
                LogTestDetail(Service, "SubmitSupportTicketAsync", "Submit ticket successfully", new { UserId = 1, Subject = "Test Subject" }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "SubmitSupportTicketAsync", "Submit ticket successfully", new { UserId = 1, Subject = "Test Subject" }, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_SubmitSupportTicketAsync_Success_WithRelatedId()
        {
            _userRepositoryMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            SupportTicket? savedTicket = null;
            _supportTicketRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SupportTicket>()))
                .Callback<SupportTicket>(t => savedTicket = t)
                .Returns(Task.CompletedTask);

            try
            {
                var result = await _supportService.SubmitSupportTicketAsync(1, "Test Subject", "Test Message", "GEN", 12345);
                Assert.That(result, Is.True);
                Assert.That(savedTicket, Is.Not.Null);
                Assert.That(savedTicket!.RelatedId, Is.EqualTo(12345));
                LogTestDetail(Service, "SubmitSupportTicketAsync", "Submit ticket successfully with RelatedId", new { UserId = 1, Subject = "Test Subject", RelatedId = 12345 }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "SubmitSupportTicketAsync", "Submit ticket successfully with RelatedId", new { UserId = 1, Subject = "Test Subject", RelatedId = 12345 }, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_SubmitSupportTicketAsync_UserNotFound_ThrowsNotFoundException()
        {
            _userRepositoryMock.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);

            try
            {
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _supportService.SubmitSupportTicketAsync(999, "Test Subject", "Test Message", "GEN")
                );
                LogTestDetail(Service, "SubmitSupportTicketAsync", "Non-existent user submitting ticket throws exception", new { UserId = 999 }, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "SubmitSupportTicketAsync", "Non-existent user submitting ticket throws exception", new { UserId = 999 }, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_SubmitSupportTicketAsync_CreatesAssetFileAndUpdatesUrl()
        {
            const int userId = 99888;

            _userRepositoryMock.Setup(r => r.ExistsAsync(userId)).ReturnsAsync(true);
            SupportTicket? savedTicket = null;
            _supportTicketRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SupportTicket>()))
                .Callback<SupportTicket>(ticket => savedTicket = ticket)
                .Returns(Task.CompletedTask);
            _supportTicketRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SupportTicket>()))
                .Returns(Task.CompletedTask);

            try
            {
                var result = await _supportService.SubmitSupportTicketAsync(userId, "Asset Subject", "Asset Message", "GEN");

                Assert.That(result, Is.True);

                // Verify the storage service was called with the correct path pattern
                _fileStorageServiceMock.Verify(
                    x => x.SaveTextAsync(
                        It.Is<string>(path => path.Contains($"users/{userId}/support")),
                        It.Is<string>(content => content.Contains("Asset Subject") && content.Contains("Asset Message"))
                    ),
                    Times.Once
                );

                Assert.That(savedTicket, Is.Not.Null);
                Assert.That(savedTicket!.ConcernUrl, Does.Contain($"/assets/users/{userId}/support/"));

                LogTestDetail(Service, "SubmitSupportTicketAsync", "Create asset file and update concern URL", new { UserId = userId }, savedTicket.ConcernUrl, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "SubmitSupportTicketAsync", "Create asset file and update concern URL", new { UserId = userId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
