using System;
using System.Threading.Tasks;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Business.Helpers;
using Event.Business.Exceptions;

namespace Event.Business.Services
{
    public class DeptAuthService : IDeptAuthService
    {
        #region Fields

        private readonly IAdminRepository _adminRepository;
        private readonly OtpService _otpService;
        private readonly JwtTokenGenerator _jwtGenerator;

        #endregion

        #region Constructor

        public DeptAuthService(
            IAdminRepository adminRepository,
            OtpService otpService,
            JwtTokenGenerator jwtGenerator)
        {
            _adminRepository = adminRepository;
            _otpService      = otpService;
            _jwtGenerator    = jwtGenerator;
        }

        #endregion

        #region LoginAdminAsync

        public async Task<string?> LoginAdminAsync(string adminId, string password, string role)
        {
            // 1. Enforce ID-prefix / role binding before any DB lookup
            if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                // Admin IDs must start with "ADM". Finance IDs are not permitted here.
                if (!adminId.StartsWith("ADM", StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedException(
                        "Access denied. Finance credentials cannot be used on the admin login portal.");
            }
            else if (string.Equals(role, "finance", StringComparison.OrdinalIgnoreCase))
            {
                // Finance IDs must start with "FIN". Admin IDs are not permitted here.
                if (!adminId.StartsWith("FIN", StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedException(
                        "Access denied. Admin credentials cannot be used on the finance login portal.");
            }

            // 2. Fetch record by admin identifier
            var admin = await _adminRepository.GetByAdminIdAsync(adminId);
            if (admin == null)
                throw new UnauthorizedException("Invalid administrator credentials.");

            // 3. Validate hash of input password
            if (!PasswordHasher.Verify(password, admin.Password_Hash))
                throw new UnauthorizedException("Invalid administrator credentials.");

            // 4. If role is finance, trigger OTP instead of immediately returning token
            if (string.Equals(role, "finance", StringComparison.OrdinalIgnoreCase))
            {
                await _otpService.SendEmailOtpAsync(admin.Email, "finance-login");
                return "OTP_SENT";
            }

            // 5. Return a signed administrator JWT token
            return _jwtGenerator.GenerateAdminToken(admin.Admin_Id, admin.Email, admin.Name);
        }

        #endregion

        #region VerifyFinanceLoginOtpAsync

        public async Task<string?> VerifyFinanceLoginOtpAsync(string adminId, string otp)
        {
            // 1. Enforce Finance ID prefix before any DB lookup or OTP verification
            if (!adminId.StartsWith("FIN", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedException(
                    "Access denied. Only finance accounts can complete the finance OTP verification.");

            var admin = await _adminRepository.GetByAdminIdAsync(adminId);
            if (admin == null)
                throw new UnauthorizedException("Invalid administrator credentials.");

            if (!await _otpService.VerifyOtpAsync(admin.Email, otp, "finance-login"))
                throw new UnauthorizedException("Invalid or expired OTP.");

            return _jwtGenerator.GenerateAdminToken(admin.Admin_Id, admin.Email, admin.Name);
        }

        #endregion

        #region ResetAdminPasswordAsync

        public async Task<string> ResetAdminPasswordAsync(string email, string otp, string newPassword)
        {
            // 1. Verify the OTP details (if we're passing it here, maybe they didn't pre-verify)
            if (await _otpService.IsOtpVerificationExpiredAsync(email, "admin-password-reset"))
            {
                // If the marker isn't there, maybe they are doing it in one step. Try to verify the raw OTP.
                if (!await _otpService.VerifyOtpAsync(email, otp, "admin-password-reset"))
                    throw new UnauthorizedException("Invalid or expired OTP.");
            }

            // Consume the marker so it can't be reused
            await _otpService.ConsumeOtpVerificationAsync(email, "admin-password-reset");

            // 2. Retrieve the admin record to update
            var admin = await _adminRepository.GetByEmailAsync(email);
            if (admin == null)
                throw new NotFoundException("No administrator account registered with this email address.");

            // 3. Update the password hash and save to database
            admin.Password_Hash        = PasswordHasher.Hash(newPassword);

            await _adminRepository.UpdateAsync(admin);
            return "Admin password reset successfully.";
        }

        #endregion
    }
}
