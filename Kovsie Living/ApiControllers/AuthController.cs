using Kovsie_Living.Data;
using Kovsie_Living.Helpers;
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
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly JwtHelper _jwtHelper;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext context,
            JwtHelper jwtHelper)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _jwtHelper = jwtHelper;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            try
            {
                var user = await _userManager.FindByNameAsync(model.Username);

                if (user == null)
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    });
                }

                if (!user.IsActive)
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Your account is currently inactive. Please contact support."
                    });
                }

                if (await _userManager.IsInRoleAsync(user, "Officer"))
                {
                    var officer = await _context.FinancialOfficers
                        .FirstOrDefaultAsync(o => o.UserId == user.Id);

                    if (officer == null || !officer.IsAuthorized)
                    {
                        return Unauthorized(new AuthResponseDto
                        {
                            Success = false,
                            Message = "Your account has not been authorized yet. Please contact the administrator."
                        });
                    }
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user, model.Password, model.RememberMe, false);

                if (!result.Succeeded)
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    });
                }

                var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Customer";

                var isApproved = true;
                var unitNumber = "N/A";
                var vehicleNumber = 0;

                if (user.Student != null)
                {
                    isApproved = user.Student.IsApproved;
                    unitNumber = user.Student.UnitNumber;
                    vehicleNumber = user.Student.VehicleNumber;
                }
                else if (user.Staff != null)
                {
                    isApproved = user.Staff.IsApproved;
                    unitNumber = user.Staff.UnitNumber;
                    vehicleNumber = user.Staff.VehicleNumber;
                }

                var token = _jwtHelper.GenerateToken(user, role);

                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = role,
                    IsActive = user.IsActive,
                    IsApproved = isApproved,
                    UnitNumber = unitNumber,
                    VehicleNumber = vehicleNumber
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "Something went wrong, please try again."
                });
            }
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            try
            {
                var exists = await _userManager.FindByNameAsync(model.UserName);

                if (exists != null)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "User is already registered, please login."
                    });
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

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Registration failed",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                if (model.IsStudent)
                {
                    _context.Students.Add(new Student
                    {
                        StudentNumber = model.UserName,
                        UnitNumber = model.UnitNumber?.Trim(),
                        VehicleNumber = model.VehicleNumber,
                        UserId = user.Id
                    });
                }
                else
                {
                    _context.Staff.Add(new Staff
                    {
                        StaffNumber = model.UserName,
                        UnitNumber = model.UnitNumber?.Trim(),
                        VehicleNumber = model.VehicleNumber,
                        UserId = user.Id
                    });
                }

                var roleResult = await _userManager.AddToRoleAsync(user, "Customer");

                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Registration failed",
                        Errors = roleResult.Errors.Select(e => e.Description).ToList()
                    });
                }

                await _context.SaveChangesAsync();

                var token = _jwtHelper.GenerateToken(user, "Customer");

                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Registration successful",
                    Token = token,
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = "Customer",
                    IsActive = true,
                    IsApproved = false,
                    UnitNumber = model.UnitNumber ?? "N/A",
                    VehicleNumber = model.VehicleNumber
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred while creating your account."
                });
            }
        }

        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid token"
                });
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized(new AuthResponseDto
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new AuthResponseDto
                {
                    Success = false,
                    Message = "Your account is currently inactive."
                });
            }

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Customer";

            var isApproved = true;
            var unitNumber = "N/A";
            var vehicleNumber = 0;

            if (user.Student != null)
            {
                isApproved = user.Student.IsApproved;
                unitNumber = user.Student.UnitNumber;
                vehicleNumber = user.Student.VehicleNumber;
            }
            else if (user.Staff != null)
            {
                isApproved = user.Staff.IsApproved;
                unitNumber = user.Staff.UnitNumber;
                vehicleNumber = user.Staff.VehicleNumber;
            }

            return Ok(new AuthResponseDto
            {
                Success = true,
                Message = "Token is valid",
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = role,
                IsActive = user.IsActive,
                IsApproved = isApproved,
                UnitNumber = unitNumber,
                VehicleNumber = vehicleNumber
            });
        }

        [HttpPost("change-password")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid token"
                });
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound(new AuthResponseDto
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = "Password change failed",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            return Ok(new AuthResponseDto
            {
                Success = true,
                Message = "Password changed successfully"
            });
        }
    }
}