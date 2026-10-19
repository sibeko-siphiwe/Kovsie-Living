using Kovsie_Living.Data;
using Kovsie_Living.Models;
using Kovsie_Living.Models.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kovsie_Living.Controllers.api
{
    [ApiController]
    [Route("api/profile")] // Explicit route to avoid conflict with Web CustomerController
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ProfileApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public ProfileApiController(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new ProfileResponseDto { Success = false, Message = "Invalid token" });

            var user = await _context.Users.Include(u => u.Student).Include(u => u.Staff).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new ProfileResponseDto { Success = false, Message = "User not found" });

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Customer";
            var isApproved = true;
            if (user.Student != null) isApproved = user.Student.IsApproved;
            else if (user.Staff != null) isApproved = user.Staff.IsApproved;

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
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new ProfileResponseDto { Success = false, Message = "Invalid token" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new ProfileResponseDto { Success = false, Message = "User not found" });

            if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
            {
                if (model.ProfilePictureFile.Length > 2 * 1024 * 1024)
                    return BadRequest(new ProfileResponseDto { Success = false, Message = "File is too large. Maximum size is 2 MB." });

                var contentType = (model.ProfilePictureFile.ContentType ?? "").ToLower().Trim();
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/pjpeg", "image/x-png" };
                var isImageWildcard = contentType.StartsWith("image/");

                if (!allowedTypes.Contains(contentType) && !isImageWildcard)
                    return BadRequest(new ProfileResponseDto { Success = false, Message = $"Only image files are allowed. Received: {contentType}" });

                using (var memoryStream = new MemoryStream())
                {
                    await model.ProfilePictureFile.CopyToAsync(memoryStream);
                    user.ProfilePicture = memoryStream.ToArray();
                    user.ProfilePictureContentType = model.ProfilePictureFile.ContentType;
                }
            }

            user.FirstName = model.FirstName.Trim();
            user.LastName = model.LastName.Trim();

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
                CreatedAt = user.CreatedAt
            });
        }

        [HttpGet("picture/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPicture(string id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null || user.ProfilePicture == null || user.ProfilePicture.Length == 0) return NotFound();

            var contentType = string.IsNullOrEmpty(user.ProfilePictureContentType) ? "image/jpeg" : user.ProfilePictureContentType;
            return File(user.ProfilePicture, contentType);
        }
    }
}