namespace Kovsie_Living.Models.DTOs
{
    public class AdminOfficersResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalCount { get; set; }
        public List<AdminOfficerItemDto> Officers { get; set; }
    }

    public class AdminOfficerItemDto
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string EmployeeNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public bool IsAuthorized { get; set; }
        public bool IsActive { get; set; }
    }
}