using Kovsie_Living.Data;
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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ProfileResponseDto
                {
                    Success = false,
                    Message = "Invalid token"
                });
            }

            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new ProfileResponseDto
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Customer";

            var isApproved = true;
            var unitNumber = "N/A";
            var vehicleNumber = 0;

            if (user.Student != null)
            {
                isApproved = user.Student.IsApproved;
                unitNumber = user.Student.UnitNumber ?? "N/A";
                vehicleNumber = user.Student.VehicleNumber;
            }
            else if (user.Staff != null)
            {
                isApproved = user.Staff.IsApproved;
                unitNumber = user.Staff.UnitNumber ?? "N/A";
                vehicleNumber = user.Staff.VehicleNumber;
            }

            return Ok(new ProfileResponseDto
            {
                Success = true,
                Message = "Profile retrieved successfully",
                UserId = user.Id,
                FullName = user.FullName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = role,
                IsActive = user.IsActive,
                IsApproved = isApproved,
                UnitNumber = unitNumber,
                VehicleNumber = vehicleNumber,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileFormDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ProfileResponseDto
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
                return Unauthorized(new ProfileResponseDto
                {
                    Success = false,
                    Message = "Invalid token"
                });
            }

            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new ProfileResponseDto
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
            {
                if (model.ProfilePictureFile.Length > 2 * 1024 * 1024)
                {
                    return BadRequest(new ProfileResponseDto
                    {
                        Success = false,
                        Message = "File is too large. Maximum size is 2 MB."
                    });
                }

                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
                if (!allowedTypes.Contains(model.ProfilePictureFile.ContentType.ToLower()))
                {
                    return BadRequest(new ProfileResponseDto
                    {
                        Success = false,
                        Message = "Only JPG and PNG files are allowed."
                    });
                }

                using (var memoryStream = new MemoryStream())
                {
                    await model.ProfilePictureFile.CopyToAsync(memoryStream);
                    user.ProfilePicture = memoryStream.ToArray();
                    user.ProfilePictureContentType = model.ProfilePictureFile.ContentType;
                }
            }

            user.FirstName = model.FirstName.Trim();
            user.LastName = model.LastName.Trim();

            if (user.Student != null)
            {
                user.Student.UnitNumber = model.UnitNumber?.Trim();
                user.Student.VehicleNumber = model.VehicleNumber;
            }
            else if (user.Staff != null)
            {
                user.Staff.UnitNumber = model.UnitNumber?.Trim();
                user.Staff.VehicleNumber = model.VehicleNumber;
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new ProfileResponseDto
                {
                    Success = false,
                    Message = "Failed to update profile",
                    Errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            await _context.SaveChangesAsync();

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Customer";

            return Ok(new ProfileResponseDto
            {
                Success = true,
                Message = "Profile updated successfully",
                UserId = user.Id,
                FullName = user.FullName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Email = user.Email,
                Role = role,
                UnitNumber = user.Student?.UnitNumber ?? user.Staff?.UnitNumber ?? "N/A",
                VehicleNumber = user.Student?.VehicleNumber ?? user.Staff?.VehicleNumber ?? 0,
                CreatedAt = user.CreatedAt
            });
        }

        [HttpGet("picture/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPicture(string id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null || user.ProfilePicture == null || user.ProfilePicture.Length == 0)
            {
                return NotFound();
            }

            var contentType = string.IsNullOrEmpty(user.ProfilePictureContentType)
                ? "image/jpeg"
                : user.ProfilePictureContentType;

            return File(user.ProfilePicture, contentType);
        }
    }
}