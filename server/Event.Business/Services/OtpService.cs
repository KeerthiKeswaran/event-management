using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Event.Contracts.IServices;
using Event.Contracts.IRepositories;
using Event.Business.Exceptions;
using Event.Models.DTOs;
using System.Collections.Generic;

namespace Event.Business.Services
{
    public class OtpService
    {
        #region Fields

        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly ICacheService _cacheService;

        // OTP is valid for 10 minutes
        private const int OtpExpiryMinutes = 10;

        #endregion

        #region Constructor

        public OtpService(
            IEmailService emailService,
            IUserRepository userRepository,
            IAdminRepository adminRepository,
            ICacheService cacheService)
        {
            _emailService = emailService;
            _userRepository = userRepository;
            _adminRepository = adminRepository;
            _cacheService = cacheService;
        }

        #endregion

        #region SendEmailOtpAsync

        public async Task SendEmailOtpAsync(string email, string purpose)
        {
            // 1. Validate email address input
            if (string.IsNullOrWhiteSpace(email))
                throw new ValidationException("Email address cannot be empty.");

            // 2. Perform account checking based on purpose (registration/reset/admin-reset)
            if (purpose == "registration")
            {
                var existing = await _userRepository.GetByEmailAsync(email);
                if (existing != null)
                {
                    if (string.Equals(existing.Status, "Deactivated", StringComparison.OrdinalIgnoreCase))
                        throw new UnauthorizedException("Your account has been deactivated. You cannot register with this email.");
                    throw new ConflictException("Email is already registered.");
                }
            }
            else if (purpose == "password-reset")
            {
                var existing = await _userRepository.GetByEmailAsync(email);
                if (existing == null)
                    throw new NotFoundException("No account registered with this email address.");
                if (string.Equals(existing.Status, "Deactivated", StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedException("Your account has been deactivated.");
            }
            else if (purpose == "admin-password-reset" || purpose == "finance-login")
            {
                var existing = await _adminRepository.GetByEmailAsync(email);
                if (existing == null)
                    throw new NotFoundException("No administrator account registered with this email address.");
            }
            else if (purpose == "close-account")
            {
                var existing = await _userRepository.GetByEmailAsync(email);
                if (existing == null)
                    throw new NotFoundException("No account registered with this email address.");
                if (string.Equals(existing.Status, "Deactivated", StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedException("Your account is already deactivated.");
            }

            // 3. Check OTP Rate Limiting
            string rateLimitKey = $"otp_rate_limit:{email}";
            var rateLimitInfo = await _cacheService.GetAsync<OtpRateLimitInfo>(rateLimitKey);
            
            if (rateLimitInfo != null)
            {
                if (rateLimitInfo.Attempts > 3)
                {
                    // Check if 10 minutes have passed since WindowStart
                    if ((DateTime.UtcNow - rateLimitInfo.WindowStart).TotalMinutes < 10)
                    {
                        throw new TooManyRequestsException("Maximum OTP requests exceeded. Please try again after 10 minutes.");
                    }
                    else
                    {
                        // Reset after cooldown expires
                        rateLimitInfo = new OtpRateLimitInfo { Attempts = 1, WindowStart = DateTime.UtcNow };
                    }
                }
                else
                {
                    rateLimitInfo.Attempts++;
                }
            }
            else
            {
                rateLimitInfo = new OtpRateLimitInfo { Attempts = 1, WindowStart = DateTime.UtcNow };
            }
            
            // Save updated rate limit info back to cache with a sliding 10 minute window (or absolute window)
            await _cacheService.SetAsync(rateLimitKey, rateLimitInfo, TimeSpan.FromMinutes(10));

            // 4. Generate a secure random 6-digit OTP code
            string otp = Random.Shared.Next(100000, 999999).ToString();
            
            // 5. Cache the OTP in our Redis cache with auto-clearing expiry time
            string cacheKey = $"otp:{purpose}:{email}";
            await _cacheService.SetAsync(cacheKey, otp, TimeSpan.FromMinutes(OtpExpiryMinutes));

            // 5. Define the subject lines depending on the authentication purpose
            string subject = purpose == "registration"
                 ? "Your Event Platform Email Verification OTP"
                 : (purpose == "finance-login" ? "Your Finance Dept Login Verification OTP" : 
                   (purpose == "close-account" ? "Your Event Platform Close Account Verification OTP" : 
                   (purpose == "email-change" ? "Verify Your New Email Address" : "Your Event Platform Password Reset OTP")));

            // 6. Load/Compile the formatted HTML email body using the generic EmailTemplateDto
            var purposeLabel = purpose == "registration"
                ? "verify your email address and complete your registration"
                : (purpose == "finance-login" ? "complete your Finance Dept login" : 
                  (purpose == "close-account" ? "confirm and authorize closing your account" : 
                  (purpose == "email-change" ? "verify your new email address" : "reset your account password")));

            string templateName = purpose == "email-change" ? "ProfileUpdateOtpTemplate.html" : "OtpEmailTemplate.html";

            var emailDto = new EmailTemplateDto
            {
                TemplateName = templateName,
                Placeholders = new Dictionary<string, string>
                {
                    { "purposeLabel", purposeLabel },
                    { "otp", otp },
                    { "year", DateTime.UtcNow.Year.ToString() }
                }
            };

            string htmlBody = await _emailService.BuildEmailHtmlAsync(emailDto);

            // 7. Send the compiled email message via the EmailService
            await _emailService.SendEmailAsync(email, subject, htmlBody);
        }

        #endregion

        #region VerifyOtpAsync

        public async Task<bool> VerifyOtpAsync(string email, string otp, string purpose)
        {
            // 1. Validate the incoming input arguments
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(purpose))
            {
                return false;
            }

            // 2. Fetch the cached OTP from Redis cache using the composite key
            string cacheKey = $"otp:{purpose}:{email}";
            string? cachedOtp = await _cacheService.GetAsync<string>(cacheKey);

            // 3. Verify matching value and remove key on success
            if (cachedOtp != null && cachedOtp == otp)
            {
                await _cacheService.RemoveAsync(cacheKey);

                // Store verification marker in Redis for 15 minutes
                string verifiedKey = $"otp-verified:{purpose}:{email}";
                await _cacheService.SetAsync(verifiedKey, "true", TimeSpan.FromMinutes(15));
                return true;
            }

            return false;
        }

        #endregion

        #region IsOtpVerificationExpiredAsync

        public async Task<bool> IsOtpVerificationExpiredAsync(string email, string purpose)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(purpose))
            {
                return true;
            }

            string verifiedKey = $"otp-verified:{purpose}:{email}";
            string? verifiedValue = await _cacheService.GetAsync<string>(verifiedKey);
            return string.IsNullOrEmpty(verifiedValue);
        }

        #endregion

        #region ConsumeOtpVerificationAsync

        public async Task ConsumeOtpVerificationAsync(string email, string purpose)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(purpose))
            {
                return;
            }

            string verifiedKey = $"otp-verified:{purpose}:{email}";
            await _cacheService.RemoveAsync(verifiedKey);
        }

        #endregion
    }

    public class OtpRateLimitInfo
    {
        public int Attempts { get; set; }
        public DateTime WindowStart { get; set; }
    }
}
