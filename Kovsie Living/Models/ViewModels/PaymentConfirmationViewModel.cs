namespace Kovsie_Living.Models.ViewModels
{
    public class PaymentConfirmationViewModel
    {
        public int PaymentId { get; set; }
        public string TariffName { get; set; }
        public decimal Amount { get; set; }
        public string Reference { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; }
    }
}
