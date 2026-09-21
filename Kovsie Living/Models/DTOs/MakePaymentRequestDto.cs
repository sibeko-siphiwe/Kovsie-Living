using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models.DTOs
{
    public class MakePaymentRequestDto
    {
        [Required]
        public int TariffId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
    }
}