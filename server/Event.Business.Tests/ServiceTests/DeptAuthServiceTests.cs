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
using Event.Business.Helpers;
using Event.Business.Exceptions;

namespace Event.Business.Tests.ServiceTests
{
    [TestFixture]
    public class DeptAuthServiceTests : ServiceTestBase
    {
        private Mock<IAdminRepository> _adminRepositoryMock = null!;
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private ICacheService _cacheService = null!;
        private IEmailService _emailService = null!;
        private IConfiguration _configuration = null!;
        private OtpService _otpService = null!;
        private JwtTokenGenerator _jwtGenerator = null!;
        private DeptAuthService _deptAuthService = null!;

        private const string Service = "DeptAuthService";
        private const string TestEmail = "keshwarankeerthi@gmail.com";
        private const string TestName = "KeerthiKeswaran";
        private const string AdminId    = "ADM_1001";
        private const string FinanceId  = "FIN_2001";

        private async Task<string> GetCapturedOtpAsync(string email, string purpose)
        {
            return await _cacheService.GetAsync<string>($"otp:{purpose}:{email}") ?? "";
        }

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _adminRepositoryMock = new Mock<IAdminRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();

            var inMemorySettings = new Dictionary<string, string?> {
                {"Jwt:SecretKey", "super_secret_key_123456789012345678"},
                {"Jwt:Issuer", "EventPlatform"},
                {"Jwt:Audience", "EventPlatformUsers"},
                {"Jwt:ExpiryHours", "24"}
            };

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string apiDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Event.API"));
            string appSettingsPath = Path.Combine(apiDir, "appsettings.json");

            _configuration = new ConfigurationBuilder()
                .AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false)
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // _emailService = CreateConcreteEmailService(_configuration);
            _emailService = CreateMockEmailService();
            _cacheService = CreateConcreteCacheService();

