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

        [HttpPost("customers/create")]
        public async Task<IActionResult> CreateCustomer([FromBody] AdminCreateCustomerRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AdminCreateCustomerResponseDto
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
                var userName = model.UserName?.Trim();

                var exists = await _userManager.FindByNameAsync(userName);
                if (exists != null)
                {
                    return BadRequest(new AdminCreateCustomerResponseDto
                    {
                        Success = false,
                        Message = "This student/staff number is already registered."
                    });
                }

                var email = $"{userName}@ufs.ac.za";

                var emailExists = await _userManager.FindByEmailAsync(email);
                if (emailExists != null)
                {
                    return BadRequest(new AdminCreateCustomerResponseDto
                    {
                        Success = false,
                        Message = "An account with this email already exists."
                    });
                }

                var tempPassword = TemporaryPasswordGenerator.Generate();

                var user = new ApplicationUser
                {
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    UserName = userName,
                    Email = email,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, tempPassword);
                if (!result.Succeeded)
                {
                    return BadRequest(new AdminCreateCustomerResponseDto
                    {
                        Success = false,
                        Message = "Failed to create account",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                if (model.IsStudent)
                {
                    _context.Students.Add(new Student
                    {
                        StudentNumber = userName,
                        UnitNumber = model.UnitNumber?.Trim(),
                        VehicleNumber = model.VehicleNumber,
                        IsApproved = true,
                        UserId = user.Id
                    });
                }
                else
                {
                    _context.Staff.Add(new Staff
                    {
                        StaffNumber = userName,
                        UnitNumber = model.UnitNumber?.Trim(),
                        VehicleNumber = model.VehicleNumber,
                        IsApproved = true,
                        UserId = user.Id
                    });
                }

                var roleResult = await _userManager.AddToRoleAsync(user, "Customer");
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    return BadRequest(new AdminCreateCustomerResponseDto
                    {
                        Success = false,
                        Message = "Failed to assign customer role",
                        Errors = roleResult.Errors.Select(e => e.Description).ToList()
                    });
                }

                await _context.SaveChangesAsync();

                return Ok(new AdminCreateCustomerResponseDto
                {
                    Success = true,
                    Message = "Customer created successfully",
                    UserId = user.Id,
                    FullName = user.FullName,
                    UserName = user.UserName,
                    TemporaryPassword = tempPassword
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new AdminCreateCustomerResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred."
                });
            }
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

        [HttpPut("customers/update/{userId}")]
        public async Task<IActionResult> UpdateCustomer(string userId,
                                                [FromBody] AdminUpdateCustomerRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AdminCreateCustomerResponseDto
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
                var user = await _context.Users
                    .Include(u => u.Student)
                    .Include(u => u.Staff)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new AdminCreateCustomerResponseDto
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var newUserName = model.UserName?.Trim();
                var userNameChanged = !string.Equals(user.UserName, newUserName,
                                                      StringComparison.OrdinalIgnoreCase);

                if (userNameChanged)
                {
                    var existing = await _userManager.FindByNameAsync(newUserName);
                    if (existing != null && existing.Id != user.Id)
                    {
                        return BadRequest(new AdminCreateCustomerResponseDto
                        {
                            Success = false,
                            Message = "This student/staff number is already registered."
                        });
                    }
                }

                user.FirstName = model.FirstName.Trim();
                user.LastName = model.LastName.Trim();

                if (userNameChanged)
                {
                    user.UserName = newUserName;
                    user.Email = $"{newUserName}@ufs.ac.za";

                    if (user.Student != null)
                    {
                        user.Student.StudentNumber = newUserName;
                    }
                    else if (user.Staff != null)
                    {
                        user.Staff.StaffNumber = newUserName;
                    }
                }

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
                    return BadRequest(new AdminCreateCustomerResponseDto
                    {
                        Success = false,
                        Message = "Failed to update customer",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                await _context.SaveChangesAsync();

                return Ok(new AdminCreateCustomerResponseDto
                {
                    Success = true,
                    Message = "Customer updated successfully",
                    UserId = user.Id,
                    FullName = user.FullName,
                    UserName = user.UserName
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new AdminCreateCustomerResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred."
                });
            }
        }

        [HttpPost("officers/create")]
        public async Task<IActionResult> CreateOfficer([FromBody] AdminCreateOfficerRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AdminCreateOfficerResponseDto
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
                var employeeNumber = model.EmployeeNumber?.Trim();

                var exists = await _userManager.FindByNameAsync(employeeNumber);
                if (exists != null)
                {
                    return BadRequest(new AdminCreateOfficerResponseDto
                    {
                        Success = false,
                        Message = "This employee number is already registered."
                    });
                }

                var email = $"{employeeNumber}@ufs.ac.za";

                var emailExists = await _userManager.FindByEmailAsync(email);
                if (emailExists != null)
                {
                    return BadRequest(new AdminCreateOfficerResponseDto
                    {
                        Success = false,
                        Message = "An account with this email already exists."
                    });
                }

                var tempPassword = TemporaryPasswordGenerator.Generate();

                var user = new ApplicationUser
                {
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    UserName = employeeNumber,
                    Email = email,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, tempPassword);
                if (!result.Succeeded)
                {
                    return BadRequest(new AdminCreateOfficerResponseDto
                    {
                        Success = false,
                        Message = "Failed to create account",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                _context.FinancialOfficers.Add(new FinancialOfficer
                {
                    EmployeeNumber = employeeNumber,
                    PhoneNumber = model.PhoneNumber.Trim(),
                    IsAuthorized = false,
                    UserId = user.Id
                });

                var roleResult = await _userManager.AddToRoleAsync(user, "Officer");
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    return BadRequest(new AdminCreateOfficerResponseDto
                    {
                        Success = false,
                        Message = "Failed to assign officer role",
                        Errors = roleResult.Errors.Select(e => e.Description).ToList()
                    });
                }

                await _context.SaveChangesAsync();

                return Ok(new AdminCreateOfficerResponseDto
                {
                    Success = true,
                    Message = "Officer created. Authorize them to allow sign-in.",
                    UserId = user.Id,
                    FullName = user.FullName,
                    UserName = user.UserName,
                    TemporaryPassword = tempPassword
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new AdminCreateOfficerResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred."
                });
            }
        }

        [HttpPut("officers/update/{userId}")]
        public async Task<IActionResult> UpdateOfficer(string userId,
                                               [FromBody] AdminUpdateOfficerRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AdminCreateOfficerResponseDto
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
                var user = await _context.Users
                    .Include(u => u.FinancialOfficer)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new AdminCreateOfficerResponseDto
                    {
                        Success = false,
                        Message = "Officer not found"
                    });
                }

                var newEmployeeNumber = model.EmployeeNumber?.Trim();
                var numberChanged = !string.Equals(user.UserName, newEmployeeNumber,
                                                    StringComparison.OrdinalIgnoreCase);

                if (numberChanged)
                {
                    var existing = await _userManager.FindByNameAsync(newEmployeeNumber);
                    if (existing != null && existing.Id != user.Id)
                    {
                        return BadRequest(new AdminCreateOfficerResponseDto
                        {
                            Success = false,
                            Message = "This employee number is already registered."
                        });
                    }
                }

                user.FirstName = model.FirstName.Trim();
                user.LastName = model.LastName.Trim();

                if (numberChanged)
                {
                    user.UserName = newEmployeeNumber;
                    user.Email = $"{newEmployeeNumber}@ufs.ac.za";

                    if (user.FinancialOfficer != null)
                    {
                        user.FinancialOfficer.EmployeeNumber = newEmployeeNumber;
                    }
                }

                if (user.FinancialOfficer != null)
                {
                    user.FinancialOfficer.PhoneNumber = model.PhoneNumber.Trim();
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new AdminCreateOfficerResponseDto
                    {
                        Success = false,
                        Message = "Failed to update officer",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                await _context.SaveChangesAsync();

                return Ok(new AdminCreateOfficerResponseDto
                {
                    Success = true,
                    Message = "Officer updated successfully",
                    UserId = user.Id,
                    FullName = user.FullName,
                    UserName = user.UserName
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new AdminCreateOfficerResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred."
                });
            }
        }

        [HttpPost("tariffs/create")]
        public async Task<IActionResult> CreateTariff([FromBody] AdminCreateTariffRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AdminTariffActionResponseDto
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
                var name = model.Name?.Trim();

                var exists = await _context.Tariffs
                    .AnyAsync(t => t.Name.ToLower() == name.ToLower());

                if (exists)
                {
                    return BadRequest(new AdminTariffActionResponseDto
                    {
                        Success = false,
                        Message = "A tariff with this name already exists."
                    });
                }

                var tariff = new Tariff
                {
                    Name = name,
                    Amount = model.Amount,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Tariffs.Add(tariff);
                await _context.SaveChangesAsync();

                return Ok(new AdminTariffActionResponseDto
                {
                    Success = true,
                    Message = "Tariff created successfully",
                    TariffId = tariff.TariffId,
                    Name = tariff.Name,
                    Amount = tariff.Amount,
                    IsActive = tariff.IsActive
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new AdminTariffActionResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred."
                });
            }
        }

        [HttpPut("tariffs/update/{id}")]
        public async Task<IActionResult> UpdateTariff(int id,
                                                      [FromBody] AdminUpdateTariffRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AdminTariffActionResponseDto
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
                var tariff = await _context.Tariffs.FindAsync(id);
                if (tariff == null)
                {
                    return NotFound(new AdminTariffActionResponseDto
                    {
                        Success = false,
                        Message = "Tariff not found"
                    });
                }

                var name = model.Name?.Trim();

                var exists = await _context.Tariffs
                    .AnyAsync(t => t.TariffId != id && t.Name.ToLower() == name.ToLower());

                if (exists)
                {
                    return BadRequest(new AdminTariffActionResponseDto
                    {
                        Success = false,
                        Message = "A tariff with this name already exists."
                    });
                }

                tariff.Name = name;
                tariff.Amount = model.Amount;
                tariff.IsActive = model.IsActive;
                tariff.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new AdminTariffActionResponseDto
                {
                    Success = true,
                    Message = "Tariff updated successfully",
                    TariffId = tariff.TariffId,
                    Name = tariff.Name,
                    Amount = tariff.Amount,
                    IsActive = tariff.IsActive
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new AdminTariffActionResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred."
                });
            }
        }

        [HttpDelete("tariffs/delete/{id}")]
        public async Task<IActionResult> DeleteTariff(int id)
        {
            try
            {
                var tariff = await _context.Tariffs.FindAsync(id);
                if (tariff == null)
                {
                    return NotFound(new AdminTariffActionResponseDto
                    {
                        Success = false,
                        Message = "Tariff not found"
                    });
                }

                var inUse = await _context.Payments.AnyAsync(p => p.TariffId == id);
                if (inUse)
                {
                    return BadRequest(new AdminTariffActionResponseDto
                    {
                        Success = false,
                        Message = "This tariff is in use and cannot be deleted. Deactivate it instead.",
                        TariffId = id
                    });
                }

                _context.Tariffs.Remove(tariff);
                await _context.SaveChangesAsync();

                return Ok(new AdminTariffActionResponseDto
                {
                    Success = true,
                    Message = "Tariff deleted successfully",
                    TariffId = id
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new AdminTariffActionResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred."
                });
            }
        }
    }
}