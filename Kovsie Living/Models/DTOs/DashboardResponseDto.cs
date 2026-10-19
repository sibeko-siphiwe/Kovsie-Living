namespace Kovsie_Living.Models.DTOs
{
    public class DashboardResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
        public bool IsApproved { get; set; }

        public decimal TotalPaid { get; set; }
        public decimal PendingPayments { get; set; }
        public int TransactionCount { get; set; }

        public List<RecentTransactionDto> RecentTransactions { get; set; }
    }

    public class RecentTransactionDto
    {
        public int PaymentId { get; set; }
        public string TariffName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Reference { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}