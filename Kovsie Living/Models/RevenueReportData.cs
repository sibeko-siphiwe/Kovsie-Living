namespace Kovsie_Living.Models
{
    public class RevenueReportData
    {
        public decimal TotalRevenue { get; set; }
        public decimal PendingRevenue { get; set; }
        public int TransactionCount { get; set; }
        public Dictionary<string, decimal> MonthlyBreakdown { get; set; } 
        public Dictionary<string, decimal> RevenueByTariff { get; set; } 
    }
}
