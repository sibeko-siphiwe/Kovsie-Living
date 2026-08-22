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
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<AppDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Create roles if they don't exist
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create admin user if it doesn't exist
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
        }
    }
}