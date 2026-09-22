namespace Kovsie_Living.Models.DTOs
{
    public class AdminCreateOfficerResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string TemporaryPassword { get; set; }
        public List<string> Errors { get; set; }
    }
}