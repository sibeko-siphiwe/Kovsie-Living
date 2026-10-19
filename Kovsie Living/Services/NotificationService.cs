using Kovsie_Living.Data;
using Kovsie_Living.Models;
using Microsoft.AspNetCore.Identity;

namespace Kovsie_Living.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task NotifyAdminsAndOfficers(string message, string type = "info")
        {
            var admins = await _userManager.GetUsersInRoleAsync("Administrator");
            var officers = await _userManager.GetUsersInRoleAsync("Officer");

            var recipients = admins.Concat(officers).ToList();

            foreach (var user in recipients)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }
    }
}