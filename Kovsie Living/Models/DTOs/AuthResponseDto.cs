namespace Kovsie_Living.Models.DTOs
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public string UnitNumber { get; set; }
        public int VehicleNumber { get; set; }
        public List<string> Errors { get; set; }
    }
}