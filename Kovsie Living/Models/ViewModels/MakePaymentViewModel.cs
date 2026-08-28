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
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        public string TariffName { get; set; }
        public List<Tariff> AvailableTariffs { get; set; }
    }
}
