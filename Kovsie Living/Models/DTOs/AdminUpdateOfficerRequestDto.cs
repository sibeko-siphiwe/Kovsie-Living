using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models.DTOs
{
    public class AdminUpdateOfficerRequestDto
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
        public string EmployeeNumber { get; set; }

        [Required]
        [StringLength(15, MinimumLength = 10)]
        [RegularExpression(@"^[0-9]+$",
            ErrorMessage = "Phone number can only contain digits")]
        public string PhoneNumber { get; set; }
    }
}