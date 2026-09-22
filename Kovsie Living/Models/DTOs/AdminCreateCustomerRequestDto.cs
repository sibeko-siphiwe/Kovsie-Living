using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models.DTOs
{
    public class AdminCreateCustomerRequestDto
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9]+$",
            ErrorMessage = "Only letters and numbers are allowed")]
        public string UserName { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 1)]
        public string UnitNumber { get; set; }

        [Range(0, 10)]
        public int VehicleNumber { get; set; }

        public bool IsStudent { get; set; }
    }
}