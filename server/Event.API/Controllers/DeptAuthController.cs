using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Event.Contracts.IServices;
using Event.Models.DTOs;
using Event.Business.Services;

namespace Event.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class DeptAuthController : ControllerBase
    {
        private readonly IDeptAuthService _deptAuthService;
        private readonly OtpService _otpService;

        public DeptAuthController(IDeptAuthService deptAuthService, OtpService otpService)
        {
            _deptAuthService = deptAuthService;
            _otpService = otpService;
        }

        [HttpPost("admin/login")]
        public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest request)
        {
            var token = await _deptAuthService.LoginAdminAsync(request.AdminId, request.Password, "admin");
            return Ok(new { Token = token });
        }

        [HttpPost("finance/login")]
        public async Task<IActionResult> FinanceLogin([FromBody] AdminLoginRequest request)
        {
            var result = await _deptAuthService.LoginAdminAsync(request.AdminId, request.Password, "finance");
            if (result == "OTP_SENT")
            {
                return Ok(new { OtpRequired = true, Message = "OTP has been sent to your registered email." });
            }
            return Ok(new { Token = result });
        }

        [HttpPost("finance/login/verify")]
        public async Task<IActionResult> VerifyFinanceLoginOtp([FromBody] FinanceLoginVerifyRequest request)
        {
            var token = await _deptAuthService.VerifyFinanceLoginOtpAsync(request.AdminId, request.Otp);
            return Ok(new { Token = token });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _otpService.SendEmailOtpAsync(request.Email, "admin-password-reset");
            return Ok(new { Message = "OTP has been sent to your registered email." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var message = await _deptAuthService.ResetAdminPasswordAsync(request.Email, request.Otp, request.NewPassword);
            return Ok(new { Message = message });
        }
    }
}
