namespace Kovsie_Living.Models.DTOs
{
    public class PaymentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int PaymentId { get; set; }
        public string TariffName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Reference { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}