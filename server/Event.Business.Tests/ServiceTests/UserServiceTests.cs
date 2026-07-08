using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
    public class UserServiceTests : ServiceTestBase
    {
        private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<IEventRepository> _eventRepositoryMock = null!;
        private Mock<ICacheService> _cacheServiceMock = null!;
        private Mock<IAdminRepository> _adminRepositoryMock = null!;
        private OtpService _otpService = null!;
        private IEmailService _emailService = null!;
        private UserService _userService = null!;

        private const string Service = "UserService";
        private const string TestEmail = "keshwarankeerthi@gmail.com";
        private const string TestName = "KeerthiKeswaran";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _eventRepositoryMock = new Mock<IEventRepository>();
            _cacheServiceMock = new Mock<ICacheService>();
            _adminRepositoryMock = new Mock<IAdminRepository>();
            _emailService = CreateMockEmailService();

            _otpService = new OtpService(
                _emailService,
                _userRepositoryMock.Object,
                _adminRepositoryMock.Object,
                _cacheServiceMock.Object
            );

            _userService = new UserService(
                _httpContextAccessorMock.Object, 
                _userRepositoryMock.Object, 
                _eventRepositoryMock.Object,
                _otpService,
                _emailService,
                _adminRepositoryMock.Object
            );
        }
        #endregion

        #region GetCurrentUserId Tests
        [Test]
        public void Test_GetCurrentUserId_FromClaims_Success()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal };
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

            try
            {
                var userId = _userService.GetCurrentUserId();
                Assert.That(userId, Is.EqualTo(1));
                LogTestDetail(Service, "GetCurrentUserId", "Retrieve user ID from ClaimsPrincipal", null, userId, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetCurrentUserId", "Retrieve user ID from ClaimsPrincipal", null, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_GetCurrentUserId_FromHeaders_Success()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-User-Id"] = "2";
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

            try
            {
                var userId = _userService.GetCurrentUserId();
                Assert.That(userId, Is.EqualTo(2));
                LogTestDetail(Service, "GetCurrentUserId", "Retrieve user ID from Request Headers", null, userId, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetCurrentUserId", "Retrieve user ID from Request Headers", null, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_GetCurrentUserId_MissingId_ThrowsUnauthorizedException()
        {
            var httpContext = new DefaultHttpContext();
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

            try
            {
                Assert.Throws<UnauthorizedException>(() => _userService.GetCurrentUserId());
                LogTestDetail(Service, "GetCurrentUserId", "Missing identification throws exception", null, "UnauthorizedException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetCurrentUserId", "Missing identification throws exception", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region SelectInterestedRegionsAsync Tests
        [Test]
        public async Task Test_SelectInterestedRegionsAsync_Success()
        {
            _userRepositoryMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _userRepositoryMock.Setup(r => r.UpdateInterestedRegionsAsync(1, It.IsAny<IEnumerable<string>>())).Returns(Task.CompletedTask);

            try
            {
                var result = await _userService.SelectInterestedRegionsAsync(1, "US-EAST");
                Assert.That(result, Is.True);
                LogTestDetail(Service, "SelectInterestedRegionsAsync", "Select interested regions for user successfully", new { UserId = 1, Region = "US-EAST" }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "SelectInterestedRegionsAsync", "Select interested regions for user successfully", new { UserId = 1, Region = "US-EAST" }, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_SelectInterestedRegionsAsync_UserNotFound_ThrowsNotFoundException()
        {
            _userRepositoryMock.Setup(r => r.ExistsAsync(999)).ReturnsAsync(false);

            try
            {
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _userService.SelectInterestedRegionsAsync(999, "US-EAST")
                );
                LogTestDetail(Service, "SelectInterestedRegionsAsync", "Non-existent user throws not found exception", new { UserId = 999 }, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "SelectInterestedRegionsAsync", "Non-existent user throws not found exception", new { UserId = 999 }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region UpdateUserProfileAsync Tests
        [Test]
        public async Task Test_UpdateUserProfileAsync_Success()
        {
            var user = new User { User_Id = 1, Name = TestName, Email = TestEmail, Mobile_Number = "1234" };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _userRepositoryMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);

            try
            {
                var result = await _userService.UpdateUserProfileAsync(1, new UpdateProfileRequest { Name = "Updated Name", MobileNumber = "9876" });
                Assert.That(result, Is.True);
                Assert.That(user.Name, Is.EqualTo("Updated Name"));
                LogTestDetail(Service, "UpdateUserProfileAsync", "Update user profile", new { UserId = 1, Name = "Updated Name" }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "UpdateUserProfileAsync", "Update user profile", new { UserId = 1, Name = "Updated Name" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region GetUserProfileAsync Tests
        [Test]
        public async Task Test_GetUserProfileAsync_Success()
        {
            var user = new User 
            { 
                User_Id = 1, 
                Name = TestName, 
                Email = TestEmail,
                InterestedRegions = new List<UserInterestedRegion>
                {
                    new UserInterestedRegion { Region_Id = "US-EAST" }
                }
            };
            _userRepositoryMock.Setup(r => r.GetUserProfileAsync(1)).ReturnsAsync(user);

            try
            {
                var result = await _userService.GetUserProfileAsync(1);
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Name, Is.EqualTo(TestName));
                Assert.That(result.RegionId, Is.EqualTo("US-EAST"));
                LogTestDetail(Service, "GetUserProfileAsync", "Retrieve user profile details", 1, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetUserProfileAsync", "Retrieve user profile details", 1, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region GetMyEventsAsync Tests
        [Test]
        public async Task Test_GetMyEventsAsync_Success()
        {
            _userRepositoryMock.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            var mockEvents = new List<Event.Models.Event>
            {
                new Event.Models.Event { Event_Id = 101, Title = "Test Event 1", Event_Type = "Physical", Date_Time = DateTime.UtcNow }
            };
            _eventRepositoryMock.Setup(r => r.GetEventsByOrganizerAsync(1)).ReturnsAsync(mockEvents);

            try
            {
                var result = await _userService.GetMyEventsAsync(1);
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count(), Is.EqualTo(1));
                LogTestDetail(Service, "GetMyEventsAsync", "Retrieve my events list overview", 1, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetMyEventsAsync", "Retrieve my events list overview", 1, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region GetMyEventDetailsAsync Tests
        [Test]
        public async Task Test_GetMyEventDetailsAsync_Success()
        {
            var mockEvent = new Event.Models.Event 
            { 
                Event_Id = 101, 
                Organizer_Id = 1, 
                Title = "Test Event 1", 
                Event_Type = "Virtual", 
                Virtual_Url = "http://jitsi",
                Virtual_Password_Hash = "hashed"
            };
            _eventRepositoryMock.Setup(r => r.GetEventDetailsAsync(101)).ReturnsAsync(mockEvent);

            try
            {
                var result = await _userService.GetMyEventDetailsAsync(1, 101);
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Virtual_Url, Is.EqualTo("http://jitsi"));
                LogTestDetail(Service, "GetMyEventDetailsAsync", "Retrieve my event full details", 101, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetMyEventDetailsAsync", "Retrieve my event full details", 101, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region CloseAccountAsync Tests
        [Test]
        public async Task Test_CloseAccountAsync_Success()
        {
            int userId = 1;
            var mockUser = new User
            {
                User_Id = userId,
                Name = "KeerthiKeswaran",
                Email = "keshwarankeerthi@gmail.com",
                Status = "Active"
            };

            var request = new CloseAccountRequest
            {
                Reason = "No longer needed",
                Explanation = "Closing my account",
                ConfirmName = "KeerthiKeswaran",
                Otp = "123456"
            };

            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(mockUser);
            _cacheServiceMock.Setup(c => c.GetAsync<string>("otp:close-account:keshwarankeerthi@gmail.com")).ReturnsAsync("123456");
            _userRepositoryMock.Setup(r => r.UpdateAsync(mockUser)).Returns(Task.CompletedTask);

            try
            {
                var result = await _userService.CloseAccountAsync(userId, request);
                Assert.That(result, Is.True);
                Assert.That(mockUser.Status, Is.EqualTo("Deactivated"));
                LogTestDetail(Service, "CloseAccountAsync", "Deactivate account successfully", userId, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CloseAccountAsync", "Deactivate account successfully", userId, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_CloseAccountAsync_UserNotFound_ThrowsNotFoundException()
        {
            int userId = 999;
            var request = new CloseAccountRequest { Otp = "123456", ConfirmName = "Keerthi" };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

            try
            {
                Assert.ThrowsAsync<NotFoundException>(async () =>
                    await _userService.CloseAccountAsync(userId, request)
                );
                LogTestDetail(Service, "CloseAccountAsync", "User not found throws NotFoundException", userId, "NotFoundException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CloseAccountAsync", "User not found throws NotFoundException", userId, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_CloseAccountAsync_AlreadyDeactivated_ThrowsValidationException()
        {
            int userId = 1;
            var mockUser = new User
            {
                User_Id = userId,
                Name = "KeerthiKeswaran",
                Email = "keshwarankeerthi@gmail.com",
                Status = "Deactivated"
            };
            var request = new CloseAccountRequest { Otp = "123456", ConfirmName = "KeerthiKeswaran" };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(mockUser);

            try
            {
                Assert.ThrowsAsync<ValidationException>(async () =>
                    await _userService.CloseAccountAsync(userId, request)
                );
                LogTestDetail(Service, "CloseAccountAsync", "Already deactivated throws ValidationException", userId, "ValidationException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CloseAccountAsync", "Already deactivated throws ValidationException", userId, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_CloseAccountAsync_InvalidOtp_ThrowsUnauthorizedException()
        {
            int userId = 1;
            var mockUser = new User
            {
                User_Id = userId,
                Name = "KeerthiKeswaran",
                Email = "keshwarankeerthi@gmail.com",
                Status = "Active"
            };
            var request = new CloseAccountRequest { Otp = "999999", ConfirmName = "KeerthiKeswaran" };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(mockUser);
            _cacheServiceMock.Setup(c => c.GetAsync<string>("otp:close-account:keshwarankeerthi@gmail.com")).ReturnsAsync("123456");

            try
            {
                Assert.ThrowsAsync<UnauthorizedException>(async () =>
                    await _userService.CloseAccountAsync(userId, request)
                );
                LogTestDetail(Service, "CloseAccountAsync", "Invalid OTP throws UnauthorizedException", userId, "UnauthorizedException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CloseAccountAsync", "Invalid OTP throws UnauthorizedException", userId, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_CloseAccountAsync_MismatchedName_ThrowsValidationException()
        {
            int userId = 1;
            var mockUser = new User
            {
                User_Id = userId,
                Name = "KeerthiKeswaran",
                Email = "keshwarankeerthi@gmail.com",
                Status = "Active"
            };
            var request = new CloseAccountRequest { Otp = "123456", ConfirmName = "WrongName" };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(mockUser);
            _cacheServiceMock.Setup(c => c.GetAsync<string>("otp:close-account:keshwarankeerthi@gmail.com")).ReturnsAsync("123456");

            try
            {
                Assert.ThrowsAsync<ValidationException>(async () =>
                    await _userService.CloseAccountAsync(userId, request)
                );
                LogTestDetail(Service, "CloseAccountAsync", "Mismatched name throws ValidationException", userId, "ValidationException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CloseAccountAsync", "Mismatched name throws ValidationException", userId, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
