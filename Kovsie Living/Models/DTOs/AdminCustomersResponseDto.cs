namespace Kovsie_Living.Models.DTOs
{
    public class AdminCustomersResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalCount { get; set; }
        public List<AdminCustomerItemDto> Customers { get; set; }
    }

    public class AdminCustomerItemDto
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string UserType { get; set; }
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
    }
}