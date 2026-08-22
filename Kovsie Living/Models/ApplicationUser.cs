using Microsoft.AspNetCore.Identity;

namespace Kovsie_Living.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Navigation Properties
        public Student Student { get; set; }
        public Staff Staff { get; set; }
        public FinancialOfficer FinancialOfficer { get; set; }
    }
}
