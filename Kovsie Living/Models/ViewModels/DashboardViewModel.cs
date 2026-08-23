namespace Kovsie_Living.Models.ViewModels
{
    public class DashboardViewModel
    {
        public IEnumerable<ApplicationUser> Users { get; set; }
        public IEnumerable<ApplicationUser> Officers { get; set; }
        public IEnumerable<Tariff> Tariffs { get; set; }
        //public IEnumerable<Payment> Payments { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int ApprovedUsers { get; set; }
        public int PendingUsers { get; set; }
    }
}
