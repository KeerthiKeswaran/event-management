using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Event.Models;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Business.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Event.Models.DTOs;
using Event.Business.Helpers;

namespace Event.Business.Services
{
    public class RefundService : IRefundService
    {
        #region Fields

        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IBookingPaymentRepository _bookingPaymentRepository;
        private readonly IPaymentService _paymentService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEmailService _emailService;
        private readonly INotificationRepository _notificationRepository;

        #endregion

        #region Constructor

        public RefundService(
            IBookingRepository bookingRepository,
            IEventRepository eventRepository,
            ITransactionRepository transactionRepository,
            IBookingPaymentRepository bookingPaymentRepository,
            IPaymentService paymentService,
            IServiceProvider serviceProvider,
            IEmailService emailService,
            INotificationRepository notificationRepository)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
            _transactionRepository = transactionRepository;
            _bookingPaymentRepository = bookingPaymentRepository;
            _paymentService = paymentService;
            _serviceProvider = serviceProvider;
            _emailService = emailService;
            _notificationRepository = notificationRepository;
        }

        #endregion

        #region RefundAttendeeAsync

        public async Task<(decimal RefundAmount, string Remarks)> RefundAttendeeAsync(int bookingId, string refundType = "Dynamic", string refundMessage = "")
        {
            var booking = await _bookingRepository.GetBookingDetailsAsync(bookingId);
            if (booking == null)
                throw new NotFoundException($"Booking with ID {bookingId} not found.");

            if (booking.Booking_Status != "Cancelled")
            {
                booking.Booking_Status = "Cancelled";
                await _bookingRepository.UpdateAsync(booking);

                // Send cancellation email right when status is set to Cancelled
                bool isEventCancelled = booking.Event?.Status == "Cancelled";
                string cancellationReason = isEventCancelled
                    ? (string.IsNullOrWhiteSpace(refundMessage) ? $"This booking has been cancelled because the event \"{booking.Event?.Title}\" was cancelled." : refundMessage)
                    : "Your booking has been cancelled as requested.";

                var cancellationEmailDto = new EmailTemplateDto
                {
                    TemplateName = "BookingCancellationTemplate.html",
                    Placeholders = new Dictionary<string, string>
                    {
                        { "bookingId", booking.Booking_Id.ToString() },
                        { "eventName", booking.Event?.Title ?? "" },
                        { "cancellationReason", cancellationReason },
                        { "refundStatusMessage", "A refund will be processed shortly if applicable." },
                        { "year", DateTime.UtcNow.Year.ToString() }
                    }
                };
                try
                {
                    string htmlCancelBody = await _emailService.BuildEmailHtmlAsync(cancellationEmailDto);
                    await NotificationHelper.SendAndSaveNotificationAsync(
                        _notificationRepository,
                        _emailService,
                        booking.Attendee?.Email ?? string.Empty,
                        $"Booking Cancelled: {booking.Event?.Title}",
                        htmlCancelBody
                    );
                }
                catch (Exception) { }
            }

            var originalPayment = await _bookingPaymentRepository.GetSuccessPaymentByBookingIdAsync(bookingId);
            var originalTx = await _transactionRepository.GetSuccessBookingTransactionAsync(bookingId);

            decimal refundAmountVal = 0m;
            string remarksVal = "No payment transaction found to refund.";
            bool isRefunded = false;

            if (originalPayment != null && originalTx != null)
            {
                var refundDetails = CalculateAttendeeRefund(booking.Event!.Date_Time, originalPayment.Amount, refundType, originalTx.Refunded_Amount);
                refundAmountVal = refundDetails.RefundAmount;
                remarksVal = refundDetails.Remarks;
                isRefunded = refundAmountVal > 0;
            }

            if (originalPayment != null && originalTx != null)
            {
                if (isRefunded)
                {
                    var stripeRefund = await _paymentService.CreateRefundAsync(originalTx.Transaction_Reference ?? string.Empty, refundAmountVal);
                    if (stripeRefund.Success)
                    {
                        originalPayment.Payment_Status = "Refunded";
                        await _bookingPaymentRepository.UpdateAsync(originalPayment);

                        originalTx.Refunded_Amount = originalTx.Refunded_Amount + refundAmountVal;
                        await _transactionRepository.UpdateAsync(originalTx);
                    }
                    else
                    {
                        isRefunded = false;
                        refundAmountVal = 0m;
                        remarksVal = $"Stripe refund failed: {stripeRefund.ErrorMessage}";
                    }
                }

                var refundTx = new Transaction
                {
                    Sender_Id = "Platform_Escrow",
                    Receiver_Id = $"Attendee_User_{booking.Attendee_Id}",
                    Transaction_Type = "BookingRefund",
                    Related_Id = bookingId,
                    Amount = refundAmountVal,
                    Currency = "INR",
                    Status = (isRefunded) ? "Success" : "Failed",
                    Created_At = DateTime.UtcNow,
                    Remarks = remarksVal
                };
                await _transactionRepository.AddAsync(refundTx);

                // Send refund email only after transaction has been persisted to DB
                if (isRefunded)
                {
                    var refundEmailDto = new EmailTemplateDto
                    {
                        TemplateName = "RefundTemplate.html",
                        Placeholders = new Dictionary<string, string>
                        {
                            { "referenceName", booking.Event?.Title ?? "" },
                            { "refundAmount", $"INR {refundAmountVal:N2}" },
                            { "refundMessage", string.IsNullOrWhiteSpace(refundMessage) ? "" : refundMessage },
                            { "year", DateTime.UtcNow.Year.ToString() }
                        }
                    };
                    try
                    {
                        string htmlRefundBody = await _emailService.BuildEmailHtmlAsync(refundEmailDto);
                        await NotificationHelper.SendAndSaveNotificationAsync(
                            _notificationRepository,
                            _emailService,
                            booking.Attendee?.Email ?? string.Empty,
                            $"Refund Processed: {booking.Event?.Title}",
                            htmlRefundBody
                        );
                    }
                    catch (Exception) { }
                }
            }

            return (refundAmountVal, remarksVal);
        }

