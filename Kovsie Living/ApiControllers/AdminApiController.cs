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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
         
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Unauthorized(new AdminDashboardResponseDto
                {
                    Success = false,
                    Message = "Invalid token"
                });
            }

            var customerRoleId = await _context.Roles
                .Where(r => r.Name == "Customer")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var officerRoleId = await _context.Roles
                .Where(r => r.Name == "Officer")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var totalCustomers = 0;
            if (!string.IsNullOrEmpty(customerRoleId))
            {
                var customerIds = _context.UserRoles
                    .Where(ur => ur.RoleId == customerRoleId)
                    .Select(ur => ur.UserId);
                totalCustomers = await _context.Users.CountAsync(u => customerIds.Contains(u.Id));
            }

            var totalOfficers = 0;
            if (!string.IsNullOrEmpty(officerRoleId))
            {
                var officerIds = _context.UserRoles
                    .Where(ur => ur.RoleId == officerRoleId)
                    .Select(ur => ur.UserId);
                totalOfficers = await _context.Users.CountAsync(u => officerIds.Contains(u.Id));
            }

            var pendingPaymentsCount = await _context.Payments
                .Where(p => p.Status == "Pending")
                .CountAsync();

            var totalRevenue = await _context.Payments
                .Where(p => p.Status == "Paid")
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            // Recent activity - combine last 5 users + last 5 payments
            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .Select(u => new AdminRecentActivityDto
                {
                    ActivityType = "User",
                    Title = u.FullName,
                    Subtitle = u.UserName,
                    Status = u.IsActive ? "Active" : "Inactive",
                    ActivityDate = u.CreatedAt
                })
                .ToListAsync();

            var recentPayments = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Tariff)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .Select(p => new AdminRecentActivityDto
                {
                    ActivityType = "Payment",
                    Title = p.User != null ? p.User.FullName : "Unknown",
                    Subtitle = p.Tariff != null ? p.Tariff.Name : "Unknown",
                    Status = p.Status,
                    ActivityDate = p.PaymentDate
                })
                .ToListAsync();

            var combined = recentUsers
                .Concat(recentPayments)
                .OrderByDescending(a => a.ActivityDate)
                .Take(5)
                .ToList();

            return Ok(new AdminDashboardResponseDto
            {
                Success = true,
                Message = "Dashboard loaded",
                FullName = user.FullName,
                Role = "Administrator",
                TotalCustomers = totalCustomers,
                TotalOfficers = totalOfficers,
                PendingPaymentsCount = pendingPaymentsCount,
                TotalRevenue = totalRevenue,
                RecentActivities = combined
            });
        }
         
        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            var customerRoleId = await _context.Roles
                .Where(r => r.Name == "Customer")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(customerRoleId))
            {
                return Ok(new AdminCustomersResponseDto
                {
                    Success = true,
                    Message = "No customers found",
                    TotalCount = 0,
                    Customers = new List<AdminCustomerItemDto>()
                });
            }

            var customerIds = await _context.UserRoles
                .Where(ur => ur.RoleId == customerRoleId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var customers = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Staff)
                .Where(u => customerIds.Contains(u.Id))
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var items = customers.Select(u =>
            {
                var isStudent = u.Student != null;
                var isStaff = u.Staff != null;
                var isApproved = u.Student?.IsApproved ?? u.Staff?.IsApproved ?? false;
                var unit = u.Student?.UnitNumber ?? u.Staff?.UnitNumber ?? "N/A";

                return new AdminCustomerItemDto
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    UserName = u.UserName,
                    Email = u.Email,
                    UserType = isStudent ? "Student" : (isStaff ? "Staff" : "Unknown"),
                    UnitNumber = unit,
                    IsApproved = isApproved,
                    IsActive = u.IsActive
                };
            }).ToList();

            return Ok(new AdminCustomersResponseDto
            {
                Success = true,
                Message = "Customers retrieved",
                TotalCount = items.Count,
                Customers = items
            });
        }

        [HttpPost("customers/toggle-approval/{userId}")]
        public async Task<IActionResult> ToggleCustomerApproval(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            if (user.Student != null)
            {
                user.Student.IsApproved = !user.Student.IsApproved;
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    success = true,
                    message = $"Student {(user.Student.IsApproved ? "approved" : "unapproved")} successfully"
                });
            }
            else if (user.Staff != null)
            {
                user.Staff.IsApproved = !user.Staff.IsApproved;
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    success = true,
                    message = $"Staff {(user.Staff.IsApproved ? "approved" : "unapproved")} successfully"
                });
            }

            return BadRequest(new { success = false, message = "User is not a customer" });
        }

        [HttpPost("customers/toggle-active/{userId}")]
        public async Task<IActionResult> ToggleCustomerActive(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"User {(user.IsActive ? "activated" : "deactivated")} successfully"
            });
        }

        [HttpPost("customers/delete/{userId}")]
        public async Task<IActionResult> DeleteCustomer(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            if (user.Student != null) _context.Students.Remove(user.Student);
            if (user.Staff != null) _context.Staff.Remove(user.Staff);

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);

            return Ok(new
            {
                success = result.Succeeded,
                message = result.Succeeded ? "User deleted successfully" : "Failed to delete user"
            });
        }
         
        [HttpGet("officers")]
        public async Task<IActionResult> GetOfficers()
        {
            var officerRoleId = await _context.Roles
                .Where(r => r.Name == "Officer")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(officerRoleId))
            {
                return Ok(new AdminOfficersResponseDto
                {
                    Success = true,
                    Message = "No officers found",
                    TotalCount = 0,
                    Officers = new List<AdminOfficerItemDto>()
                });
            }

            var officerIds = await _context.UserRoles
                .Where(ur => ur.RoleId == officerRoleId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var officers = await _context.Users
                .Include(u => u.FinancialOfficer)
                .Where(u => officerIds.Contains(u.Id))
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var items = officers.Select(u => new AdminOfficerItemDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                EmployeeNumber = u.FinancialOfficer?.EmployeeNumber ?? u.UserName,
                PhoneNumber = u.FinancialOfficer?.PhoneNumber ?? "N/A",
                Email = u.Email,
                IsAuthorized = u.FinancialOfficer?.IsAuthorized ?? false,
                IsActive = u.IsActive
            }).ToList();

            return Ok(new AdminOfficersResponseDto
            {
                Success = true,
                Message = "Officers retrieved",
                TotalCount = items.Count,
                Officers = items
            });
        }

        [HttpPost("officers/authorize/{userId}")]
        public async Task<IActionResult> AuthorizeOfficer(string userId)
        {
            var officer = await _context.FinancialOfficers
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (officer == null)
            {
                return NotFound(new { success = false, message = "Officer not found" });
            }

            officer.IsAuthorized = true;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Officer authorized successfully" });
        }

        [HttpPost("officers/revoke/{userId}")]
        public async Task<IActionResult> RevokeOfficer(string userId)
        {
            var officer = await _context.FinancialOfficers
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (officer == null)
            {
                return NotFound(new { success = false, message = "Officer not found" });
            }

            officer.IsAuthorized = false;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Officer authorization revoked" });
        }

        [HttpPost("officers/delete/{userId}")]
        public async Task<IActionResult> DeleteOfficer(string userId)
        {
            var user = await _context.Users
                .Include(u => u.FinancialOfficer)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { success = false, message = "Officer not found" });
            }

            if (user.FinancialOfficer != null)
            {
                _context.FinancialOfficers.Remove(user.FinancialOfficer);
            }

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);

            return Ok(new
            {
                success = result.Succeeded,
                message = result.Succeeded ? "Officer deleted successfully" : "Failed to delete officer"
            });
        } 

        [HttpGet("tariffs")]
        public async Task<IActionResult> GetTariffs()
        {
            var tariffs = await _context.Tariffs
                .OrderBy(t => t.Name)
                .Select(t => new AdminTariffItemDto
                {
                    TariffId = t.TariffId,
                    Name = t.Name,
                    Amount = t.Amount,
                    IsActive = t.IsActive
                })
                .ToListAsync();

            return Ok(new AdminTariffsResponseDto
            {
                Success = true,
                Message = "Tariffs retrieved",
                TotalCount = tariffs.Count,
                Tariffs = tariffs
            });
        }
    }
}