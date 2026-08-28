namespace Kovsie_Living.Models
{
    public class PaymentReportData
    {
        public int TotalPayments { get; set; }
        public int ApprovedPayments { get; set; }
        public int PendingPayments { get; set; }
        public int RejectedPayments { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
        public Dictionary<string, decimal> AmountByTariff { get; set; } 
        public List<PaymentReportItem> Payments { get; set; }
    }

    public class PaymentReportItem
    {
        public string CustomerName { get; set; } 
        public string TariffName { get; set; } 
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; } 
        public string Reference { get; set; } 
        public string ProcessedBy { get; set; } 
    }
}
