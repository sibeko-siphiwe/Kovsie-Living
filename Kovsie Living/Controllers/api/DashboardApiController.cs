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
    [Route("api/dashboard")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class DashboardApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Models.ApplicationUser> _userManager;

        public DashboardApiController(AppDbContext context, UserManager<Models.ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new DashboardResponseDto { Success = false, Message = "Invalid token" });

            var user = await _context.Users.Include(u => u.Student).Include(u => u.Staff).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new DashboardResponseDto { Success = false, Message = "User not found" });

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Customer";

            var isApproved = true;
            if (user.Student != null) isApproved = user.Student.IsApproved;
            else if (user.Staff != null) isApproved = user.Staff.IsApproved;

            var payments = await _context.Payments.Where(p => p.UserId == userId).ToListAsync();

            var recentTransactions = await _context.Payments
                .Include(p => p.Tariff)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .Select(p => new RecentTransactionDto
                {
                    PaymentId = p.PaymentId,
                    TariffName = p.Tariff != null ? p.Tariff.Name : "Unknown",
                    Amount = p.Amount,
                    Status = p.Status,
                    Reference = p.Reference,
                    PaymentDate = p.PaymentDate
                }).ToListAsync();

            return Ok(new DashboardResponseDto
            {
                Success = true,
                Message = "Dashboard loaded",
                FullName = user.FullName,
                UserName = user.UserName,
                Role = role,
                IsApproved = isApproved,
                TotalPaid = payments.Where(p => p.Status == "Paid").Sum(p => p.Amount),
                PendingPayments = payments.Where(p => p.Status == "Pending").Sum(p => p.Amount),
                TransactionCount = payments.Count,
                RecentTransactions = recentTransactions
            });
        }
    }
}