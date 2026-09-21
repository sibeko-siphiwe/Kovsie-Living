using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Kovsie_Living.Models.DTOs
{
    public class UpdateProfileFormDto
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; }

        [StringLength(10)]
        public string UnitNumber { get; set; }

        [Range(0, 10)]
        public int VehicleNumber { get; set; }

        public IFormFile ProfilePictureFile { get; set; }
    }
}