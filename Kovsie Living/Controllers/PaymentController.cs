using Kovsie_Living.Data;
using Kovsie_Living.Helpers;
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
                TariffId = tariffs.FirstOrDefault()?.TariffId ?? 0,
                Amount = tariffs.FirstOrDefault()?.Amount.ToString("F2") ?? "0.00"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakePayment(MakePaymentViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var tariff = await _context.Tariffs.FindAsync(model.TariffId);
            if (tariff == null)
            {
                ModelState.AddModelError("", "Selected service not found");
                model.AvailableTariffs = await _context.Tariffs.Where(t => t.IsActive).ToListAsync();
                return View(model);
            }

            // 1. FORCE THE AMOUNT (Ignore what the client sent)
            model.Amount = tariff.Amount.ToString("F2");
            model.TariffName = tariff.Name;

            // 2. CHECK MONTHLY LIMIT
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;

            var existingPayment = await _context.Payments
                .Where(p => p.UserId == user.Id
                         && p.TariffId == model.TariffId
                         && p.Status == "Paid"
                         && p.PaymentDate.Month == currentMonth
                         && p.PaymentDate.Year == currentYear)
                .FirstOrDefaultAsync();

            if (existingPayment != null)
            {
                ModelState.AddModelError("", $"You have already paid for {tariff.Name} this month. You can only pay once per month.");
            }

            // 3. VALIDATE CARD DETAILS
            var cleanCardNumber = model.CardNumber?.Replace(" ", "") ?? "";

            if (!CardValidator.IsValidCardNumber(cleanCardNumber))
            {
                ModelState.AddModelError("CardNumber", "Invalid card number. Please check and try again.");
            }
            if (!CardValidator.IsValidExpiryDate(model.ExpiryDate))
            {
                ModelState.AddModelError("ExpiryDate", "Card is expired or invalid.");
            }

            if (!ModelState.IsValid)
            {
                model.AvailableTariffs = await _context.Tariffs.Where(t => t.IsActive).ToListAsync();
                return View(model);
            }

            // 4. SIMULATE PAYMENT PROCESSING (Delay happens BEFORE DB operations to prevent SQL timeouts)
            await Task.Delay(1500);

            var reference = GenerateReference();

            var payment = new Payment
            {
                UserId = user.Id,
                TariffId = model.TariffId,
                Amount = tariff.Amount, // Safely stored as decimal
                Reference = reference,
                Status = "Pending",
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "Card (Simulated)",
                Notes = $"Card ending in {cleanCardNumber.Substring(cleanCardNumber.Length - 4)}"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // After _context.SaveChangesAsync(); in MakePayment POST
            var notificationService = HttpContext.RequestServices.GetRequiredService<Kovsie_Living.Services.NotificationService>();
            await notificationService.NotifyAdminsAndOfficers($"{user.FullName} has paid R {payment.Amount:N2} for {tariff.Name}.", "success");

            // 5. PREVENT EF TRACKING CRASHES ON REDIRECT
            // Detach the user and tariff so EF Core doesn't try to update them again on redirect
            _context.Entry(user).State = EntityState.Detached;
            _context.Entry(tariff).State = EntityState.Detached;

            TempData["message"] = $"Payment of R{payment.Amount} for {tariff.Name} submitted successfully! Awaiting approval.";
            TempData["messageType"] = "info";

            // Explicitly route to the Web Payment Success page
            return RedirectToAction("Success", "Payment", new { id = payment.PaymentId });
        }

        [HttpGet]
        public async Task<IActionResult> Confirm(int id)
        {
            var userId = _userManager.GetUserId(User);
            var payment = await _context.Payments
                .Include(p => p.Tariff)
                .FirstOrDefaultAsync(p => p.PaymentId == id && p.UserId == userId);

            if (payment == null) return RedirectToAction("Index", "Customer");

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

            if (payment == null) return RedirectToAction("Index", "Customer");

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