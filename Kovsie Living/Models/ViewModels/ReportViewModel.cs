namespace Kovsie_Living.Models.ViewModels
{
    public class ReportViewModel
    {
        public int ReportId { get; set; }
        public string Title { get; set; }
        public string ReportType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string FilePath { get; set; }
    }
}
