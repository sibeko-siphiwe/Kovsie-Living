using Kovsie_Living.Data;
using Kovsie_Living.Data.Repositories;
using Kovsie_Living.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kovsie_Living.Controllers
{
    [Authorize(Roles = "Administrator, Officer")]
    public class DashboardController : Controller
    {
        private readonly IDashboardRepository _repository;
        private readonly AppDbContext _context;

        public DashboardController(IDashboardRepository repository, AppDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        public IActionResult Index()
        {
            var users = _repository.GetAllCustomers().ToList();
            var officers = _repository.GetAllOfficers().ToList();
            var tariffs = _repository.GetActiveTariffs().ToList();

            // Calculate user status counts for the chart
            var activeUsers = users.Count(u => u.IsActive);
            var inactiveUsers = users.Count(u => !u.IsActive);

            var approvedUsers = users.Count(u =>
                (u.Student != null && u.Student.IsApproved) ||
                (u.Staff != null && u.Staff.IsApproved)
            );
            var pendingUsers = users.Count(u =>
                (u.Student != null && !u.Student.IsApproved) ||
                (u.Staff != null && !u.Staff.IsApproved)
            );

            // Get payment stats
            var pendingPaymentsCount = _context.Payments.Count(p => p.Status == "Pending");
            var recentPayments = _context.Payments
                .Include(p => p.User)
                .Include(p => p.Tariff)
                .OrderByDescending(p => p.PaymentDate)
                .Take(10)
                .ToList();

            // Calculate Total Revenue
            var totalRevenue = _context.Payments
                .Where(p => p.Status == "Paid")
                .Sum(p => p.Amount);

            var model = new DashboardViewModel
            {
                Users = users,
                Officers = officers,
                Tariffs = tariffs,
                ActiveUsers = activeUsers,
                InactiveUsers = inactiveUsers,
                ApprovedUsers = approvedUsers,
                PendingUsers = pendingUsers,
                PendingPaymentsCount = pendingPaymentsCount,
                RecentPayments = recentPayments,
                TotalRevenue = totalRevenue
            };

            return View(model);
        }

        public IActionResult Customers(string searchTerm, string userTypeFilter, string statusFilter, int page = 1)
        {
            var customers = _repository.GetAllCustomers().AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower().Trim();
                customers = customers.Where(u =>
                    (u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(searchTerm)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(searchTerm)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchTerm))
                );
            }

            if (!string.IsNullOrEmpty(userTypeFilter))
            {
                if (userTypeFilter == "Student")
                {
                    customers = customers.Where(u => u.Student != null);
                }
                else if (userTypeFilter == "Staff")
                {
                    customers = customers.Where(u => u.Staff != null);
                }
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (statusFilter == "Active")
                {
                    customers = customers.Where(u => u.IsActive &&
                        ((u.Student != null && u.Student.IsApproved) ||
                         (u.Staff != null && u.Staff.IsApproved)));
                }
                else if (statusFilter == "Inactive")
                {
                    customers = customers.Where(u => !u.IsActive);
                }
                else if (statusFilter == "Pending")
                {
                    customers = customers.Where(u => u.IsActive &&
                        ((u.Student != null && !u.Student.IsApproved) ||
                         (u.Staff != null && !u.Staff.IsApproved)));
                }
            }

            int pageSize = 10;
            int totalCount = customers.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedCustomers = customers
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.UserTypeFilter = userTypeFilter;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;

            return View(pagedCustomers);
        }

        public IActionResult Officers(string searchTerm, string statusFilter)
        {
            var officers = _repository.GetAllOfficers().AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower().Trim();
                officers = officers.Where(o =>
                    (o.FirstName != null && o.FirstName.ToLower().Contains(searchTerm)) ||
                    (o.LastName != null && o.LastName.ToLower().Contains(searchTerm)) ||
                    (o.UserName != null && o.UserName.ToLower().Contains(searchTerm)) ||
                    (o.Email != null && o.Email.ToLower().Contains(searchTerm)) ||
                    (o.FinancialOfficer != null && o.FinancialOfficer.EmployeeNumber.ToLower().Contains(searchTerm))
                );
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (statusFilter == "Active")
                {
                    officers = officers.Where(o => o.IsActive);
                }
                else if (statusFilter == "Inactive")
                {
                    officers = officers.Where(o => !o.IsActive);
                }
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.StatusFilter = statusFilter;

            return View(officers.ToList());
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Tariffs()
        {
            var tariffs = _repository.GetActiveTariffs();
            return View(tariffs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePayment(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Tariff)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                TempData["message"] = "Payment not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Index");
            }

            payment.Status = "Paid";
            payment.PaymentDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Trigger Notification
            var notificationService = HttpContext.RequestServices.GetRequiredService<Kovsie_Living.Services.NotificationService>();
            await notificationService.NotifyAdminsAndOfficers($"{User.Identity.Name} approved payment for {payment.User.FullName} - R {payment.Amount:N2}.", "success");

            TempData["message"] = $"Payment of R{payment.Amount} from {payment.User.FullName} approved!";
            TempData["messageType"] = "success";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPayment(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Tariff)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                TempData["message"] = "Payment not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Index");
            }

            payment.Status = "Rejected";
            await _context.SaveChangesAsync();

            TempData["message"] = $"Payment of R{payment.Amount} from {payment.User.FullName} rejected.";
            TempData["messageType"] = "danger";

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Messages(string searchTerm, string statusFilter, int page = 1)
        {
            var messages = _context.ContactMessages
                .Include(m => m.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower().Trim();
                messages = messages.Where(m =>
                    m.FullName.ToLower().Contains(searchTerm) ||
                    m.Email.ToLower().Contains(searchTerm) ||
                    m.Subject.ToLower().Contains(searchTerm) ||
                    m.Message.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                if (statusFilter == "Unread")
                    messages = messages.Where(m => !m.IsRead);
                else if (statusFilter == "Read")
                    messages = messages.Where(m => m.IsRead);
            }

            int pageSize = 10;
            int totalCount = await messages.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedMessages = await messages
                .OrderByDescending(m => m.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.UnreadCount = await _context.ContactMessages.CountAsync(m => !m.IsRead);

            return View(pagedMessages);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> MessageDetails(int id)
        {
            var message = await _context.ContactMessages
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.ContactMessageId == id);

            if (message == null)
            {
                TempData["message"] = "Message not found";
                TempData["messageType"] = "danger";
                return RedirectToAction(nameof(Messages));
            }

            // Mark as read automatically on open
            if (!message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ToggleMessageRead(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null)
            {
                TempData["message"] = "Message not found";
                TempData["messageType"] = "danger";
                return RedirectToAction(nameof(Messages));
            }

            message.IsRead = !message.IsRead;
            await _context.SaveChangesAsync();

            TempData["message"] = $"Message marked as {(message.IsRead ? "read" : "unread")}";
            TempData["messageType"] = "success";
            return RedirectToAction(nameof(Messages));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null)
            {
                TempData["message"] = "Message not found";
                TempData["messageType"] = "danger";
                return RedirectToAction(nameof(Messages));
            }

            _context.ContactMessages.Remove(message);
            await _context.SaveChangesAsync();

            TempData["message"] = "Message deleted successfully";
            TempData["messageType"] = "success";
            return RedirectToAction(nameof(Messages));
        }

        // ==========================================
        // NOTIFICATION ENDPOINTS
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(7)
                .Select(n => new {
                    n.NotificationId,
                    n.Message,
                    n.Type,
                    n.IsRead,
                    TimeAgo = n.CreatedAt.ToString("dd MMM HH:mm")
                })
                .ToListAsync();

            return Json(new { unreadCount, notifications });
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationsRead()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread) { n.IsRead = true; }
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            foreach (var n in notifications.Where(x => !x.IsRead)) { n.IsRead = true; }
            await _context.SaveChangesAsync();

            return View(notifications);
        }
    }
}