using Kovsie_Living.Data;
using Kovsie_Living.Models;
using Kovsie_Living.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Kovsie_Living.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class OfficerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        public OfficerController(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new CreateOfficerViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOfficerViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var exists = await _userManager.FindByNameAsync(model.EmployeeNumber.Trim());
                    if (exists != null)
                    {
                        ModelState.AddModelError("", "This employee number is already registered.");
                        return View(model);
                    }

                    var emailExists = await _userManager.FindByEmailAsync(model.Email);
                    if (emailExists != null)
                    {
                        ModelState.AddModelError("", "An account with this email already exists.");
                        return View(model);
                    }

                    var user = new ApplicationUser
                    {
                        FirstName = model.FirstName.Trim(),
                        LastName = model.LastName.Trim(),
                        UserName = model.EmployeeNumber.Trim(),
                        Email = model.Email,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await _userManager.CreateAsync(user, model.TemporaryPassword);

                    if (result.Succeeded)
                    {
                        var officer = new FinancialOfficer
                        {
                            EmployeeNumber = model.EmployeeNumber.Trim(),
                            PhoneNumber = model.PhoneNumber.Trim(),
                            IsAuthorized = false,
                            UserId = user.Id
                        };
                        _context.FinancialOfficers.Add(officer);

                        var roleResult = await _userManager.AddToRoleAsync(user, "Officer");

                        if (roleResult.Succeeded)
                        {
                            await _context.SaveChangesAsync();
                            TempData["message"] = $"Officer {user.FullName} created successfully!";
                            TempData["messageType"] = "success";
                            return RedirectToAction("Officers", "Dashboard");
                        }

                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        await _userManager.DeleteAsync(user);
                        return View(model);
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                catch (DbException)
                {
                    ModelState.AddModelError("", "A database error occurred. Please try again.");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _context.Users
                .Include(u => u.FinancialOfficer)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["message"] = "Officer not found";
                return RedirectToAction("Officers", "Dashboard");
            }

            var model = new EditOfficerViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmployeeNumber = user.UserName,
                PhoneNumber = user.FinancialOfficer?.PhoneNumber ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditOfficerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .Include(u => u.FinancialOfficer)
                    .FirstOrDefaultAsync(u => u.Id == model.Id);

                if (user == null)
                {
                    TempData["message"] = "Officer not found";
                    TempData["messageType"] = "danger";
                    return RedirectToAction("Officers", "Dashboard");
                }

                if (user.UserName != model.EmployeeNumber)
                {
                    var existingUser = await _userManager.FindByNameAsync(model.EmployeeNumber);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        ModelState.AddModelError("", "Employee number is already taken");
                        return View(model);
                    }
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.UserName = model.EmployeeNumber;
                user.Email = $"{model.EmployeeNumber}@ufs.ac.za";

                if (user.FinancialOfficer != null)
                {
                    user.FinancialOfficer.EmployeeNumber = model.EmployeeNumber;
                    user.FinancialOfficer.PhoneNumber = model.PhoneNumber;
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _context.SaveChangesAsync();
                    TempData["message"] = "Officer updated successfully";
                    TempData["messageType"] = "success";
                    return RedirectToAction("Officers", "Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _context.Users
                .Include(u => u.FinancialOfficer)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["message"] = "Officer not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Officers", "Dashboard");
            }

            if (user.FinancialOfficer != null)
            {
                _context.FinancialOfficers.Remove(user.FinancialOfficer);
            }

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["message"] = "Officer deleted successfully";
                TempData["messageType"] = "success";
            }
            else
            {
                TempData["message"] = "Failed to delete officer";
                TempData["messageType"] = "danger";
            }

            return RedirectToAction("Officers", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Authorize(string id)
        {
            var user = await _context.Users
                .Include(u => u.FinancialOfficer)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["message"] = "Officer not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Officers", "Dashboard");
            }

            if (user.FinancialOfficer == null)
            {
                TempData["message"] = "Officer record not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Officers", "Dashboard");
            }

            user.FinancialOfficer.IsAuthorized = true;
            await _context.SaveChangesAsync();

            TempData["message"] = $"Officer {user.FullName} authorized successfully";
            TempData["messageType"] = "success";
            return RedirectToAction("Officers", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(string id)
        {
            var user = await _context.Users
                .Include(u => u.FinancialOfficer)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["message"] = "Officer not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Officers", "Dashboard");
            }

            if (user.FinancialOfficer == null)
            {
                TempData["message"] = "Officer record not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Officers", "Dashboard");
            }

            user.FinancialOfficer.IsAuthorized = false;
            await _context.SaveChangesAsync();

            TempData["message"] = $"Officer {user.FullName} authorization revoked";
            TempData["messageType"] = "success";
            return RedirectToAction("Officers", "Dashboard");
        }
    }
}
