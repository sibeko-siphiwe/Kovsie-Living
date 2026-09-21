using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models.DTOs
{
    public class RegisterRequestDto
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string UserName { get; set; }

        public string UnitNumber { get; set; }

        public int VehicleNumber { get; set; }

        public bool IsStudent { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }
}