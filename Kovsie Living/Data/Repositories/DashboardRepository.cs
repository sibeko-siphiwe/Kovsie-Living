using Kovsie_Living.Models;
using Microsoft.EntityFrameworkCore;

namespace Kovsie_Living.Data.Repositories
{
    public interface IDashboardRepository
    {
        IEnumerable<ApplicationUser> GetAllCustomers();
        IEnumerable<ApplicationUser> GetAllOfficers();
    }

    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;
        public DashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<ApplicationUser> GetAllCustomers()
        {
            var customerRoleId = _context.Roles
                .Where(r => r.Name == "Customer")
                .Select(r => r.Id)
                .FirstOrDefault();

            if (customerRoleId == null)
                return new List<ApplicationUser>();

            var customerUserIds = _context.UserRoles
                .Where(ur => ur.RoleId == customerRoleId)
                .Select(ur => ur.UserId)
                .ToList();

            return _context.Users
                .Include(u => u.Student)
                .Include(u => u.Staff)
                .Where(u => customerUserIds.Contains(u.Id) && u.IsActive)
                .ToList();
        }

        public IEnumerable<ApplicationUser> GetAllOfficers()
        {
            var officerRoleId = _context.Roles
                .Where(r => r.Name == "Officer")
                .Select(r => r.Id)
                .FirstOrDefault();

            if (officerRoleId == null)
                return new List<ApplicationUser>();

            var officerUserIds = _context.UserRoles
                .Where(ur => ur.RoleId == officerRoleId)
                .Select(ur => ur.UserId)
                .ToList();

            return _context.Users
                .Include(u => u.FinancialOfficer)
                .Where(u => officerUserIds.Contains(u.Id))
                .ToList();
        }
    }
}
