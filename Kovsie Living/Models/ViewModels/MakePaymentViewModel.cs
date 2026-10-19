using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models.ViewModels
{
    public class MakePaymentViewModel
    {
        [Required(ErrorMessage = "Please select a service")]
        [DisplayName("Service")]
        public int TariffId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [DisplayName("Amount (R)")]
        public string Amount { get; set; } // Changed from decimal to string

        public string TariffName { get; set; }
        public List<Tariff> AvailableTariffs { get; set; }

        // --- NEW CARD DETAILS ---
        [Required(ErrorMessage = "Cardholder name is required")]
        [DisplayName("Name on Card")]
        public string CardHolderName { get; set; }

        [Required(ErrorMessage = "Card number is required")]
        [DisplayName("Card Number")] 
        [RegularExpression(@"^(\d{4}\s?){3}\d{4}$", ErrorMessage = "Card number must be exactly 16 digits")]
        public string CardNumber { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        [DisplayName("Expiry Date (MM/YY)")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$", ErrorMessage = "Invalid format. Use MM/YY")]
        public string ExpiryDate { get; set; }

        [Required(ErrorMessage = "CVV is required")]
        [DisplayName("CVV")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits")]
        public string CVV { get; set; }
    }
}