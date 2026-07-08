using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Event.Contracts.IServices;
using Event.Models;
using Event.Models.DTOs;
using Event.Business.Services;

namespace Event.API.Controllers
{
    [ApiController]
    [Route("api/auth/user")]
    public class UserAuthController : ControllerBase
    {
        private readonly IUserAuthService _userAuthService;
        private readonly OtpService _otpService;

        public UserAuthController(IUserAuthService userAuthService, OtpService otpService)
        {
            _userAuthService = userAuthService;
            _otpService = otpService;
        }

        [AllowAnonymous]
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            await _otpService.SendEmailOtpAsync(request.Email, request.Purpose);
            return Ok(new { Message = "OTP sent successfully." });
        }

        [AllowAnonymous]
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var success = await _otpService.VerifyOtpAsync(request.Email, request.Otp, request.Purpose);
            if (!success)
                return BadRequest(new { Message = "Invalid or expired OTP." });

            return Ok(new { Message = "OTP verified successfully." });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                Mobile_Number = request.MobileNumber,
                Consented_Terms_Id = request.ConsentedTermsId,
                Has_Marketing_Consent = request.HasMarketingConsent
            };

            var token = await _userAuthService.RegisterUserAsync(user, request.Password);
            return Ok(new { Token = token, Message = "User registered successfully." });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var token = await _userAuthService.LoginUserAsync(request.Email, request.Password);
            return Ok(new { Token = token });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var message = await _userAuthService.ResetUserPasswordAsync(request.Email, request.Otp, request.NewPassword);
            return Ok(new { Message = message });
        }

        [AllowAnonymous]
        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { Message = "Email is required." });

            var exists = await _userAuthService.CheckEmailExistsAsync(email);
            return Ok(new { Exists = exists });
        }
    }
}
