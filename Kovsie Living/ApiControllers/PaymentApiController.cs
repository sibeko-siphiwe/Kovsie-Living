using Kovsie_Living.Data;
using Kovsie_Living.Models;
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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("tariffs")]
        public async Task<IActionResult> GetTariffs()
        {
            var tariffs = await _context.Tariffs
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .Select(t => new TariffDto
                {
                    TariffId = t.TariffId,
                    Name = t.Name,
                    Amount = t.Amount
                })
                .ToListAsync();

            return Ok(tariffs);
        }

        [HttpPost("make")]
        public async Task<IActionResult> MakePayment([FromBody] MakePaymentRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new PaymentResponseDto
                {
                    Success = false,
                    Message = "Invalid payment details"
                });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new PaymentResponseDto
                {
                    Success = false,
                    Message = "Invalid token"
                });
            }

            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new PaymentResponseDto
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            var isApproved = user.Student?.IsApproved ?? user.Staff?.IsApproved ?? false;

            if (!isApproved)
            {
                return BadRequest(new PaymentResponseDto
                {
                    Success = false,
                    Message = "Your account is pending approval. You cannot make payments yet."
                });
            }

            var tariff = await _context.Tariffs.FindAsync(model.TariffId);

            if (tariff == null || !tariff.IsActive)
            {
                return BadRequest(new PaymentResponseDto
                {
                    Success = false,
                    Message = "Selected tariff is not available"
                });
            }

            var reference = "REF" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            var payment = new Payment
            {
                UserId = user.Id,
                TariffId = tariff.TariffId,
                Amount = model.Amount,
                Reference = reference,
                Status = "Pending",
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "Online"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(new PaymentResponseDto
            {
                Success = true,
                Message = "Payment submitted successfully. Awaiting approval.",
                PaymentId = payment.PaymentId,
                TariffName = tariff.Name,
                Amount = payment.Amount,
                Status = payment.Status,
                Reference = payment.Reference,
                PaymentDate = payment.PaymentDate
            });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new PaymentHistoryResponseDto
                {
                    Success = false,
                    Message = "Invalid token"
                });
            }

            var payments = await _context.Payments
                .Include(p => p.Tariff)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var totalPaid = payments
                .Where(p => p.Status == "Paid")
                .Sum(p => p.Amount);

            var totalPending = payments
                .Where(p => p.Status == "Pending")
                .Sum(p => p.Amount);

            var history = payments.Select(p => new PaymentHistoryItemDto
            {
                PaymentId = p.PaymentId,
                TariffName = p.Tariff != null ? p.Tariff.Name : "Unknown",
                Amount = p.Amount,
                Status = p.Status,
                Reference = p.Reference,
                PaymentDate = p.PaymentDate
            }).ToList();

            return Ok(new PaymentHistoryResponseDto
            {
                Success = true,
                Message = "History retrieved",
                TotalPaid = totalPaid,
                TotalPending = totalPending,
                TransactionCount = payments.Count,
                Payments = history
            });
        }
    }
}