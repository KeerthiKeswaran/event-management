using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Event.Models;
using Event.Models.DTOs;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Business.Exceptions;

namespace Event.Business.Services
{
    public class UserService : IUserService
    {
        #region Fields

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;
        private readonly IEventRepository _eventRepository;
        private readonly OtpService _otpService;
        private readonly IEmailService _emailService;
        private readonly IAdminRepository _adminRepository;

        #endregion

        #region Constructor

        public UserService(
            IHttpContextAccessor httpContextAccessor, 
            IUserRepository userRepository,
            IEventRepository eventRepository,
            OtpService otpService,
            IEmailService emailService,
            IAdminRepository adminRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
            _eventRepository = eventRepository;
            _otpService = otpService;
            _emailService = emailService;
            _adminRepository = adminRepository;
        }

        #endregion

        #region GetCurrentUserId

        public int GetCurrentUserId()
        {
            // 1. Retrieve the HTTP context associated with the current request
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new InvalidOperationException("HTTP Context is not available outside a request.");
            }

            // 2. Extract user ID value from claims names token identifier
            var userIdStr = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                return userId;
            }

            // 3. Fallback: Parse user ID value from request header keys
            if (httpContext.Request.Headers.TryGetValue("X-User-Id", out var headerId) && int.TryParse(headerId, out int parsedId))
            {
                return parsedId;
            }

