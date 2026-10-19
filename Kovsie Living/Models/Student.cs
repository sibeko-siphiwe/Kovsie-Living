namespace Kovsie_Living.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string StudentNumber { get; set; }
        public bool IsApproved { get; set; } = false;

        // Navigation Properties
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
