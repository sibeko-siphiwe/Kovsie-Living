namespace Kovsie_Living.Models.DTOs
{
    public class PaymentHistoryResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPending { get; set; }
        public int TransactionCount { get; set; }
        public List<PaymentHistoryItemDto> Payments { get; set; }
    }
}