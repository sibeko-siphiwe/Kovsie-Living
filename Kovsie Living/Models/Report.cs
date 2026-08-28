using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kovsie_Living.Models
{
    public class Report
    {
        [Key]
        public int ReportId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(50)]
        public string ReportType { get; set; } // "Payments", "Customers", "Revenue"

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string GeneratedByUserId { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public string FilePath { get; set; } // Path to saved PDF

        [Required]
        public string Status { get; set; } = "Generated"; // Generated, Viewed, Archived

        // Store report data as JSON - make nullable
        public string? ReportDataJson { get; set; }

        // Navigation Properties
        [ForeignKey("GeneratedByUserId")]
        public virtual ApplicationUser GeneratedBy { get; set; }
    }
}