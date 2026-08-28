namespace Kovsie_Living.Models.ViewModels
{
    public class CustomerDashboardViewModel
    {
        public ApplicationUser User { get; set; }
        public bool IsStudent { get; set; }
        public bool IsStaff { get; set; }
        public string UnitNumber { get; set; }
        public bool IsApproved { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal PendingPayments { get; set; }
        public int TransactionCount { get; set; }
        public IEnumerable<Payment> RecentTransactions { get; set; }
    }
}
