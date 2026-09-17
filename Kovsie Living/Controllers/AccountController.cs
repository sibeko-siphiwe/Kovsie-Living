using Kovsie_Living.Data;
using Kovsie_Living.Models;
using Kovsie_Living.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.Common;

namespace Kovsie_Living.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;
        public AccountController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.FindByNameAsync(model.Username);

                    if (user != null)
                    {
                        if (!user.IsActive)
                        {
                            ModelState.AddModelError("", "Your account is currently inactive. Please contact support.");
                            return View(model);
                        }
                        var results = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);

                        if (results.Succeeded)
                        {
                            if (User.IsInRole("Administrator")|| User.IsInRole("Officer"))
                            {
                                return RedirectToAction("Index", "Dashboard");
                            }
                            else
                            {
                                return RedirectToAction("Index", "Customer");
                            }
                        }
                        ModelState.AddModelError("", "Invalid username or password");
                    }
                    ModelState.AddModelError("", "Invalid username or password");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Something went wrong, while logging in please try again.");
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
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

                    var results = await _userManager.CreateAsync(user, model.Password);

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

                        var roleResult = await _userManager.AddToRoleAsync(user, "Customer");

                        if (roleResult.Succeeded)
                        {
                            await _context.SaveChangesAsync();

                            await _signInManager.SignInAsync(user, isPersistent: true);
                            return RedirectToAction("Index", "Customer");
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
                    ModelState.AddModelError("", "An unexpected error occurred while creating your account.");
                    return View(model);
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ProfilePicture(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.ProfilePicture == null || user.ProfilePicture.Length == 0)
            {
                return NotFound();
            }

            return File(user.ProfilePicture, user.ProfilePictureContentType ?? "image/jpeg");
        }
    }
}
