using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models.ViewModels
{
    public class EditTariffViewModel
    {
        public int TariffId { get; set; }

        [Required(ErrorMessage = "Tariff Name is required")]
        [DisplayName("Tariff Name")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [DisplayName("Amount (R)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }
    }
}