            // 4. Throw unauthorized exception if identification cannot be resolved
            throw new UnauthorizedException("User identification not found in claims or headers.");
        }

        #endregion

        #region SelectInterestedRegionsAsync

        public async Task<bool> SelectInterestedRegionsAsync(int userId, string regionId)
        {
            // 1. Retrieve the target user and validate existence
            var userExists = await _userRepository.ExistsAsync(userId);
            if (!userExists)
                throw new NotFoundException($"User with ID {userId} not found.");

            // 2. Update interested regions using the repository
            var regionIds = string.IsNullOrEmpty(regionId) ? new List<string>() : new List<string> { regionId };
            await _userRepository.UpdateInterestedRegionsAsync(userId, regionIds);
            return true;
        }

        #endregion

        #region UpdateUserProfileAsync

        public async Task<bool> UpdateUserProfileAsync(int userId, UpdateProfileRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException($"User with ID {userId} not found.");

            if (!string.IsNullOrWhiteSpace(request.Email) && !user.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(request.Otp))
                    throw new ValidationException("OTP is required to change email address.");

                var emailExists = await _userRepository.GetByEmailAsync(request.Email);
                if (emailExists != null && emailExists.User_Id != userId)
                    throw new ConflictException("Email is already registered.");

                var adminEmailExists = await _adminRepository.GetByEmailAsync(request.Email);
                if (adminEmailExists != null)
                    throw new ConflictException("Email is already registered.");

                if (!await _otpService.VerifyOtpAsync(request.Email, request.Otp, "email-change"))
                    throw new UnauthorizedException("Invalid or expired OTP.");
                
                user.Email = request.Email;
            }

            user.Name = request.Name;
            user.Mobile_Number = request.MobileNumber;
            await _userRepository.UpdateAsync(user);

            return true;
        }

        #endregion

        #region GetUserProfileAsync

        public async Task<UserProfileResponse?> GetUserProfileAsync(int userId)
        {
            // 1. Query user record with eager interest regions loading from repository
            var user = await _userRepository.GetUserProfileAsync(userId);

            // 2. Validate that user exists
            if (user == null)
                throw new NotFoundException($"User with ID {userId} not found.");

            var regionId = string.Empty;
            if (user.InterestedRegions != null)
            {
                foreach (var ir in user.InterestedRegions)
                {
                    if (ir.Region_Id != null)
                    {
                        regionId = ir.Region_Id;
                        break;
                    }
                }
            }

            return new UserProfileResponse
            {
                User_Id = user.User_Id,
                Name = user.Name,
                Email = user.Email,
                Mobile_Number = user.Mobile_Number,
                Status = user.Status,
                RegionId = regionId
            };
        }

        #endregion

        #region GetMyEventsAsync

        public async Task<IEnumerable<MyEventOverviewResponse>> GetMyEventsAsync(int organizerId)
        {
            var userExists = await _userRepository.ExistsAsync(organizerId);
            if (!userExists)
                throw new NotFoundException($"User with ID {organizerId} not found.");

            var events = await _eventRepository.GetEventsByOrganizerAsync(organizerId);
            var response = new List<MyEventOverviewResponse>();
            foreach (var ev in events)
            {
                response.Add(new MyEventOverviewResponse
                {
                    Event_Id = ev.Event_Id,
                    Title = ev.Title,
                    Event_Type = ev.Event_Type,
                    Date_Time = ev.Date_Time,
                    Duration_Hours = ev.Duration_Hours,
                    Status = ev.Status,
                    Venue_Name = ev.Venue?.Name,
                    Tickets_Sold = ev.TicketTiers?.Sum(t => t.Tickets_Sold) ?? 0,
                    Net_Earnings = ev.TicketTiers?.Sum(t => t.Tickets_Sold * t.Price) ?? 0m,
                    Category = ev.Category,
                    Description_Url = ev.Description_Url,
                    Title_Update_Count = ev.Title_Update_Count,
                    Virtual_Url = ev.Virtual_Url,
                    Virtual_Password_Hash = ev.Virtual_Password_Hash
                });
            }
            return response;
        }

        #endregion

        #region GetMyEventDetailsAsync

        public async Task<MyEventDetailsResponse?> GetMyEventDetailsAsync(int organizerId, int eventId)
        {
            var ev = await _eventRepository.GetEventDetailsAsync(eventId);
            if (ev == null)
                return null;

            if (ev.Organizer_Id != organizerId)
                throw new UnauthorizedAccessException("You are not authorized to view this event's details.");

            var ticketTiers = new List<TicketTierDetailsDto>();
            if (ev.TicketTiers != null)
            {
                foreach (var tier in ev.TicketTiers)
                {
                    ticketTiers.Add(new TicketTierDetailsDto
                    {
                        Tier_Name = tier.Tier_Name,
                        Price = tier.Price,
                        Tickets_Sold = tier.Tickets_Sold,
                        Capacity = ev.Venue != null && ev.Venue.SeatCapacities != null 
                                    ? ev.Venue.SeatCapacities.FirstOrDefault(sc => sc.Tier_Name == tier.Tier_Name)?.Total_Seats ?? 0
                                    : 0
                    });
                }
            }

            return new MyEventDetailsResponse
            {
                Event_Id = ev.Event_Id,
                Organizer_Id = ev.Organizer_Id,
                Event_Type = ev.Event_Type,
                Title = ev.Title,
                Description_Url = ev.Description_Url,
                Image_Url = ev.Image_Url,
                Date_Time = ev.Date_Time,
                Duration_Hours = ev.Duration_Hours,
                Status = ev.Status,
                Requires_Staff = ev.Requires_Staff,
                Venue_Id = ev.Venue_Id,
                Venue_Name = ev.Venue?.Name,
                Virtual_Url = ev.Virtual_Url,
                Virtual_Password_Hash = ev.Virtual_Password_Hash,
                Category = ev.Category,
                Title_Update_Count = ev.Title_Update_Count,
                TicketTiers = ticketTiers
            };
        }

        #endregion

        #region CloseAccountAsync

        public async Task<bool> CloseAccountAsync(int userId, CloseAccountRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException($"User with ID {userId} not found.");

            if (user.Status == "Deactivated")
                throw new ValidationException("Account is already closed.");

            if (!await _otpService.VerifyOtpAsync(user.Email, request.Otp, "close-account"))
                throw new UnauthorizedException("Invalid or expired OTP.");

            if (!string.Equals(user.Name, request.ConfirmName, StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Confirmation name does not match your account name.");

            user.Status = "Deactivated";
            await _userRepository.UpdateAsync(user);

            try
            {
                var emailDto = new EmailTemplateDto
                {
                    TemplateName = "CloseAccountTemplate.html",
                    Placeholders = new Dictionary<string, string>
                    {
                        { "userName", user.Name },
                        { "year", DateTime.UtcNow.Year.ToString() }
                    }
                };
                string htmlBody = await _emailService.BuildEmailHtmlAsync(emailDto);
                await _emailService.SendEmailAsync(user.Email, "Account Closed - Event Platform", htmlBody);
            }
            catch (Exception)
            {
                // Do not throw on email dispatch failure to keep deactivation state change robust
            }

            return true;
        }

        #endregion
    }
}
