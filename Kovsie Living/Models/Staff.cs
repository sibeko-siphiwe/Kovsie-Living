namespace Kovsie_Living.Models
{
    public class Staff
    {
        public int Id { get; set; }
        public string StaffNumber { get; set; }
        public string UnitNumber { get; set; }
        public int VehicleNumber { get; set; } = 0;
        public bool IsApproved { get; set; } = false;  

        // Navigation Properties
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
