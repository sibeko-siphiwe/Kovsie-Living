using Kovsie_Living.Data;
using Kovsie_Living.Data.Repositories;
using Kovsie_Living.Models;
using Kovsie_Living.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kovsie_Living.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IReportRepository _reportRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ReportController(
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            IReportRepository reportRepository,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _reportRepository = reportRepository;
            _webHostEnvironment = webHostEnvironment;
        }

        // Officer: Generate Report
        [Authorize(Roles = "Officer")]
        [HttpGet]
        public IActionResult Generate()
        {
            var model = new GenerateReportViewModel();
            return View(model);
        }

        // Officer: Generate Report (POST)
        [Authorize(Roles = "Officer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(GenerateReportViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null)
                    {
                        return RedirectToAction("Login", "Account");
                    }

                    var report = await _reportRepository.GenerateReportAsync(
                        user.Id,
                        model.ReportType,
                        model.StartDate,
                        model.EndDate,
                        model.Description);

                    TempData["message"] = "Report generated successfully!";
                    TempData["messageType"] = "success";

                    return RedirectToAction("Details", new { id = report.ReportId });
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while generating the report.");
                }
            }
            return View(model);
        }

        // Admin: View All Reports
        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> Index(string officerFilter, string typeFilter)
        {
            var reports = _context.Reports
                .Include(r => r.GeneratedBy)
                .AsQueryable();

            if (!string.IsNullOrEmpty(officerFilter))
            {
                reports = reports.Where(r => r.GeneratedByUserId == officerFilter);
            }

            if (!string.IsNullOrEmpty(typeFilter))
            {
                reports = reports.Where(r => r.ReportType == typeFilter);
            }

            var reportList = await reports
                .OrderByDescending(r => r.GeneratedDate)
                .ToListAsync();

            var model = reportList.Select(r => new ReportViewModel
            {
                ReportId = r.ReportId,
                Title = r.Title,
                ReportType = r.ReportType,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                GeneratedDate = r.GeneratedDate,
                GeneratedBy = r.GeneratedBy?.FullName ?? "Unknown",
                Description = r.Description,
                Status = r.Status,
                FilePath = r.FilePath
            }).ToList();

            // Get officers for filter dropdown
            var officerRoleId = await _context.Roles
                .Where(r => r.Name == "Officer")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (officerRoleId != null)
            {
                var officerIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == officerRoleId)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                var officers = await _context.Users
                    .Where(u => officerIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync();

                ViewBag.Officers = officers;
            }
            else
            {
                ViewBag.Officers = new List<object>();
            }

            ViewBag.OfficerFilter = officerFilter;
            ViewBag.TypeFilter = typeFilter;

            return View(model);
        }

        // Officer: View My Reports
        [Authorize(Roles = "Officer")]
        [HttpGet]
        public async Task<IActionResult> MyReports()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var reports = await _context.Reports
                .Include(r => r.GeneratedBy)
                .Where(r => r.GeneratedByUserId == user.Id)
                .OrderByDescending(r => r.GeneratedDate)
                .ToListAsync();

            var model = reports.Select(r => new ReportViewModel
            {
                ReportId = r.ReportId,
                Title = r.Title,
                ReportType = r.ReportType,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                GeneratedDate = r.GeneratedDate,
                GeneratedBy = r.GeneratedBy?.FullName ?? "Unknown",
                Description = r.Description,
                Status = r.Status,
                FilePath = r.FilePath
            }).ToList();

            return View(model);
        }

        // View Report Details (Admin and Officer)
        [Authorize(Roles = "Administrator, Officer")]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var report = await _context.Reports
                .Include(r => r.GeneratedBy)
                .FirstOrDefaultAsync(r => r.ReportId == id);

            if (report == null)
            {
                TempData["message"] = "Report not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Index");
            }

            // Check authorization - Officer can only view their own reports
            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Officer") && report.GeneratedByUserId != user?.Id)
            {
                TempData["message"] = "You can only view your own reports";
                TempData["messageType"] = "danger";
                return RedirectToAction("MyReports");
            }

            var model = new ReportViewModel
            {
                ReportId = report.ReportId,
                Title = report.Title,
                ReportType = report.ReportType,
                StartDate = report.StartDate,
                EndDate = report.EndDate,
                GeneratedDate = report.GeneratedDate,
                GeneratedBy = report.GeneratedBy?.FullName ?? "Unknown",
                Description = report.Description,
                Status = report.Status,
                FilePath = report.FilePath
            };

            // Pass report data to view
            ViewBag.ReportDataJson = report.ReportDataJson;
            ViewBag.ReportType = report.ReportType;

            return View(model);
        }

        // Download PDF
        [Authorize(Roles = "Administrator, Officer")]
        [HttpGet]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null || string.IsNullOrEmpty(report.FilePath))
            {
                TempData["message"] = "PDF not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Index");
            }

            // Check authorization - Officer can only download their own reports
            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Officer") && report.GeneratedByUserId != user?.Id)
            {
                TempData["message"] = "You can only download your own reports";
                TempData["messageType"] = "danger";
                return RedirectToAction("MyReports");
            }

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, report.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                TempData["message"] = "PDF file not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Index");
            }

            var fileName = $"{report.Title}.pdf";
            return File(System.IO.File.ReadAllBytes(filePath), "application/pdf", fileName);
        }

        // Admin: Delete Report
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null)
            {
                TempData["message"] = "Report not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Index");
            }

            // Delete PDF file if exists
            if (!string.IsNullOrEmpty(report.FilePath))
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, report.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();

            TempData["message"] = "Report deleted successfully";
            TempData["messageType"] = "success";
            return RedirectToAction("Index");
        }
    }
}