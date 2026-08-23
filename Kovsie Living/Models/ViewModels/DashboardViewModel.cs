namespace Kovsie_Living.Models.ViewModels
{
    public class DashboardViewModel
    {
        public IEnumerable<ApplicationUser> Users { get; set; }
        public IEnumerable<ApplicationUser> Officers { get; set; }
        //public IEnumerable<Payment> Payments { get; set; }
    }
}
