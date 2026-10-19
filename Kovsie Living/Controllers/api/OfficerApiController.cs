using Kovsie_Living.Data;
using Kovsie_Living.Models.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kovsie_Living.Controllers.api
{
    [ApiController]
    [Route("api/officer")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Officer")]
    public class OfficerApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Models.ApplicationUser> _userManager;

        public OfficerApiController(AppDbContext context, UserManager<Models.ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new OfficerDashboardResponseDto { Success = false, Message = "Invalid token" });

            var user = await _context.Users.Include(u => u.FinancialOfficer).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new OfficerDashboardResponseDto { Success = false, Message = "Officer not found" });

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Officer";

            var customerRoleId = await _context.Roles.Where(r => r.Name == "Customer").Select(r => r.Id).FirstOrDefaultAsync();
            var totalCustomers = 0;
            if (!string.IsNullOrEmpty(customerRoleId))
            {
                var customerIds = _context.UserRoles.Where(ur => ur.RoleId == customerRoleId).Select(ur => ur.UserId);
                totalCustomers = await _context.Users.Where(u => customerIds.Contains(u.Id)).CountAsync();
            }

            var pendingPaymentsCount = await _context.Payments.Where(p => p.Status == "Pending").CountAsync();
            var totalRevenue = await _context.Payments.Where(p => p.Status == "Paid").SumAsync(p => (decimal?)p.Amount) ?? 0;

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
            var payment = await _context.Payments.Include(p => p.User).FirstOrDefaultAsync(p => p.PaymentId == id);
            if (payment == null) return NotFound(new { success = false, message = "Payment not found" });
            if (payment.Status != "Pending") return BadRequest(new { success = false, message = "Only pending payments can be approved" });

            payment.Status = "Paid";
            payment.PaymentDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // TRIGGER NOTIFICATIONS
            var notificationService = HttpContext.RequestServices.GetRequiredService<Kovsie_Living.Services.NotificationService>();

            // Notify the Admins/Officers (activity log)
            await notificationService.NotifyAdminsAndOfficers($"{User.Identity.Name} approved payment for {payment.User.FullName} - R {payment.Amount:N2}.", "success");

            // Notify the Customer directly
            await notificationService.NotifyUser(payment.UserId, $"Your payment of R {payment.Amount:N2} has been approved and processed.", "success");

            return Ok(new { success = true, message = $"Payment of R {payment.Amount:N2} approved successfully", paymentId = payment.PaymentId, status = payment.Status });
        }

        [HttpPost("reject/{id}")]
        public async Task<IActionResult> RejectPayment(int id)
        {
            var payment = await _context.Payments.Include(p => p.User).FirstOrDefaultAsync(p => p.PaymentId == id);
            if (payment == null) return NotFound(new { success = false, message = "Payment not found" });
            if (payment.Status != "Pending") return BadRequest(new { success = false, message = "Only pending payments can be rejected" });

            payment.Status = "Rejected";
            await _context.SaveChangesAsync();

            // Notify the Customer directly about rejection
            var notificationService = HttpContext.RequestServices.GetRequiredService<Kovsie_Living.Services.NotificationService>();
            await notificationService.NotifyUser(payment.UserId, $"Your payment of R {payment.Amount:N2} has been rejected.", "danger");

            return Ok(new { success = true, message = $"Payment of R {payment.Amount:N2} rejected", paymentId = payment.PaymentId, status = payment.Status });
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetMyReports()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new OfficerReportsResponseDto { Success = false, Message = "Invalid token" });

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

            return Ok(new OfficerReportsResponseDto { Success = true, Message = "Reports retrieved", TotalReports = reports.Count, Reports = reports });
        }

        [HttpGet("report/download/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null || string.IsNullOrEmpty(report.FilePath)) return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", report.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, "application/pdf", $"{report.Title}.pdf");
        }

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            var customerRoleId = await _context.Roles.Where(r => r.Name == "Customer").Select(r => r.Id).FirstOrDefaultAsync();
            if (string.IsNullOrEmpty(customerRoleId)) return Ok(new OfficerCustomersResponseDto { Success = true, Message = "No customers found", TotalCount = 0, Customers = new List<OfficerCustomerItemDto>() });

            var customerIds = await _context.UserRoles.Where(ur => ur.RoleId == customerRoleId).Select(ur => ur.UserId).ToListAsync();
            var customers = await _context.Users.Include(u => u.Student).Include(u => u.Staff).Where(u => customerIds.Contains(u.Id)).OrderByDescending(u => u.CreatedAt).ToListAsync();

            var items = customers.Select(u =>
            {
                var isStudent = u.Student != null;
                var isStaff = u.Staff != null;
                var isApproved = u.Student?.IsApproved ?? u.Staff?.IsApproved ?? false;

                return new OfficerCustomerItemDto
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    UserName = u.UserName,
                    Email = u.Email,
                    UserType = isStudent ? "Student" : isStaff ? "Staff" : "Unknown",
                    IsApproved = isApproved,
                    IsActive = u.IsActive
                };
            }).ToList();

            return Ok(new OfficerCustomersResponseDto { Success = true, Message = "Customers retrieved", TotalCount = items.Count, Customers = items });
        }

        [HttpPost("customers/toggle-approval/{userId}")]
        public async Task<IActionResult> ToggleCustomerApproval(string userId)
        {
            var user = await _context.Users.Include(u => u.Student).Include(u => u.Staff).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new { success = false, message = "User not found" });

            if (user.Student != null)
            {
                user.Student.IsApproved = !user.Student.IsApproved;
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = $"Student {(user.Student.IsApproved ? "approved" : "unapproved")} successfully" });
            }
            else if (user.Staff != null)
            {
                user.Staff.IsApproved = !user.Staff.IsApproved;
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = $"Staff {(user.Staff.IsApproved ? "approved" : "unapproved")} successfully" });
            }

            return BadRequest(new { success = false, message = "User is not a customer" });
        }
    }
}