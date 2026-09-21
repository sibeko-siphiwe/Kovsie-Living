namespace Kovsie_Living.Models.DTOs
{
    public class OfficerDashboardResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }

        public int TotalCustomers { get; set; }
        public int PendingPaymentsCount { get; set; }
        public decimal TotalRevenue { get; set; }

        public List<OfficerRecentPaymentDto> RecentPayments { get; set; }
    }

    public class OfficerRecentPaymentDto
    {
        public int PaymentId { get; set; }
        public string CustomerName { get; set; }
        public string TariffName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Reference { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}