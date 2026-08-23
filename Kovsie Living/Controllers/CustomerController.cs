using Kovsie_Living.Data;
using Kovsie_Living.Models;
using Kovsie_Living.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace Kovsie_Living.Controllers
{
    public class CustomerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        public CustomerController(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        //public async Task<IActionResult> Details(string id)
        //{
        //    var user = await _context.Users
        //        .Include(u => u.Student)
        //        .Include(u => u.Staff)
        //        .Include(u => u.FinancialOfficer)
        //        .FirstOrDefaultAsync(u => u.Id == id);

        //    if (user == null)
        //    {
        //        TempData["message"] = "User not found";
        //        return RedirectToAction("Users", "Dashboard");
        //    }
             
        //    var roles = await _userManager.GetRolesAsync(user);
        //    ViewBag.Role = roles.FirstOrDefault() ?? "N/A";

        //    ViewBag.Payments = await _context.Payments
        //        .Include(p => p.Tariff)
        //        .Where(p => p.UserId == id)
        //        .OrderByDescending(p => p.CreatedAt)
        //        .ToListAsync();

        //    ViewBag.TotalPaid = await _context.Payments
        //        .Where(p => p.UserId == id && p.Status == "Paid")
        //        .SumAsync(p => p.Amount);

        //    ViewBag.TotalPending = await _context.Payments
        //        .Where(p => p.UserId == id && (p.Status == "Pending" || p.Status == "Overdue"))
        //        .SumAsync(p => p.Amount);

        //    ViewBag.PaymentsCount = await _context.Payments
        //        .Where(p => p.UserId == id)
        //        .CountAsync();

        //    return View(user);
        //}

        [HttpGet]
        public IActionResult Create()
        {
            var model = new CreateUserViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var exists = await _userManager.FindByNameAsync(model.UserName);

                    if (exists != null)
                    {
                        ModelState.AddModelError("", "User is already registered, please login.");
                        return View(model);
                    }

                    var user = new ApplicationUser
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        UserName = model.UserName,
                        Email = $"{model.UserName}@ufs.ac.za",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var results = await _userManager.CreateAsync(user, model.TemporaryPassword);

                    if (results.Succeeded)
                    {
                        if (model.IsStudent)
                        {
                            var student = new Student
                            {
                                StudentNumber = model.UserName,
                                UnitNumber = model.UnitNumber?.Trim(),
                                VehicleNumber = model.VehicleNumber,
                                UserId = user.Id
                            };

                            _context.Students.Add(student);
                        }
                        else
                        {
                            var staff = new Staff
                            {
                                StaffNumber = model.UserName,
                                UnitNumber = model.UnitNumber?.Trim(),
                                VehicleNumber = model.VehicleNumber,
                                UserId = user.Id
                            };

                            _context.Staff.Add(staff);
                        }

                        var roleResult = await _userManager.AddToRoleAsync(user, "Customers");

                        if (roleResult.Succeeded)
                        {
                            await _context.SaveChangesAsync(); 
                            return RedirectToAction("Customers", "Dashboard");
                        }
                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        await _userManager.DeleteAsync(user);
                        return View(model);
                    }
                    foreach (var error in results.Errors)
                    {
                        ModelState.AddModelError("", $"An error has occured: {error.Description.ToString()}");
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occured while creating user");
                }
            }
            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> Update(string id)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["message"] = "User not found";
                return RedirectToAction("Customers", "Dashboard");
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                IsStudent = user.Student != null,
            };

            if (user.Student != null)
            {
                model.UnitNumber = user.Student.UnitNumber;
                model.VehicleNumber = user.Student.VehicleNumber;
            }
            else if (user.Staff != null)
            {
                model.UnitNumber = user.Staff.UnitNumber;
                model.VehicleNumber = user.Staff.VehicleNumber;
            }

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .Include(u => u.Student)
                    .Include(u => u.Staff)
                    .FirstOrDefaultAsync(u => u.Id == model.Id);

                if (user == null)
                {
                    TempData["message"] = "User not found";
                    return RedirectToAction("Customers", "Dashboard");
                }

                if (user.UserName != model.UserName)
                {
                    var existingUser = await _userManager.FindByNameAsync(model.UserName);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        ModelState.AddModelError("", "Username is already taken");
                        return View(model);
                    }
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.UserName = model.UserName;
                user.Email = $"{model.UserName}@ufs.ac.za";

                if (user.Student != null)
                {
                    user.Student.StudentNumber = model.UserName;
                    user.Student.UnitNumber = model.UnitNumber;
                    user.Student.VehicleNumber = model.VehicleNumber;
                }
                else if (user.Staff != null)
                {
                    user.Staff.StaffNumber = model.UserName;
                    user.Staff.UnitNumber = model.UnitNumber;
                    user.Staff.VehicleNumber = model.VehicleNumber;
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _context.SaveChangesAsync();
                    TempData["message"] = "User updated successfully";

                    if (!User.IsInRole("Customers"))
                    {
                        return RedirectToAction("Customers", "Dashboard");
                    }
                    else
                    {
                        return RedirectToAction("Account", "Profile");
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", $"An error has occured: {error.Description.ToString()}");
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["message"] = "User not found";
                return RedirectToAction("Customers", "Dashboard");
            }

            var model = new ChangePasswordViewModel
            {
                Id = user.Id,
                FullName = user.FullName
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    TempData["message"] = "User not found";
                    return RedirectToAction("Customers", "Dashboard");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (result.Succeeded)
                {
                    TempData["message"] = "Password changed successfully";
                    return RedirectToAction("Customers", "Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", $"An error has occured: {error.Description.ToString()}");
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Staff)
                .Include(u => u.FinancialOfficer)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["message"] = "User not found";
                return RedirectToAction("Customers", "Dashboard");
            }

            // Remove related profile
            if (user.Student != null)
            {
                _context.Students.Remove(user.Student);
            }
            if (user.Staff != null)
            {
                _context.Staff.Remove(user.Staff);
            }
            if (user.FinancialOfficer != null)
            {
                _context.FinancialOfficers.Remove(user.FinancialOfficer);
            }

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["message"] = "User deleted successfully";
            }
            else
            {
                TempData["message"] = "Failed to delete user";
            }

            return RedirectToAction("Customers", "Dashboard");
        }
    }
}
