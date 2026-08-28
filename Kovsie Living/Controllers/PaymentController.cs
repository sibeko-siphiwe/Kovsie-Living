using Kovsie_Living.Data;
using Kovsie_Living.Models;
using Kovsie_Living.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kovsie_Living.Controllers
{
    [Authorize(Roles = "Customer")]
    public class PaymentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public PaymentController(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> MakePayment()
        {
            var tariffs = await _context.Tariffs
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();

            var model = new MakePaymentViewModel
            {
                AvailableTariffs = tariffs,
                TariffId = tariffs.FirstOrDefault()?.TariffId ?? 0
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakePayment(MakePaymentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var tariff = await _context.Tariffs.FindAsync(model.TariffId);
                if (tariff == null)
                {
                    ModelState.AddModelError("", "Selected service not found");
                    model.AvailableTariffs = await _context.Tariffs.Where(t => t.IsActive).ToListAsync();
                    return View(model);
                }

                var reference = GenerateReference();

                var payment = new Payment
                {
                    UserId = user.Id,
                    TariffId = model.TariffId,
                    Amount = model.Amount,
                    Reference = reference,
                    Status = "Pending",  // ← Always starts as Pending
                    PaymentDate = DateTime.UtcNow,
                    PaymentMethod = "Online"
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                TempData["message"] = "Payment submitted successfully! Awaiting approval.";
                TempData["messageType"] = "info";

                return RedirectToAction("Success", new { id = payment.PaymentId });
            }

            model.AvailableTariffs = await _context.Tariffs.Where(t => t.IsActive).ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Confirm(int id)
        {
            var userId = _userManager.GetUserId(User);
            var payment = await _context.Payments
                .Include(p => p.Tariff)
                .FirstOrDefaultAsync(p => p.PaymentId == id && p.UserId == userId);

            if (payment == null)
            {
                TempData["message"] = "Payment not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Index", "Customer");
            }

            var model = new PaymentConfirmationViewModel
            {
                PaymentId = payment.PaymentId,
                TariffName = payment.Tariff?.Name ?? "Unknown",
                Amount = payment.Amount,
                Reference = payment.Reference,
                PaymentDate = payment.PaymentDate,
                Status = payment.Status
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var userId = _userManager.GetUserId(User);
            var payment = await _context.Payments
                .Include(p => p.Tariff)
                .FirstOrDefaultAsync(p => p.PaymentId == id && p.UserId == userId);

            if (payment == null)
            {
                return RedirectToAction("Index", "Customer");
            }

            var model = new PaymentConfirmationViewModel
            {
                PaymentId = payment.PaymentId,
                TariffName = payment.Tariff?.Name ?? "Unknown",
                Amount = payment.Amount,
                Reference = payment.Reference,
                PaymentDate = payment.PaymentDate,
                Status = payment.Status
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var userId = _userManager.GetUserId(User);
            var payments = await _context.Payments
                .Include(p => p.Tariff)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var totalPaid = payments.Where(p => p.Status == "Paid").Sum(p => p.Amount);
            var totalPending = payments.Where(p => p.Status == "Pending").Sum(p => p.Amount);

            var model = new PaymentHistoryViewModel
            {
                Payments = payments,
                TotalPaid = totalPaid,
                TotalPending = totalPending,
                PaymentCount = payments.Count
            };

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Pending()
        {
            var pendingPayments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Tariff)
                .Where(p => p.Status == "Pending")
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(pendingPayments);
        }

        private string GenerateReference()
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var reference = new string(Enumerable.Repeat(chars, 10)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return $"REF{reference}";
        }
    }
}
