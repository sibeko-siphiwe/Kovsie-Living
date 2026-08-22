namespace Kovsie_Living.Models
{
    public class FinancialOfficer
    {
        public int Id { get; set; }
        public string EmployeeNumber { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsAuthorized { get; set; } = false;

        // Navigation Properties
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
