namespace Kovsie_Living.Models.DTOs
{
    public class PendingPaymentsResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalPending { get; set; }
        public List<PendingPaymentItemDto> Payments { get; set; }
    }

    public class PendingPaymentItemDto
    {
        public int PaymentId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerNumber { get; set; }
        public string TariffName { get; set; }
        public decimal Amount { get; set; }
        public string Reference { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}