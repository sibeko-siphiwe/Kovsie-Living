using Kovsie_Living.Models;
using Microsoft.AspNetCore.Identity;

namespace Kovsie_Living.Data.SeedData
{
    public class SeedData
    {
        private const string Username = "Admin";
        private const string Email = "Admin@ufs.ac.za";
        private const string Password = "Secret123!";
        private static readonly string[] Roles =
        {
            "Administrator",
            "Officer",
            "Customer"
        };

        public static async Task EnsurePopulated(IApplicationBuilder app)
        {
            AppDbContext _context = app.ApplicationServices
                .CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<AppDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
             
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
             
            var admin = await userManager.FindByNameAsync(Username);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = Username,
                    Email = Email,
                    FirstName = "System",
                    LastName = "Administrator",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(admin, Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Administrator");
                }
            }

            if (!_context.Tariffs.Any())
            {
                var tariffs = new List<Tariff>
                {
                    new Tariff
                    {
                        Name = "Electricity",
                        Amount = 1500.00m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Tariff
                    {
                        Name = "Rent",
                        Amount = 3500.00m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Tariff
                    {
                        Name = "Water Usage",
                        Amount = 800.00m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Tariff
                    {
                        Name = "Parking Fee",
                        Amount = 500.00m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Tariff
                    {
                        Name = "Refuse",
                        Amount = 300.00m,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                _context.Tariffs.AddRange(tariffs);
                await _context.SaveChangesAsync();
            }
        }
    }
}