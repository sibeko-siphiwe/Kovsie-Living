namespace Kovsie_Living.Models.DTOs
{
    public class AdminTariffsResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalCount { get; set; }
        public List<AdminTariffItemDto> Tariffs { get; set; }
    }

    public class AdminTariffItemDto
    {
        public int TariffId { get; set; }
        public string Name { get; set; }
        public decimal Amount { get; set; }
        public bool IsActive { get; set; }
    }
}