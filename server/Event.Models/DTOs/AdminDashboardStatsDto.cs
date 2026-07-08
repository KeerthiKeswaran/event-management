using System;
using System.Collections.Generic;

namespace Event.Models.DTOs
{
    public class AdminDashboardStatsDto
    {
        public StatsSummaryDto Summary { get; set; } = null!;
        public StaffMetricsDto StaffMetrics { get; set; } = null!;
    }

    public class StatsSummaryDto
    {
        public int TotalUsers { get; set; }
        public int TotalLiveEvents { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal PlatformCommission { get; set; }
    }

    public class StaffMetricsDto
    {
        public int TotalStaff { get; set; }
        public int AllocatedStaffCount { get; set; }
        public double AllocationPercentage { get; set; }
    }

    public class EventDetailEto
    {
        public int EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string OrganizerName { get; set; } = string.Empty;
        public string OrganizerEmail { get; set; } = string.Empty;
        public int AllocatedStaffCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public double AllocatedStaffPercentage { get; set; }
        public string DescriptionUrl { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
