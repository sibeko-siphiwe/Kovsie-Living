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

        [Required(ErrorMessage = "Cardholder name is required")]
        public string CardHolderName { get; set; }

        [Required(ErrorMessage = "Card number is required")]
        public string CardNumber { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        public string ExpiryDate { get; set; }

        [Required(ErrorMessage = "CVV is required")]
        public string CVV { get; set; }
    }
}