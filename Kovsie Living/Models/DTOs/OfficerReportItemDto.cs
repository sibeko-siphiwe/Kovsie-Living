namespace Kovsie_Living.Models.DTOs
{
    public class OfficerReportItemDto
    {
        public int ReportId { get; set; }
        public string Title { get; set; }
        public string ReportType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string Status { get; set; }
        public string FilePath { get; set; }
    }

    public class OfficerReportsResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalReports { get; set; }
        public List<OfficerReportItemDto> Reports { get; set; }
    }
}