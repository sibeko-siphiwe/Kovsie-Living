using Kovsie_Living.Data;
using Kovsie_Living.Models;
using Kovsie_Living.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kovsie_Living.Controllers
{
    public class ReportController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public ReportController(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
         
        [Authorize(Roles = "Officer")]
        [HttpGet]
        public IActionResult Generate()
        {
            var model = new GenerateReportViewModel();
            return View(model);
        }
         
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

                    // Generate report data (simulated)
                    var reportData = await GenerateReportData(model.ReportType, model.StartDate, model.EndDate);

                    // Save report to database
                    var report = new Report
                    {
                        Title = $"{model.ReportType} Report - {DateTime.UtcNow:dd MMM yyyy}",
                        ReportType = model.ReportType,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        GeneratedDate = DateTime.UtcNow,
                        GeneratedByUserId = user.Id,
                        Description = model.Description ?? $"{model.ReportType} report generated",
                        FilePath = $"/reports/report_{Guid.NewGuid()}.pdf", // Simulated path
                        Status = "Generated"
                    };

                    _context.Reports.Add(report);
                    await _context.SaveChangesAsync();

                    TempData["message"] = "Report generated successfully!";
                    TempData["messageType"] = "success";

                    return RedirectToAction("Generate");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while generating the report.");
                }
            }
            return View(model);
        }
         
        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var reports = await _context.Reports
                .Include(r => r.GeneratedBy)
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
         
        [Authorize(Roles = "Administrator")]
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

            return View(model);
        }
         
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

            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();

            TempData["message"] = "Report deleted successfully";
            TempData["messageType"] = "success";
            return RedirectToAction("Index");
        }
         
        private async Task<object> GenerateReportData(string reportType, DateTime startDate, DateTime endDate)
        { 
            return new
            {
                ReportType = reportType,
                StartDate = startDate,
                EndDate = endDate,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }
}
