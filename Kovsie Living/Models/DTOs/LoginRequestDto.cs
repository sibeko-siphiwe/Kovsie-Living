using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}