using Kovsie_Living.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Kovsie_Living.Data.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly PdfService _pdfService;

        public ReportRepository(AppDbContext context, IWebHostEnvironment webHostEnvironment, PdfService pdfService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _pdfService = pdfService;
        }

        public async Task<Report> GenerateReportAsync(string officerUserId, string reportType, DateTime startDate, DateTime endDate, string description)
        {
            object reportData = null;

            switch (reportType)
            {
                case "Customers":
                    reportData = await GetCustomerReportDataAsync(officerUserId, startDate, endDate);
                    break;
                case "Payments":
                    reportData = await GetPaymentReportDataAsync(officerUserId, startDate, endDate);
                    break;
                case "Revenue":
                    reportData = await GetRevenueReportDataAsync(startDate, endDate);
                    break;
            }

            var report = new Report
            {
                Title = $"{reportType} Report - {startDate:dd MMM yyyy} to {endDate:dd MMM yyyy}",
                ReportType = reportType,
                StartDate = startDate,
                EndDate = endDate,
                GeneratedDate = DateTime.UtcNow,
                GeneratedByUserId = officerUserId,
                Description = description,
                Status = "Generated",
                ReportDataJson = JsonSerializer.Serialize(reportData),
                FilePath = "" // Initialize empty
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            // Generate PDF separately
            try
            {
                report.FilePath = await GeneratePdfAsync(report);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // If PDF generation fails, still save report but without PDF
                Console.WriteLine($"PDF generation failed: {ex.Message}");
                report.FilePath = "";
                await _context.SaveChangesAsync();
            }

            return report;
        }

        public async Task<CustomerReportData> GetCustomerReportDataAsync(string officerUserId, DateTime startDate, DateTime endDate)
        {
            // Get all customers without date filtering
            var students = await _context.Students
                .Include(s => s.User)
                .ToListAsync();

            var staff = await _context.Staff
                .Include(s => s.User)
                .ToListAsync();

            var items = new List<CustomerReportItem>();

            foreach (var student in students)
            {
                if (student.User != null)
                {
                    var payments = await _context.Payments
                        .Where(p => p.UserId == student.UserId)
                        .ToListAsync();

                    items.Add(new CustomerReportItem
                    {
                        FullName = student.User.FullName,
                        UserType = "Student",
                        UserNumber = student.StudentNumber,
                        UnitNumber = student.UnitNumber,
                        Status = student.IsApproved ? "Approved" : "Pending",
                        RegistrationDate = student.User.CreatedAt,
                        TotalPaid = payments.Where(p => p.Status == "Paid").Sum(p => p.Amount),
                        PaymentCount = payments.Count
                    });
                }
            }

            foreach (var staffMember in staff)
            {
                if (staffMember.User != null)
                {
                    var payments = await _context.Payments
                        .Where(p => p.UserId == staffMember.UserId)
                        .ToListAsync();

                    items.Add(new CustomerReportItem
                    {
                        FullName = staffMember.User.FullName,
                        UserType = "Staff",
                        UserNumber = staffMember.StaffNumber,
                        UnitNumber = staffMember.UnitNumber,
                        Status = staffMember.IsApproved ? "Approved" : "Pending",
                        RegistrationDate = staffMember.User.CreatedAt,
                        TotalPaid = payments.Where(p => p.Status == "Paid").Sum(p => p.Amount),
                        PaymentCount = payments.Count
                    });
                }
            }

            // REMOVED date filtering - show all customers
            // If you want date filtering, use a wider range or make it optional

            return new CustomerReportData
            {
                TotalCustomers = items.Count,
                NewCustomers = items.Count,
                ApprovedCustomers = items.Count(i => i.Status == "Approved"),
                PendingCustomers = items.Count(i => i.Status == "Pending"),
                TotalCollected = items.Sum(i => i.TotalPaid),
                Customers = items
            };
        }

        public async Task<PaymentReportData> GetPaymentReportDataAsync(string officerUserId, DateTime startDate, DateTime endDate)
        {
            // Get all payments without date filtering
            var payments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Tariff)
                .ToListAsync();

            var items = payments.Select(p => new PaymentReportItem
            {
                CustomerName = p.User?.FullName ?? "Unknown",
                TariffName = p.Tariff?.Name ?? "Unknown",
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                Status = p.Status,
                Reference = p.Reference,
                ProcessedBy = p.User?.FullName ?? "Unknown"
            }).ToList();

            var amountByTariff = items
                .GroupBy(i => i.TariffName)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

            return new PaymentReportData
            {
                TotalPayments = payments.Count,
                ApprovedPayments = payments.Count(p => p.Status == "Paid"),
                PendingPayments = payments.Count(p => p.Status == "Pending"),
                RejectedPayments = payments.Count(p => p.Status == "Rejected"),
                TotalAmount = payments.Sum(p => p.Amount),
                ApprovedAmount = payments.Where(p => p.Status == "Paid").Sum(p => p.Amount),
                AmountByTariff = amountByTariff,
                Payments = items
            };
        }

        public async Task<RevenueReportData> GetRevenueReportDataAsync(DateTime startDate, DateTime endDate)
        {
            // Get all payments without date filtering
            var paidPayments = await _context.Payments
                .Include(p => p.Tariff)
                .Where(p => p.Status == "Paid")
                .ToListAsync();

            var pendingPayments = await _context.Payments
                .Where(p => p.Status == "Pending")
                .ToListAsync();

            var monthlyBreakdown = paidPayments
                .GroupBy(p => p.PaymentDate.ToString("MMM yyyy"))
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

            var revenueByTariff = paidPayments
                .GroupBy(p => p.Tariff?.Name ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

            return new RevenueReportData
            {
                TotalRevenue = paidPayments.Sum(p => p.Amount),
                PendingRevenue = pendingPayments.Sum(p => p.Amount),
                TransactionCount = paidPayments.Count + pendingPayments.Count,
                MonthlyBreakdown = monthlyBreakdown,
                RevenueByTariff = revenueByTariff
            };
        }
        
        public async Task<string> GeneratePdfAsync(Report report)
        {
            try
            {
                var pdfBytes = _pdfService.GenerateReportPdf(report);

                var reportsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "reports");
                if (!Directory.Exists(reportsFolder))
                {
                    Directory.CreateDirectory(reportsFolder);
                }

                var fileName = $"report_{report.ReportId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                var filePath = Path.Combine(reportsFolder, fileName);

                await File.WriteAllBytesAsync(filePath, pdfBytes);

                return $"/reports/{fileName}";
            }
            catch (Exception ex)
            { 
                Console.WriteLine($"PDF Generation Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;  
            }
        }
    }
}
