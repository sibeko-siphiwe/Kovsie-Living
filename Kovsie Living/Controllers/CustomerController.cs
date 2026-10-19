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
    [Authorize(Roles = "Customer, Administrator, Officer")]
    public class CustomerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        public CustomerController(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var isStudent = user.Student != null;
            var isStaff = user.Staff != null;
            var isApproved = isStudent ? (user.Student?.IsApproved ?? false) : (isStaff ? (user.Staff?.IsApproved ?? false) : false);

            var payments = await _context.Payments
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var totalPaid = payments.Where(p => p.Status == "Paid").Sum(p => p.Amount);
            var pendingPayments = payments.Where(p => p.Status == "Pending").Sum(p => p.Amount);
            var transactionCount = payments.Count;

            var recentTransactions = await _context.Payments
                .Include(p => p.Tariff)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToListAsync();

            var model = new CustomerDashboardViewModel
            {
                User = user,
                IsStudent = isStudent,
                IsStaff = isStaff,
                IsApproved = isApproved,
                TotalPaid = totalPaid,
                PendingPayments = pendingPayments,
                TransactionCount = transactionCount,
                RecentTransactions = recentTransactions
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["message"] = "Invalid user identifier";
                TempData["messageType"] = "danger";
                return RedirectToAction("Customers", "Dashboard");
            }

            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Staff)
                .Include(u => u.FinancialOfficer)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["message"] = "User not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Customers", "Dashboard");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Customer";

            var payments = await _context.Payments
                .Where(p => p.UserId == id)
                .ToListAsync();

            ViewBag.TotalPaid = payments.Where(p => p.Status == "Paid").Sum(p => p.Amount);
            ViewBag.TotalPending = payments.Where(p => p.Status == "Pending").Sum(p => p.Amount);
            ViewBag.PaymentsCount = payments.Count;
            ViewBag.UserRole = role;

            return View(user);
        }

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
                    var exists = await _userManager.FindByNameAsync(model.UserName.Trim());
                    if (exists != null)
                    {
                        ModelState.AddModelError("", "This student/staff number is already registered.");
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
                        UserName = model.UserName.Trim(),
                        Email = model.Email,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await _userManager.CreateAsync(user, model.TemporaryPassword);

                    if (result.Succeeded)
                    {
                        if (model.IsStudent)
                        {
                            var student = new Student
                            {
                                StudentNumber = model.UserName.Trim(),
                                IsApproved = true,
                                UserId = user.Id
                            };
                            _context.Students.Add(student);
                        }
                        else
                        {
                            var staff = new Staff
                            {
                                StaffNumber = model.UserName.Trim(),
                                IsApproved = true,
                                UserId = user.Id
                            };
                            _context.Staff.Add(staff);
                        }

                        var roleResult = await _userManager.AddToRoleAsync(user, "Customer");

                        if (roleResult.Succeeded)
                        {
                            await _context.SaveChangesAsync();
                            TempData["message"] = $"User {user.FullName} created successfully!";
                            TempData["messageType"] = "success";
                            return RedirectToAction("Customers", "Dashboard");
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

            return View(model);
        }

        [HttpPost]
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
                    TempData["messageType"] = "danger";
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
                }
                else if (user.Staff != null)
                {
                    user.Staff.StaffNumber = model.UserName;
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _context.SaveChangesAsync();
                    TempData["message"] = "User updated successfully";
                    TempData["messageType"] = "success";

                    if (!User.IsInRole("Customer"))
                    {
                        return RedirectToAction("Customers", "Dashboard");
                    }
                    else
                    {
                        return RedirectToAction("Profile", "Customer");
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
                    TempData["messageType"] = "danger";
                    return RedirectToAction("Customers", "Dashboard");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (result.Succeeded)
                {
                    if (User.IsInRole("Administrator") || User.IsInRole("Officer"))
                    {
                        TempData["message"] = "Password changed successfully";
                        TempData["messageType"] = "success";
                        return RedirectToAction("Customers", "Dashboard");
                    }
                    else
                    {
                        return RedirectToAction("Profile", "Customer");
                    }
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
                TempData["messageType"] = "danger";
                return RedirectToAction("Customers", "Dashboard");
            }

            if (user.Student != null) _context.Students.Remove(user.Student);
            if (user.Staff != null) _context.Staff.Remove(user.Staff);
            if (user.FinancialOfficer != null) _context.FinancialOfficers.Remove(user.FinancialOfficer);

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["message"] = "User deleted successfully";
                TempData["messageType"] = "success";
            }
            else
            {
                TempData["message"] = "Failed to delete user";
                TempData["messageType"] = "danger";
            }

            return RedirectToAction("Customers", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleApproval(string id)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["message"] = "User not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Customers", "Dashboard");
            }

            if (user.Student != null)
            {
                user.Student.IsApproved = !user.Student.IsApproved;
                await _context.SaveChangesAsync();
                TempData["message"] = $"Student {(user.Student.IsApproved ? "approved" : "unapproved")} successfully";
                TempData["messageType"] = "success";
            }
            else if (user.Staff != null)
            {
                user.Staff.IsApproved = !user.Staff.IsApproved;
                await _context.SaveChangesAsync();
                TempData["message"] = $"Staff {(user.Staff.IsApproved ? "approved" : "unapproved")} successfully";
                TempData["messageType"] = "success";
            }
            else
            {
                TempData["message"] = "User is not a customer";
                TempData["messageType"] = "danger";
            }

            return RedirectToAction("Customers", "Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["message"] = "User not found";
                TempData["messageType"] = "danger";
                return RedirectToAction("Customers", "Dashboard");
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            TempData["message"] = $"User {(user.IsActive ? "activated" : "deactivated")} successfully";
            TempData["messageType"] = "success";
            return RedirectToAction("Customers", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                IsStudent = user.Student != null,
                CurrentProfilePicture = user.ProfilePicture,
                CurrentProfilePictureContentType = user.ProfilePictureContentType
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(EditUserViewModel model)
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
                    TempData["messageType"] = "danger";
                    return RedirectToAction("Index");
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

                if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await model.ProfilePictureFile.CopyToAsync(memoryStream);
                        user.ProfilePicture = memoryStream.ToArray();
                        user.ProfilePictureContentType = model.ProfilePictureFile.ContentType;
                    }
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.UserName = model.UserName;
                user.Email = $"{model.UserName}@ufs.ac.za";

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _context.SaveChangesAsync();
                    TempData["message"] = "Profile updated successfully!";
                    TempData["messageType"] = "success";
                    return RedirectToAction("Profile");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }
    }
}