using Kovsie_Living.Data;
using Kovsie_Living.Models.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kovsie_Living.ApiControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Officer")]
    public class OfficerController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Models.ApplicationUser> _userManager;

        public OfficerController(
            AppDbContext context,
            UserManager<Models.ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new OfficerDashboardResponseDto
                {
                    Success = false,
                    Message = "Invalid token"
                });
            }

            var user = await _context.Users
                .Include(u => u.FinancialOfficer)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new OfficerDashboardResponseDto
                {
                    Success = false,
                    Message = "Officer not found"
                });
            }

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Officer";

            // Count customers (users with Customer role)
            var customerRoleId = await _context.Roles
                .Where(r => r.Name == "Customer")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var totalCustomers = 0;
            if (!string.IsNullOrEmpty(customerRoleId))
            {
                var customerIds = _context.UserRoles
                    .Where(ur => ur.RoleId == customerRoleId)
                    .Select(ur => ur.UserId);

                totalCustomers = await _context.Users
                    .Where(u => customerIds.Contains(u.Id))
                    .CountAsync();
            }

            // Pending payments count
            var pendingPaymentsCount = await _context.Payments
                .Where(p => p.Status == "Pending")
                .CountAsync();

            // Total revenue (paid)
            var totalRevenue = await _context.Payments
                .Where(p => p.Status == "Paid")
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            // Recent 5 payments across all customers
            var recentPayments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Tariff)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .Select(p => new OfficerRecentPaymentDto
                {
                    PaymentId = p.PaymentId,
                    CustomerName = p.User != null ? p.User.FullName : "Unknown",
                    TariffName = p.Tariff != null ? p.Tariff.Name : "Unknown",
                    Amount = p.Amount,
                    Status = p.Status,
                    Reference = p.Reference,
                    PaymentDate = p.PaymentDate
                })
                .ToListAsync();

            return Ok(new OfficerDashboardResponseDto
            {
                Success = true,
                Message = "Dashboard loaded",
                FullName = user.FullName,
                Role = role,
                TotalCustomers = totalCustomers,
                PendingPaymentsCount = pendingPaymentsCount,
                TotalRevenue = totalRevenue,
                RecentPayments = recentPayments
            });
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingPayments()
        {
            var payments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Tariff)
                .Where(p => p.Status == "Pending")
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new PendingPaymentItemDto
                {
                    PaymentId = p.PaymentId,
                    CustomerName = p.User != null ? p.User.FullName : "Unknown",
                    CustomerNumber = p.User != null ? p.User.UserName : "N/A",
                    TariffName = p.Tariff != null ? p.Tariff.Name : "Unknown",
                    Amount = p.Amount,
                    Reference = p.Reference,
                    PaymentDate = p.PaymentDate
                })
                .ToListAsync();

            return Ok(new PendingPaymentsResponseDto
            {
                Success = true,
                Message = "Pending payments retrieved",
                TotalPending = payments.Count,
                Payments = payments
            });
        }

        [HttpPost("approve/{id}")]
        public async Task<IActionResult> ApprovePayment(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Payment not found"
                });
            }

            if (payment.Status != "Pending")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Only pending payments can be approved"
                });
            }

            payment.Status = "Paid";
            payment.PaymentDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Payment of R {payment.Amount:N2} approved successfully",
                paymentId = payment.PaymentId,
                status = payment.Status
            });
        }

        [HttpPost("reject/{id}")]
        public async Task<IActionResult> RejectPayment(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Payment not found"
                });
            }

            if (payment.Status != "Pending")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Only pending payments can be rejected"
                });
            }

            payment.Status = "Rejected";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Payment of R {payment.Amount:N2} rejected",
                paymentId = payment.PaymentId,
                status = payment.Status
            });
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetMyReports()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new OfficerReportsResponseDto
                {
                    Success = false,
                    Message = "Invalid token"
                });
            }

            var reports = await _context.Reports
                .Where(r => r.GeneratedByUserId == userId)
                .OrderByDescending(r => r.GeneratedDate)
                .Select(r => new OfficerReportItemDto
                {
                    ReportId = r.ReportId,
                    Title = r.Title,
                    ReportType = r.ReportType,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    GeneratedDate = r.GeneratedDate,
                    Status = r.Status,
                    FilePath = r.FilePath
                })
                .ToListAsync();

            return Ok(new OfficerReportsResponseDto
            {
                Success = true,
                Message = "Reports retrieved",
                TotalReports = reports.Count,
                Reports = reports
            });
        }

        [HttpGet("report/download/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);

            if (report == null || string.IsNullOrEmpty(report.FilePath))
            {
                return NotFound();
            }

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                report.FilePath.TrimStart('/')
            );

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, "application/pdf", $"{report.Title}.pdf");
        }
    }
}