        #endregion

        #region RefundOrganizerAsync

        public async Task<(decimal OrganizerRefundAmount, string OrganizerRemarks, List<(int BookingId, decimal RefundAmount, string Remarks)> AttendeeRefunds)> RefundOrganizerAsync(int eventId, string refundType = "Dynamic", string refundMessage = "")
        {
            var ev = await _eventRepository.GetEventDetailsAsync(eventId);
            if (ev == null)
                throw new NotFoundException($"Event with ID {eventId} not found.");

            if (ev.Status != "Cancelled")
            {
                ev.Status = "Cancelled";
                await _eventRepository.UpdateAsync(ev);

                var venueRepository = _serviceProvider.GetRequiredService<IVenueRepository>();
                var staffRepository = _serviceProvider.GetRequiredService<IStaffRepository>();

                // Release staff
                if (ev.StaffAllocations != null)
                {
                    foreach (var allocation in ev.StaffAllocations)
                    {
                        var staff = await staffRepository.GetByIdAsync(allocation.Employee_ID);
                        if (staff != null)
                        {
                            staff.IsAllocated = false;
                            await staffRepository.UpdateAsync(staff);
                        }
                    }
                }

                // Release venue
                if ((ev.Event_Type == "Physical" || ev.Event_Type == "Hybrid") && ev.Venue_Id.HasValue)
                {
                    var venue = await venueRepository.GetByIdAsync(ev.Venue_Id.Value);
                    if (venue != null)
                    {
                        venue.Is_Available = true;
                        await venueRepository.UpdateAsync(venue);
                    }
                }

                // Send cancellation email to organizer right when event is marked Cancelled
                var eventCancelEmailDto = new EmailTemplateDto
                {
                    TemplateName = "EventCancellationTemplate.html",
                    Placeholders = new Dictionary<string, string>
                    {
                        { "eventName", ev.Title },
                        { "refundStatusMessage", "A refund will be processed shortly if applicable." },
                        { "year", DateTime.UtcNow.Year.ToString() }
                    }
                };
                try
                {
                    string htmlCancelBody = await _emailService.BuildEmailHtmlAsync(eventCancelEmailDto);
                    await NotificationHelper.SendAndSaveNotificationAsync(
                        _notificationRepository,
                        _emailService,
                        ev.Organizer?.Email ?? string.Empty,
                        $"Event Cancelled: {ev.Title}",
                        htmlCancelBody
                    );
                }
                catch (Exception) { }
            }

            var organizerTx = await _transactionRepository.GetTransactionsByUserIdAsync(ev.Organizer_Id);
            var upfrontTx = organizerTx.FirstOrDefault(t => t.Related_Id == eventId && 
                                                             t.Transaction_Type == "OrganizerUpfrontPayment" && 
                                                             t.Status == "Success");

            decimal organizerRefundAmount = 0m;
            string organizerRemarks = "No upfront payment transaction found to refund.";

            if (upfrontTx != null)
            {
                var refundDetails = CalculateOrganizerRefund(ev.Date_Time, upfrontTx.Amount, refundType, upfrontTx.Refunded_Amount);
                organizerRefundAmount = refundDetails.RefundAmount;
                organizerRemarks = refundDetails.Remarks;
                
                if (organizerRefundAmount > 0)
                {
                    var stripeRefund = await _paymentService.CreateRefundAsync(upfrontTx.Transaction_Reference ?? string.Empty, organizerRefundAmount);
                    if (stripeRefund.Success)
                    {
                        var refundTx = new Transaction
                        {
                            Sender_Id = "Platform_Escrow",
                            Receiver_Id = $"Organizer_User_{ev.Organizer_Id}",
                            Transaction_Type = "BookingRefund", // Reusing BookingRefund type or log as Refund
                            Related_Id = eventId,
                            Amount = organizerRefundAmount,
                            Currency = "INR",
                            Status = "Success",
                            Created_At = DateTime.UtcNow,
                            Remarks = organizerRemarks,
                            Transaction_Reference = stripeRefund.RefundReference
                        };
                        await _transactionRepository.AddAsync(refundTx);

                        upfrontTx.Refunded_Amount = upfrontTx.Refunded_Amount + organizerRefundAmount;
                        await _transactionRepository.UpdateAsync(upfrontTx);
                    }
                    else
                    {
                        organizerRefundAmount = 0m;
                        organizerRemarks = $"Stripe refund failed: {stripeRefund.ErrorMessage}";
                    }

                    // Send refund email only after both transactions are fully persisted to DB
                    if (stripeRefund.Success)
                    {
                        try
                        {
                            var refundEmailDto = new EmailTemplateDto
                            {
                                TemplateName = "RefundTemplate.html",
                                Placeholders = new Dictionary<string, string>
                                {
                                    { "referenceName", ev.Title },
                                    { "refundAmount", $"INR {organizerRefundAmount:N2}" },
                                    { "refundMessage", string.IsNullOrWhiteSpace(refundMessage) ? "A refund was processed for your upfront activation fee." : refundMessage },
                                    { "year", DateTime.UtcNow.Year.ToString() }
                                }
                            };
                            string htmlBody = await _emailService.BuildEmailHtmlAsync(refundEmailDto);
                            await NotificationHelper.SendAndSaveNotificationAsync(
                                _notificationRepository,
                                _emailService,
                                ev.Organizer?.Email ?? string.Empty,
                                $"Refund Processed: {ev.Title}",
                                htmlBody
                            );
                        }
                        catch (Exception) { }
                    }
                }
            }

            // Attendee bookings refunds
            var attendeeRefunds = new List<(int BookingId, decimal RefundAmount, string Remarks)>();
            var bookings = await _bookingRepository.GetBookingsByEventIdAsync(eventId);

            foreach (var booking in bookings)
            {
                if (booking.Booking_Status == "Confirmed")
                {
                    var attendeeTx = await _transactionRepository.GetSuccessBookingTransactionAsync(booking.Booking_Id);
                    if (attendeeTx != null)
                    {
                        decimal alreadyRefunded = attendeeTx.Refunded_Amount;

                        // 3. Fully refunded already, skip that booking
                        if (alreadyRefunded >= attendeeTx.Amount)
                        {
                            continue;
                        }

                        // 1. already refunded partially then refund with remaining
                        // 2. Not refunded, then proceed with full refund
                        string bookingRefundType = string.Equals(refundType, "NoRefund", StringComparison.OrdinalIgnoreCase)
                            ? "NoRefund"
                            : (alreadyRefunded > 0 ? "Remaining" : "Full");
                        // Pass empty refundMessage — attendees don't get a custom message when organizer cancels;
                        // the finance team handles direct communication with them.
                        var (refundAmt, remarks) = await RefundAttendeeAsync(booking.Booking_Id, bookingRefundType, refundMessage: "");
                        
                        attendeeRefunds.Add((booking.Booking_Id, refundAmt, remarks));
                    }
                }
            }

            return (organizerRefundAmount, organizerRemarks, attendeeRefunds);
        }

