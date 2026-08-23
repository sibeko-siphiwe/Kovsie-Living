using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models.ViewModels
{
    public class EditOfficerViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [DisplayName("First Name")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First Name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "First Name can only contain letters, spaces, hyphens and apostrophes")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [DisplayName("Last Name")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last Name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Last Name can only contain letters, spaces, hyphens and apostrophes")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Employee Number is required")]
        [DisplayName("Employee Number")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Employee Number must be between 3 and 20 characters")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Only letters and numbers are allowed")]
        public string EmployeeNumber { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [DisplayName("Phone Number")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone Number must be between 10 and 15 characters")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Phone number can only contain digits")]
        public string PhoneNumber { get; set; }
    }
}