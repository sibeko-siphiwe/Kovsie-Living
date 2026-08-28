namespace Kovsie_Living.Models.ViewModels
{
    public class PaymentHistoryViewModel
    {
        public List<Payment> Payments { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPending { get; set; }
        public int PaymentCount { get; set; }
    }
}