            _otpService = new OtpService(_emailService, _userRepositoryMock.Object, _adminRepositoryMock.Object, _cacheService);
            _jwtGenerator = new JwtTokenGenerator(_configuration);
            _deptAuthService = new DeptAuthService(_adminRepositoryMock.Object, _otpService, _jwtGenerator);
        }
        #endregion

        #region Test_LoginAdmin_Success
        [Test]
        public async Task Test_LoginAdmin_Success()
        {
            var admin = new Admin
            {
                Admin_Id = AdminId,
                Email = TestEmail,
                Name = TestName,
                Password_Hash = PasswordHasher.Hash("SecurePassword123")
            };

            _adminRepositoryMock.Setup(r => r.GetByAdminIdAsync(AdminId)).ReturnsAsync(admin);

            try
            {
                var token = await _deptAuthService.LoginAdminAsync(AdminId, "SecurePassword123", "admin");
                Assert.That(token, Is.Not.Null);
                LogTestDetail(Service, "LoginAdminAsync", "Successful admin login", new { AdminId }, token, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "LoginAdminAsync", "Successful admin login", new { AdminId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_LoginAdmin_InvalidPassword_ThrowsUnauthorizedException
        [Test]
        public async Task Test_LoginAdmin_InvalidPassword_ThrowsUnauthorizedException()
        {
            var admin = new Admin
            {
                Admin_Id = AdminId,
                Email = TestEmail,
                Name = TestName,
                Password_Hash = PasswordHasher.Hash("SecurePassword123")
            };

            _adminRepositoryMock.Setup(r => r.GetByAdminIdAsync(AdminId)).ReturnsAsync(admin);

            try
            {
                Assert.ThrowsAsync<UnauthorizedException>(async () =>
                    await _deptAuthService.LoginAdminAsync(AdminId, "WrongPassword", "admin")
                );
                LogTestDetail(Service, "LoginAdminAsync", "Login with wrong password throws exception", new { AdminId }, "UnauthorizedException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "LoginAdminAsync", "Login with wrong password throws exception", new { AdminId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_LoginAdmin_NonExistentAdmin_ThrowsUnauthorizedException
        [Test]
        public async Task Test_LoginAdmin_NonExistentAdmin_ThrowsUnauthorizedException()
        {
            _adminRepositoryMock.Setup(r => r.GetByAdminIdAsync("ADM_9999")).ReturnsAsync((Admin?)null);

            try
            {
                Assert.ThrowsAsync<UnauthorizedException>(async () =>
                    await _deptAuthService.LoginAdminAsync("ADM_9999", "SecurePassword123", "admin")
                );
                LogTestDetail(Service, "LoginAdminAsync", "Login with non-existent admin ID throws exception", new { AdminId = "ADM_9999" }, "UnauthorizedException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "LoginAdminAsync", "Login with non-existent admin ID throws exception", new { AdminId = "ADM_9999" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_LoginAdmin_WithFinanceId_ThrowsUnauthorizedException
        [Test]
        public async Task Test_LoginAdmin_WithFinanceId_ThrowsUnauthorizedException()
        {
            // Arrange: a finance-prefixed ID used on the admin login portal
            try
            {
                // Act + Assert: FIN_ ID on admin portal is immediately rejected before DB lookup
                Assert.ThrowsAsync<UnauthorizedException>(async () =>
                    await _deptAuthService.LoginAdminAsync(FinanceId, "AnyPassword", "admin")
                );
                LogTestDetail(Service, "LoginAdminAsync", "Finance ID on admin portal rejected before DB lookup", new { FinanceId }, "UnauthorizedException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "LoginAdminAsync", "Finance ID on admin portal rejected before DB lookup", new { FinanceId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_LoginFinance_WithAdminId_ThrowsUnauthorizedException
        [Test]
        public async Task Test_LoginFinance_WithAdminId_ThrowsUnauthorizedException()
        {
            // Arrange: an admin-prefixed ID used on the finance login portal
            try
            {
                // Act + Assert: ADM_ ID on finance portal rejected before DB lookup or OTP dispatch
                Assert.ThrowsAsync<UnauthorizedException>(async () =>
                    await _deptAuthService.LoginAdminAsync(AdminId, "AnyPassword", "finance")
                );
                LogTestDetail(Service, "LoginAdminAsync", "Admin ID on finance portal rejected before DB lookup and OTP dispatch", new { AdminId }, "UnauthorizedException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "LoginAdminAsync", "Admin ID on finance portal rejected before DB lookup and OTP dispatch", new { AdminId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_VerifyFinanceLoginOtp_WithAdminId_ThrowsUnauthorizedException
        [Test]
        public async Task Test_VerifyFinanceLoginOtp_WithAdminId_ThrowsUnauthorizedException()
        {
            // Arrange: an ADM_ ID used in the finance OTP verification step
            try
            {
                // Act + Assert: ADM_ ID rejected before OTP cache is even consulted
                Assert.ThrowsAsync<UnauthorizedException>(async () =>
                    await _deptAuthService.VerifyFinanceLoginOtpAsync(AdminId, "123456")
                );
                LogTestDetail(Service, "VerifyFinanceLoginOtpAsync", "Admin ID on finance OTP step rejected before cache lookup", new { AdminId }, "UnauthorizedException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "VerifyFinanceLoginOtpAsync", "Admin ID on finance OTP step rejected before cache lookup", new { AdminId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_LoginFinance_OtpSent
        [Test]
        public async Task Test_LoginFinance_OtpSent()
        {
            var admin = new Admin
            {
                Admin_Id = FinanceId,
                Email = TestEmail,
                Name = TestName,
                Password_Hash = PasswordHasher.Hash("SecurePassword123")
            };

            _adminRepositoryMock.Setup(r => r.GetByAdminIdAsync(FinanceId)).ReturnsAsync(admin);
            _adminRepositoryMock.Setup(r => r.GetByEmailAsync(TestEmail)).ReturnsAsync(admin);

            try
            {
                var result = await _deptAuthService.LoginAdminAsync(FinanceId, "SecurePassword123", "finance");
                Assert.That(result, Is.EqualTo("OTP_SENT"));
                LogTestDetail(Service, "LoginAdminAsync", "Finance login triggers OTP", new { FinanceId }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "LoginAdminAsync", "Finance login triggers OTP", new { FinanceId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_VerifyFinanceLoginOtp_Success
        [Test]
        public async Task Test_VerifyFinanceLoginOtp_Success()
        {
            var admin = new Admin
            {
                Admin_Id = FinanceId,
                Email = TestEmail,
                Name = TestName,
                Password_Hash = PasswordHasher.Hash("SecurePassword123")
            };

            _adminRepositoryMock.Setup(r => r.GetByAdminIdAsync(FinanceId)).ReturnsAsync(admin);
            _adminRepositoryMock.Setup(r => r.GetByEmailAsync(TestEmail)).ReturnsAsync(admin);

            await _otpService.SendEmailOtpAsync(TestEmail, "finance-login");

            try
            {
                var token = await _deptAuthService.VerifyFinanceLoginOtpAsync(FinanceId, await GetCapturedOtpAsync(TestEmail, "finance-login"));
                Assert.That(token, Is.Not.Null);
                LogTestDetail(Service, "VerifyFinanceLoginOtpAsync", "Successful OTP verification", new { FinanceId }, token, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "VerifyFinanceLoginOtpAsync", "Successful OTP verification", new { FinanceId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_VerifyFinanceLoginOtp_InvalidOtp_ThrowsUnauthorizedException
        [Test]
        public async Task Test_VerifyFinanceLoginOtp_InvalidOtp_ThrowsUnauthorizedException()
        {
            var admin = new Admin
            {
                Admin_Id = FinanceId,
                Email = TestEmail,
                Name = TestName,
                Password_Hash = PasswordHasher.Hash("SecurePassword123")
            };

            _adminRepositoryMock.Setup(r => r.GetByAdminIdAsync(FinanceId)).ReturnsAsync(admin);

            try
            {
                Assert.ThrowsAsync<UnauthorizedException>(async () =>
                    await _deptAuthService.VerifyFinanceLoginOtpAsync(FinanceId, "000000")
                );
                LogTestDetail(Service, "VerifyFinanceLoginOtpAsync", "Invalid OTP verification throws exception", new { FinanceId }, "UnauthorizedException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "VerifyFinanceLoginOtpAsync", "Invalid OTP verification throws exception", new { FinanceId }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_ResetAdminPassword_InvalidOtp_ThrowsUnauthorizedException
        [Test]
        public async Task Test_ResetAdminPassword_InvalidOtp_ThrowsUnauthorizedException()
        {
            try
            {
                Assert.ThrowsAsync<UnauthorizedException>(async () =>
                    await _deptAuthService.ResetAdminPasswordAsync(TestEmail, "000000", "NewSecurePassword123")
                );
                LogTestDetail(Service, "ResetAdminPasswordAsync", "Reset with invalid OTP throws exception", new { TestEmail }, "UnauthorizedException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ResetAdminPasswordAsync", "Reset with invalid OTP throws exception", new { TestEmail }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_ResetAdminPassword_Success
        [Test]
        public async Task Test_ResetAdminPassword_Success()
        {
            var admin = new Admin
            {
                Admin_Id = AdminId,
                Email = TestEmail,
                Name = TestName
            };

            _adminRepositoryMock.Setup(r => r.GetByEmailAsync(TestEmail)).ReturnsAsync(admin);
            _adminRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Admin>())).Returns(Task.CompletedTask);

            await _otpService.SendEmailOtpAsync(TestEmail, "admin-password-reset");
            
            try
            {
                var result = await _deptAuthService.ResetAdminPasswordAsync(TestEmail, await GetCapturedOtpAsync(TestEmail, "admin-password-reset"), "NewSecurePassword123");
                Assert.That(result, Is.EqualTo("Admin password reset successfully."));
                LogTestDetail(Service, "ResetAdminPasswordAsync", "Successful password reset", new { TestEmail }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ResetAdminPasswordAsync", "Successful password reset", new { TestEmail }, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