        #endregion

        #region CalculateAttendeeRefund

        public (decimal RefundAmount, string Remarks) CalculateAttendeeRefund(DateTime eventDateTime, decimal originalAmount, string refundType, decimal alreadyRefunded)
        {
            if (string.Equals(refundType, "Full", StringComparison.OrdinalIgnoreCase))
            {
                return (originalAmount, "Full refund issued.");
            }
            else if (string.Equals(refundType, "Remaining", StringComparison.OrdinalIgnoreCase))
            {
                decimal remaining = Math.Max(0m, originalAmount - alreadyRefunded);
                return (remaining, "Remaining amount refunded.");
            }
            else if (string.Equals(refundType, "NoRefund", StringComparison.OrdinalIgnoreCase))
            {
                return (0m, "No refund processed as per finance policy decision.");
            }
            else // "Dynamic"
            {
                var timeDiff = eventDateTime - DateTime.UtcNow;
                if (timeDiff.TotalHours > 48)
                {
                    return (originalAmount * 0.90m, "Booking cancelled > 48 hours prior to event start. 90% refund issued.");
                }
                else if (timeDiff.TotalHours >= 12)
                {
                    return (originalAmount * 0.50m, "Booking cancelled between 12 and 48 hours prior to event start. 50% refund issued.");
                }
                else
                {
                    return (0m, "Booking cancelled < 12 hours prior to event start. No refund processed.");
                }
            }
        }

