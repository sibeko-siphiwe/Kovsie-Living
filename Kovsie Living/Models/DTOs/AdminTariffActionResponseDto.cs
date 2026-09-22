namespace Kovsie_Living.Models.DTOs
{
    public class AdminTariffActionResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TariffId { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public bool IsActive { get; set; }
        public List<string> Errors { get; set; }
    }
}