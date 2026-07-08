using System;

namespace Event.Models.DTOs
{
    public class FinanceDashboardStatsResponse
    {
        public int TotalTransactions { get; set; }
        public int PendingApprovals { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalIntake { get; set; }
    }
}
