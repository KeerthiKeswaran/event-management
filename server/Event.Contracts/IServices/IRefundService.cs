using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Event.Contracts.IServices
{
    public interface IRefundService
    {
        Task<(decimal RefundAmount, string Remarks)> RefundAttendeeAsync(int bookingId, string refundType = "Dynamic", string refundMessage = "");
        Task<(decimal OrganizerRefundAmount, string OrganizerRemarks, List<(int BookingId, decimal RefundAmount, string Remarks)> AttendeeRefunds)> RefundOrganizerAsync(int eventId, string refundType = "Dynamic", string refundMessage = "");
        (decimal RefundAmount, string Remarks) CalculateAttendeeRefund(DateTime eventDateTime, decimal originalAmount, string refundType, decimal alreadyRefunded);
    }
}
