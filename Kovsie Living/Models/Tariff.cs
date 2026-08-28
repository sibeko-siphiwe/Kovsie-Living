using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models
{
    public class Tariff
    {
        public int TariffId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public string Description => $"This is a fee for {Name}";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
