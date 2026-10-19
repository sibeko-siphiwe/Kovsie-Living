namespace Kovsie_Living.Models
{
    public class CustomerReportData
    {
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }
        public int ApprovedCustomers { get; set; }
        public int PendingCustomers { get; set; }
        public decimal TotalCollected { get; set; }
        public List<CustomerReportItem> Customers { get; set; }
    }

    public class CustomerReportItem
    {
        public string FullName { get; set; }
        public string UserType { get; set; }
        public string UserNumber { get; set; }
        public string Status { get; set; }
        public DateTime RegistrationDate { get; set; }
        public decimal TotalPaid { get; set; }
        public int PaymentCount { get; set; }
    }
}