        #endregion

        #region CalculateOrganizerRefund

        private (decimal RefundAmount, string Remarks) CalculateOrganizerRefund(DateTime eventDateTime, decimal originalAmount, string refundType, decimal alreadyRefunded)
        {
            if (string.Equals(refundType, "Full", StringComparison.OrdinalIgnoreCase))
            {
                return (originalAmount, "Full refund processed.");
            }
            else if (string.Equals(refundType, "Remaining", StringComparison.OrdinalIgnoreCase))
            {
                decimal remaining = Math.Max(0m, originalAmount - alreadyRefunded);
                return (remaining, "Remaining upfront activation refund processed.");
            }
            else if (string.Equals(refundType, "NoRefund", StringComparison.OrdinalIgnoreCase))
            {
                return (0m, "No upfront activation refund processed as per finance policy decision.");
            }
            else // "Dynamic"
            {
                var timeUntilEvent = eventDateTime - DateTime.UtcNow;
                if (timeUntilEvent.TotalHours > 48)
                {
                    return (originalAmount * 0.90m, "Event cancelled > 48 hours prior to start. 90% upfront activation refund processed.");
                }
                else if (timeUntilEvent.TotalHours > 24)
                {
                    return (originalAmount * 0.50m, "Event cancelled between 24 and 48 hours prior to start. 50% upfront activation refund processed.");
                }
                else
                {
                    return (0m, "Event cancelled < 24 hours prior to start. Upfront activation fee is non-refundable.");
                }
            }
        }

        #endregion
    }
}
