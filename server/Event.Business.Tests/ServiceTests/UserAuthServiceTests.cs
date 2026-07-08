using System;
using System.Collections.Generic;
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
    public class UserAuthServiceTests : ServiceTestBase
    {
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<IAdminRepository> _adminRepositoryMock = null!;
        private Mock<ITermsAndConditionsRepository> _termsRepositoryMock = null!;
        private ICacheService _cacheService = null!;
        private IEmailService _emailService = null!;
        private IConfiguration _configuration = null!;
        private OtpService _otpService = null!;
        private JwtTokenGenerator _jwtGenerator = null!;
        private UserAuthService _userAuthService = null!;

        private const string Service = "UserAuthService";
        private const string TestEmail = "keshwarankeerthi@gmail.com";
        private const string TestName = "KeerthiKeswaran";

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
            _termsRepositoryMock = new Mock<ITermsAndConditionsRepository>();

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
            // _cacheService = CreateConcreteCacheService();
            _emailService = CreateMockEmailService();
            _cacheService = CreateMockCacheService();

            _otpService = new OtpService(_emailService, _userRepositoryMock.Object, _adminRepositoryMock.Object, _cacheService);
            _jwtGenerator = new JwtTokenGenerator(_configuration);
            
            _userAuthService = new UserAuthService(
                _userRepositoryMock.Object,
                _otpService,
                _jwtGenerator,
                _termsRepositoryMock.Object,
                _adminRepositoryMock.Object
            );
        }
        #endregion

        #region Test_RegisterUser_InvalidOtp_ThrowsUnauthorizedException
        [Test]
        public async Task Test_RegisterUser_InvalidOtp_ThrowsUnauthorizedException()
        {
            var user = new User { User_Id = 10001, Email = TestEmail, Consented_Terms_Id = "G10001" };
            _termsRepositoryMock.Setup(r => r.GetActiveTermsAsync()).ReturnsAsync(new TermsAndConditions { Terms_Id = "G10001", Is_Active = true });

            try
            {
                Assert.ThrowsAsync<UnauthorizedException>(async () =>
                    await _userAuthService.RegisterUserAsync(user, "Password123")
                );
                LogTestDetail(Service, "RegisterUserAsync", "Register with invalid OTP throws exception", user, "UnauthorizedException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RegisterUserAsync", "Register with invalid OTP throws exception", user, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RegisterUser_TermsMismatch_ThrowsValidationException
        [Test]
        public async Task Test_RegisterUser_TermsMismatch_ThrowsValidationException()
        {
            await _otpService.SendEmailOtpAsync(TestEmail, "registration");
            
            var user = new User { User_Id = 10001, Email = TestEmail, Consented_Terms_Id = "99" };
            _termsRepositoryMock.Setup(r => r.GetActiveTermsAsync()).ReturnsAsync(new TermsAndConditions { Terms_Id = "G10001", Is_Active = true });

            try
            {
                var otp = await GetCapturedOtpAsync(TestEmail, "registration");
                await _otpService.VerifyOtpAsync(TestEmail, otp, "registration");

                Assert.ThrowsAsync<ValidationException>(async () =>
                    await _userAuthService.RegisterUserAsync(user, "Password123")
                );
                LogTestDetail(Service, "RegisterUserAsync", "Register with terms mismatch throws exception", user, "ValidationException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RegisterUserAsync", "Register with terms mismatch throws exception", user, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RegisterUser_Success
        [Test]
        public async Task Test_RegisterUser_Success()
        {
            await _otpService.SendEmailOtpAsync(TestEmail, "registration");
            
            var user = new User { User_Id = 10001, Name = TestName, Email = TestEmail, Consented_Terms_Id = "G10001" };
            _termsRepositoryMock.Setup(r => r.GetActiveTermsAsync()).ReturnsAsync(new TermsAndConditions { Terms_Id = "G10001", Is_Active = true });
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(TestEmail)).ReturnsAsync((User?)null);
            _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            try
            {
                var otp = await GetCapturedOtpAsync(TestEmail, "registration");
                await _otpService.VerifyOtpAsync(TestEmail, otp, "registration");

                var token = await _userAuthService.RegisterUserAsync(user, "Password123");
                Assert.That(token, Is.Not.Null);
                LogTestDetail(Service, "RegisterUserAsync", "Successful registration", user, token, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RegisterUserAsync", "Successful registration", user, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_LoginUser_Success
        [Test]
        public async Task Test_LoginUser_Success()
        {
            var user = new User { User_Id = 10001,
                Name = TestName,
                Email = TestEmail,
                Password_Hash = PasswordHasher.Hash("Password123")
            };

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(TestEmail)).ReturnsAsync(user);

            try
            {
                var token = await _userAuthService.LoginUserAsync(TestEmail, "Password123");
                Assert.That(token, Is.Not.Null);
                LogTestDetail(Service, "LoginUserAsync", "Successful login", new { TestEmail }, token, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "LoginUserAsync", "Successful login", new { TestEmail }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_LoginUser_InvalidPassword_ThrowsUnauthorizedException
        [Test]
        public async Task Test_LoginUser_InvalidPassword_ThrowsUnauthorizedException()
        {
            var user = new User { User_Id = 10001,
                Name = TestName,
                Email = TestEmail,
                Password_Hash = PasswordHasher.Hash("Password123")
            };

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(TestEmail)).ReturnsAsync(user);

            try
            {
                Assert.ThrowsAsync<UnauthorizedException>(async () =>
                    await _userAuthService.LoginUserAsync(TestEmail, "WrongPassword")
                );
                LogTestDetail(Service, "LoginUserAsync", "Login with wrong password throws exception", new { TestEmail }, "UnauthorizedException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "LoginUserAsync", "Login with wrong password throws exception", new { TestEmail }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_ResetUserPassword_Success
        [Test]
        public async Task Test_ResetUserPassword_Success()
        {
            var user = new User { User_Id = 10001, Name = TestName, Email = TestEmail };
            _userRepositoryMock.Setup(r => r.GetByEmailAsync(TestEmail)).ReturnsAsync(user);
            _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            await _otpService.SendEmailOtpAsync(TestEmail, "password-reset");
            
            try
            {
                var result = await _userAuthService.ResetUserPasswordAsync(TestEmail, await GetCapturedOtpAsync(TestEmail, "password-reset"), "NewPassword123");
                Assert.That(result, Is.EqualTo("Password reset successfully."));
                LogTestDetail(Service, "ResetUserPasswordAsync", "Successful password reset", new { TestEmail }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "ResetUserPasswordAsync", "Successful password reset", new { TestEmail }, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
