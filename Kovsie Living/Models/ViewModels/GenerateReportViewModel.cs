using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models.ViewModels
{
    public class GenerateReportViewModel
    {
        [Required(ErrorMessage = "Report type is required")]
        [DisplayName("Report Type")]
        public string ReportType { get; set; } // "Payments", "Customers", "Revenue"

        [Required(ErrorMessage = "Start date is required")]
        [DisplayName("Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddMonths(-1);

        [Required(ErrorMessage = "End date is required")]
        [DisplayName("End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.UtcNow;

        [DisplayName("Description")]
        [StringLength(500)]
        public string Description { get; set; }
    }
}
