using Kovsie_Living.Models;

namespace Kovsie_Living.Data.Repositories
{
    public interface IReportRepository
    {
        Task<Report> GenerateReportAsync(string officerUserId, string reportType, DateTime startDate, DateTime endDate, string description);
        Task<CustomerReportData> GetCustomerReportDataAsync(string officerUserId, DateTime startDate, DateTime endDate);
        Task<PaymentReportData> GetPaymentReportDataAsync(string officerUserId, DateTime startDate, DateTime endDate);
        Task<RevenueReportData> GetRevenueReportDataAsync(DateTime startDate, DateTime endDate);
        Task<string> GeneratePdfAsync(Report report);
    }
}
