using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Event.Models;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Business.Services;
using Event.Business.Exceptions;

namespace Event.Business.Tests.ServiceTests
{
    [TestFixture]
    public class OtpServiceTests : ServiceTestBase
    {
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<IAdminRepository> _adminRepositoryMock = null!;
        private ICacheService _cacheService = null!;
        private IConfiguration _configuration = null!;
        private IEmailService _emailService = null!;
        private OtpService _otpService = null!;

        private const string Service = "OtpService";
        private const string TestEmail = "keshwarankeerthi@gmail.com";

        private async Task<string> GetCapturedOtpAsync(string email, string purpose)
        {
            return await _cacheService.GetAsync<string>($"otp:{purpose}:{email}") ?? "";
        }

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _adminRepositoryMock = new Mock<IAdminRepository>();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string apiDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Event.API"));
            string appSettingsPath = Path.Combine(apiDir, "appsettings.json");

            _configuration = new ConfigurationBuilder()
                .AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false)
                .Build();

            // _emailService = CreateConcreteEmailService(_configuration);
            _emailService = CreateMockEmailService();
            _cacheService = CreateConcreteCacheService();

            _otpService = new OtpService(
                _emailService,
                _userRepositoryMock.Object,
                _adminRepositoryMock.Object,
                _cacheService
            );
        }
        #endregion

        #region Test_SendEmailOtpAsync_Registration_Success
        [Test]
        public async Task Test_SendEmailOtpAsync_Registration_Success()
        {
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(TestEmail)).ReturnsAsync((User?)null);

            try
            {
                await _otpService.SendEmailOtpAsync(TestEmail, "registration");
                LogTestDetail(Service, "SendEmailOtpAsync", "Send OTP for registration successfully", TestEmail, "Sent", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "SendEmailOtpAsync", "Send OTP for registration successfully", TestEmail, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_SendEmailOtpAsync_Registration_Conflict_ThrowsConflictException
        [Test]
        public async Task Test_SendEmailOtpAsync_Registration_Conflict_ThrowsConflictException()
        {
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(TestEmail)).ReturnsAsync(new User { User_Id = 10001, Email = TestEmail });

            try
            {
                Assert.ThrowsAsync<ConflictException>(async () =>
                    await _otpService.SendEmailOtpAsync(TestEmail, "registration")
                );
                LogTestDetail(Service, "SendEmailOtpAsync", "Send OTP for already registered email throws conflict exception", TestEmail, "ConflictException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "SendEmailOtpAsync", "Send OTP for already registered email throws conflict exception", TestEmail, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_VerifyOtp_Success
        [Test]
        public async Task Test_VerifyOtp_Success()
        {
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(TestEmail)).ReturnsAsync((User?)null);
            await _otpService.SendEmailOtpAsync(TestEmail, "registration");

            try
            {
                var verified = await _otpService.VerifyOtpAsync(TestEmail, await GetCapturedOtpAsync(TestEmail, "registration"), "registration");
                Assert.That(verified, Is.True);
                LogTestDetail(Service, "VerifyOtp", "Successfully verify OTP", new { TestEmail, Otp = await GetCapturedOtpAsync(TestEmail, "registration") }, verified, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "VerifyOtp", "Successfully verify OTP", new { TestEmail, Otp = await GetCapturedOtpAsync(TestEmail, "registration") }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_VerifyOtp_InvalidOtp_ReturnsFalse
        [Test]
        public async Task Test_VerifyOtp_InvalidOtp_ReturnsFalse()
        {
            try
            {
                var verified = await _otpService.VerifyOtpAsync(TestEmail, "000000", "registration");
                Assert.That(verified, Is.False);
                LogTestDetail(Service, "VerifyOtp", "Verify with invalid OTP returns false", new { TestEmail, Otp = "000000" }, verified, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "VerifyOtp", "Verify with invalid OTP returns false", new { TestEmail, Otp = "000000" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
