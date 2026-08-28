using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kovsie_Living.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int TariffId { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string Reference { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Paid, Pending, Failed, Cancelled

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Online";

        [StringLength(255)]
        public string Notes { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("TariffId")]
        public virtual Tariff Tariff { get; set; }
    }
}
