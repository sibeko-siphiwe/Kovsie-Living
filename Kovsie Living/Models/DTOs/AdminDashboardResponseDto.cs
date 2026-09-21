namespace Kovsie_Living.Models.DTOs
{
    public class AdminDashboardResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }

        public int TotalCustomers { get; set; }
        public int TotalOfficers { get; set; }
        public int PendingPaymentsCount { get; set; }
        public decimal TotalRevenue { get; set; }

        public List<AdminRecentActivityDto> RecentActivities { get; set; }
    }

    public class AdminRecentActivityDto
    {
        public string ActivityType { get; set; } // "User", "Payment"
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Status { get; set; }
        public DateTime ActivityDate { get; set; }
    }
}